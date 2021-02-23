using UnityEngine ;

using Unity.Jobs ;
using Unity.Physics ;
using Unity.Physics.Systems ;
using Unity.Physics.Extensions ;
using Unity.Entities ;
using Unity.Collections ;
using Unity.Transforms ;
using Unity.Mathematics ;

using Antypodish.DOTS ;
using Antypodish.GeneticNueralNetwork.DOTS ;


namespace Antypodish.GNNExample.DOTS
{

    // [UpdateInGroup ( typeof ( FixedStepSimulationSystemGroup ))]
    [UpdateAfter ( typeof ( FixedStepSimulationSystemGroup ))]
    // [UpdateAfter ( typeof ( EndFramePhysicsSystem ))] // Is ignored.
    public class LIDARSystem : SystemBase
    {

        BuildPhysicsWorld  buildPhysicsWorld ;
        EndFramePhysicsSystem endFramePhysicsSystem;
        
        int i_raysCount = 9 ;
        quaternion q_90deg ;
        NativeArray <quaternion> na_rays ;

        protected override void OnCreate ( )
        {
            buildPhysicsWorld     = World.GetOrCreateSystem <BuildPhysicsWorld> () ;
            endFramePhysicsSystem = World.GetExistingSystem <EndFramePhysicsSystem> () ;

            base.OnCreate ( ); 
        }

        protected override void OnStartRunning ( )
        {

            // LIDAR rays 180 degree, from left to front to right.
            q_90deg  = quaternion.RotateY ( ( math.PI * 0.5f ) ) ;
            na_rays  = new NativeArray<quaternion> ( i_raysCount, Allocator.Persistent ) ;
            
            float f_rayPiFraction = math.PI / ( i_raysCount - 1 ) ;

            for ( int i = 0; i < i_raysCount; i ++ )
            {
                na_rays [i] = math.mul ( quaternion.RotateY ( ( f_rayPiFraction * i ) ), q_90deg ) ;
            }
        }

        protected override void OnStopRunning ( )
        {
            na_rays.Dispose () ;
        }

        protected override void OnUpdate ( )
        {

            CollisionWorld collisionWorld = buildPhysicsWorld.PhysicsWorld.CollisionWorld ;
            // Dependency                    = JobHandle.CombineDependencies ( Dependency, endFramePhysicsSystem.GetOutputDependency ()) ;
            
            var na_rays                   = this.na_rays ;
            int i_raysCount               = this.i_raysCount ;


            Entities
                .WithName ( "LIDARJob" )
                .WithAll <NNBrainTag, LIDARTag, IsAliveTag> ()
                .WithNone <NNIsFinishedTag> ()
                .WithReadOnly ( collisionWorld )
                // .WithReadOnly ( collector )
                .WithReadOnly ( na_rays )
                .ForEach ( ( ref DynamicBuffer <NNInputNeuronsValuesBuffer> a_inputLayerValues, in Translation position, in Rotation q, in VehicleVelocityComponent velocity ) =>
            {

                float3 f3_startPoint = position.Value ;


                for ( int i = 0; i < i_raysCount; i ++ )
                {
                    
                    quaternion q_rayDirection = na_rays [i] ;

                    float3 f3_endPoint        = f3_startPoint + math.mul ( math.mul ( q.Value, q_rayDirection ), Vector3.forward ) * 3 ;
                    
                    var raycastInput = new RaycastInput
                    {
                        Start  = f3_startPoint,
                        End    = f3_endPoint,
                        Filter = CollisionFilter.Default
                    } ;
                    
                    // raycastInput.Filter.CollidesWith = 2 ; // Scores layer.
                    raycastInput.Filter.CollidesWith = 1 ; // Barriers layer.

                    float f_input = -1 ;
                    
                    var collector = new IgnoreTransparentClosestHitCollector ( collisionWorld ) ;

                    if ( collisionWorld.CastRay ( raycastInput, ref collector ) )
                    {
                        Unity.Physics.RaycastHit hit = collector.ClosestHit ;

// Debug.DrawLine ( f3_startPoint, f3_endPoint, Color.red ) ;
// Debug.DrawLine ( f3_startPoint, hit.Position, Color.green ) ; // Length of ray, until hit collider.

                        f_input = math.lengthsq ( hit.Position - f3_startPoint ) ;
                    }
                    // else
                    // {
// Debug.DrawLine ( f3_startPoint, f3_endPoint, Color.blue ) ;
                    // }
                    
                    a_inputLayerValues [i] = new NNInputNeuronsValuesBuffer () { f = f_input } ;

                }

                a_inputLayerValues [a_inputLayerValues.Length -1] = new NNInputNeuronsValuesBuffer () { f = math.length ( velocity.f_forwardSpeed ) } ;
                a_inputLayerValues [a_inputLayerValues.Length -2] = new NNInputNeuronsValuesBuffer () { f = math.length ( velocity.f_sideWaySkiddSpeed ) } ;

            }).ScheduleParallel ();


            // Added based on forum suggestion
            // https://forum.unity.com/threads/i-got-issue-with-raycast-performance-for-many-thousands-of-rays.1062185/#post-6867647
            // buildPhysicsWorld.AddInputDependencyToComplete ( Dependency ) ;

        }
    }
}