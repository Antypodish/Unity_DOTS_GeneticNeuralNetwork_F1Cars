using Unity.Jobs ;
using Unity.Entities ;
using Unity.Mathematics ;

using Unity.Physics.Systems ;

using Antypodish.DOTS ;
using Antypodish.GeneticNueralNetwork.DOTS ;

namespace Antypodish.AI.DOTS
{
   
    [UpdateInGroup ( typeof ( FixedStepSimulationSystemGroup ))]
    [UpdateAfter ( typeof ( Unity.Physics.Stateful.TriggerEventConversionSystem ))]
    public class CollectScoresSystem : SystemBase
    {

        BeginInitializationEntityCommandBufferSystem becb ;
        
        // private Unity.Physics.Stateful.TriggerEventConversionSystem triggerSystem ; 
        EndFramePhysicsSystem endFramePhysicsSystem ;
 
        protected override void OnCreate ( )
        {
            becb                    = World.GetOrCreateSystem <BeginInitializationEntityCommandBufferSystem> () ;
            // triggerSystem           = World.GetOrCreateSystem <Unity.Physics.Stateful.TriggerEventConversionSystem> () ;
            endFramePhysicsSystem   = World.GetExistingSystem <EndFramePhysicsSystem> () ;
        }


        protected override void OnDestroy ( )
        {
           
        }

        // Update is called once per frame
        protected override void OnUpdate ( )
        {
            
            Dependency = JobHandle.CombineDependencies ( Dependency, endFramePhysicsSystem.GetOutputDependency ()) ;

            EntityCommandBuffer ecb = becb.CreateCommandBuffer () ;
            EntityCommandBuffer.ParallelWriter ecbp = ecb.AsParallelWriter () ;

            Entities
                .WithName( "ChangeStateOnEnterJob" )
                .WithAll <IsAliveTag, CarTag> ()
                .WithNone <NNIsFinishedTag, NNIsPreviousGenerationTag> ()
                .ForEach (( Entity entity, int entityInQueryIndex, ref DynamicBuffer <Unity.Physics.Stateful.StatefulTriggerEvent> a_triggerEventBuffer, ref NNBrainScoreComponent brainScore, ref VehicleControllsComponent controlls, ref VehicleVelocityComponent velocity ) =>
            {

                // int i_brainScore   = brainScore.i ;
                float f_brainScore = brainScore.f ;

                // Do not allow to collect this brain, if it tries to go in reverse.
// Prevent NaN conditions. Unknown reason, why this occures.
                // Finish this brain.
                if ( velocity.isOnReverse || math.lengthsq ( velocity.f3_speed ) > 100 || math.abs ( velocity.f_sideWaySkiddSpeed ) > 100 || math.isnan ( controlls.f_throtle ) ) 
                {
                    _ResetStates ( ref controlls, ref velocity, ref ecbp, entityInQueryIndex, entity ) ;
                    return ;
                }


                // Gain score point, for entering score trigger.
                for ( int i = 0; i < a_triggerEventBuffer.Length; i++)
                {
                    
                    Unity.Physics.Stateful.StatefulTriggerEvent triggerEvent = a_triggerEventBuffer [i];
                    Entity otherEntity                                       = triggerEvent.GetOtherEntity (entity) ;

                    if ( triggerEvent.State == Unity.Physics.Stateful.EventOverlapState.Enter )
                    {
                        
                        if ( HasComponent <BarrierTag> ( otherEntity ) )
                        {
// Debug.Log ( "Barrier entered: " + otherEntity ) ;

                            _ResetStates ( ref controlls, ref velocity, ref ecbp, entityInQueryIndex, entity ) ;
                        }
                        else if ( HasComponent <ScoreTag> ( otherEntity ) )
                        {
                            // i_brainScore ++ ;
                            // brainScore.i = i_brainScore ;

                            f_brainScore ++ ;


// Debug.Log ( "Score gained: " + brainScore.i ) ;
                        }
                        // else
                        //{
                        //    Debug.Log ( "Something else entered: " + entity ) ;
                        //}

                        break ;

                    }
                    else if ( triggerEvent.State == Unity.Physics.Stateful.EventOverlapState.Stay )
                    {

                        if ( HasComponent <BarrierTag> ( otherEntity ) )
                        {
// Debug.Log ( "Barrier stays: " + otherEntity ) ;

                            _ResetStates ( ref controlls, ref velocity, ref ecbp, entityInQueryIndex, entity ) ;
                        }

                    }

                   // else
                   // {
                   //     Debug.Log ( "Is triggered: " + entity ) ;
                   // }

                } // for

                // Discourage skidding.
                // f_brainScore -= math.abs ( velocity.f_sideWaySkiddSpeed * 0.01f ) ;
                brainScore.f  = math.max ( 0, f_brainScore ) ;
                // Clamp and round downNNManagerBrains01.
                brainScore.i  = (int) brainScore.f ;
                

            }).ScheduleParallel () ;
            
            becb.AddJobHandleForProducer ( Dependency ) ;
            // Dependency.Complete () ;

        }

        static private void _ResetStates ( ref VehicleControllsComponent controlls, ref VehicleVelocityComponent velocity, ref EntityCommandBuffer.ParallelWriter ecbp, int entityInQueryIndex, Entity entity )
        {
            
            controlls.f_steering = 0 ;
            controlls.f_throtle = 0 ;
            velocity.f3_speed = 0 ;
            velocity.f_acceleration = 0 ;
            ecbp.AddComponent <NNIsFinishedTag> ( entityInQueryIndex, entity ) ;

            // This is required, to eliminate continous collidion detection and prevent BuildpPhysicsWorld system, from creating NativeArray memory leaks.
            ecbp.AddComponent <Unity.Physics.PhysicsExclude> ( entityInQueryIndex, entity ) ;

        }

    }
}