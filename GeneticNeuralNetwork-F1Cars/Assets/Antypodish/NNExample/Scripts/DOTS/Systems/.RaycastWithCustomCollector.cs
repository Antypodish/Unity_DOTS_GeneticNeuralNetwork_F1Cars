using UnityEngine ;

using Unity.Jobs;
using Unity.Physics;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Mathematics;

using Unity.Physics.Extensions ;

namespace Antypodish.CopsAI.DOTS
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(EndFramePhysicsSystem))]
    public class RaycastWithCustomCollectorSystem : SystemBase
    {
        BuildPhysicsWorld buildPhysicsWorld;
        EndFramePhysicsSystem endFramePhysicsSystem;
        // EndFixedStepSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

        protected override void OnCreate()
        {
            buildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
            endFramePhysicsSystem = World.GetExistingSystem<EndFramePhysicsSystem>();
            // m_EntityCommandBufferSystem = World.GetExistingSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            CollisionWorld collisionWorld = buildPhysicsWorld.PhysicsWorld.CollisionWorld;
            // EntityCommandBuffer commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();
            Dependency = JobHandle.CombineDependencies(Dependency, endFramePhysicsSystem.GetOutputDependency ()) ;
        
            // UnityEngine.Debug.Log ( "Ray 2" ) ;


            Vector3 f3_point = Camera.main.ScreenToWorldPoint ( Input.mousePosition ) ;
            float3 f3_endPoint = f3_point -Vector3.up * 200 ;
            Debug.DrawLine ( f3_point, f3_endPoint, Color.yellow ) ;

            // Perform the Raycast
            var raycastInput = new RaycastInput
            {
                Start  = f3_point,
                End    = f3_endPoint,
                Filter = CollisionFilter.Default
            } ;

            // Debug.Log ( raycastInput.Filter.BelongsTo + "; " + raycastInput.Filter.CollidesWith + "; " + raycastInput.Filter.GroupIndex ) ;
            // raycastInput.Filter.CollidesWith = 0 ;

            raycastInput.Filter.CollidesWith = 2 ;

            var collector = new IgnoreTransparentClosestHitCollector ( collisionWorld ) ;

            collisionWorld.CastRay ( raycastInput, ref collector ) ;
            
            var hit = collector.ClosestHit ;
            var hitDistance = 200 * hit.Fraction;
        
            Debug.DrawLine ( f3_point, f3_point -Vector3.up * hitDistance, Color.green ) ;

            if ( hitDistance != 0 ) Debug.Log ( "Try hit at: " + hitDistance ) ;

            /*
            Entities
                .WithName("RaycastWithCustomCollector")
                .WithBurst()
                .ForEach((Entity entity, ref Translation position, ref Rotation rotation, ref VisualizedRaycast visualizedRaycast) =>
                {
                    var raycastLength = visualizedRaycast.RayLength;

                    // Perform the Raycast
                    var raycastInput = new RaycastInput
                    {
                        Start = position.Value,
                        End = position.Value + (math.forward(rotation.Value) * visualizedRaycast.RayLength),
                        Filter = CollisionFilter.Default
                    };

                    var collector = new IgnoreTransparentClosestHitCollector(collisionWorld);

                    collisionWorld.CastRay(raycastInput, ref collector);

                    var hit = collector.ClosestHit;
                    var hitDistance = raycastLength * hit.Fraction;

                    // position the entities and scale based on the ray length and hit distance
                    // visualization elements are scaled along the z-axis aka math.forward
                    var newFullRayPosition = new float3(0, 0, raycastLength * 0.5f);
                    var newFullRayScale = new float3(1f, 1f, raycastLength);
                    var newHitPosition = new float3(0, 0, hitDistance);
                    var newHitRayPosition = new float3(0, 0, hitDistance * 0.5f);
                    var newHitRayScale = new float3(1f, 1f, raycastLength * hit.Fraction);

                    commandBuffer.SetComponent(visualizedRaycast.HitPositionEntity, new Translation { Value = newHitPosition });
                    commandBuffer.SetComponent(visualizedRaycast.HitRayEntity, new Translation { Value = newHitRayPosition });
                    commandBuffer.SetComponent(visualizedRaycast.HitRayEntity, new NonUniformScale { Value = newHitRayScale });
                    commandBuffer.SetComponent(visualizedRaycast.FullRayEntity, new Translation { Value = newFullRayPosition });
                    commandBuffer.SetComponent(visualizedRaycast.FullRayEntity, new NonUniformScale { Value = newFullRayScale });
                }).Schedule();

            m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            */
        }
    }

}