using UnityEngine ;
// using System.Collections;

using Unity.Jobs ;
using Unity.Physics ;
using Unity.Entities ;
using Unity.Transforms ;
using Unity.Mathematics ;


namespace Antypodish.CopsAI.DOTS
{

    [DisableAutoCreation]
    public class CreateCollidersSystem : SystemBase
    {

        BeginInitializationEntityCommandBufferSystem becb ;

        protected override void OnCreate ( )
        {

            becb = World.GetOrCreateSystem <BeginInitializationEntityCommandBufferSystem> () ;
            base.OnCreate ( ); 
        }

        // Use this for initialization
        protected override void OnStartRunning ( )
        {
            base.OnStartRunning ( );
        }

        // Update is called once per frame
        protected override void OnUpdate ( )
        {


            EntityCommandBuffer.ParallelWriter ecbp = becb.CreateCommandBuffer ().AsParallelWriter () ;

            CollisionFilter collisionFilter = new CollisionFilter () ;
            collisionFilter.BelongsTo    = 1 ;
            collisionFilter.CollidesWith = 1 ;

            Entities
                .WithName ( "CreateBarrierCollidersJob" )
                .WithAll <CreateColliderTag, BarrierTag> ()
                .ForEach ( ( Entity entity, int entityInQueryIndex, in CompositeScale scale ) =>
            {

                float3 f3_scale = new float3 ( scale.Value.c0.x, scale.Value.c1.y, 1 ) ;
                BlobAssetReference <Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create ( new BoxGeometry { Center = 0, Size = f3_scale, Orientation = quaternion.identity }, collisionFilter ) ;
                
                PhysicsCollider physicsCollider = new PhysicsCollider () { Value = collider } ;

                ecbp.AddComponent ( entityInQueryIndex, entity, physicsCollider ) ;
                ecbp.RemoveComponent <CreateColliderTag> ( entityInQueryIndex, entity) ;

            }).ScheduleParallel () ;

            collisionFilter.BelongsTo    = 2 ;
            collisionFilter.CollidesWith = 1 ;

            Entities
                .WithName ( "CreateScoreCollidersJob" )
                .WithAll <CreateColliderTag, ScoreTag> ()
                .ForEach ( ( Entity entity, int entityInQueryIndex, in CompositeScale scale ) =>
            {

                float3 f3_scale = new float3 ( scale.Value.c0.x, scale.Value.c1.y, 1 ) ;
                BlobAssetReference <Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create ( new BoxGeometry { Center = 0, Size = f3_scale, Orientation = quaternion.identity }, collisionFilter ) ;
                
                PhysicsCollider physicsCollider = new PhysicsCollider () { Value = collider } ;

                ecbp.AddComponent ( entityInQueryIndex, entity, physicsCollider ) ;
                ecbp.RemoveComponent <CreateColliderTag> ( entityInQueryIndex, entity) ;

            }).ScheduleParallel () ;
            
            collisionFilter.BelongsTo    = 1 ; // 4
            collisionFilter.CollidesWith = 1 ;

            Entities
                .WithName ( "CreateVehicleCollidersJob" )
                .WithAll <CreateColliderTag, VehicleControllsComponent> ()
                .ForEach ( ( Entity entity, int entityInQueryIndex, in CompositeScale scale ) =>
            {

                float3 f3_scale = new float3 ( scale.Value.c0.x, scale.Value.c1.y, 1 ) ;
                BlobAssetReference <Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create ( new BoxGeometry { Center = 0, Size = f3_scale, Orientation = quaternion.identity }, collisionFilter ) ;
                
                PhysicsCollider physicsCollider = new PhysicsCollider () { Value = collider } ;

                ecbp.AddComponent ( entityInQueryIndex, entity, physicsCollider ) ;
                ecbp.RemoveComponent <CreateColliderTag> ( entityInQueryIndex, entity) ;

            }).ScheduleParallel () ;

            becb.AddJobHandleForProducer ( Dependency ) ;

        }
    }
}