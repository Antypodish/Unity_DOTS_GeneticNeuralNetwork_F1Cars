using UnityEngine ;
// using System.Collections;

using Unity.Jobs ;
using Unity.Physics ;
using Unity.Physics.Systems ;
using Unity.Entities ;
using Unity.Transforms ;
using Unity.Mathematics ;

// using Unity.Collections.LowLevel.Unsafe ;

// using Collider = Unity.Physics.Collider;
// using RaycastHit = Unity.Physics.RaycastHit;

namespace Antypodish.CopsAI.DOTS
{

    public class RayColliderTestSystem : SystemBase
    {

        // EndFixedStepSimulationEntityCommandBufferSystem efsecb ;

        BuildPhysicsWorld  buildPhysicsWorldSystem ;
        EndFramePhysicsSystem endFramePhysicsSystem ;

        protected override void OnCreate ( )
        {
            

            buildPhysicsWorldSystem = World.GetOrCreateSystem <BuildPhysicsWorld> () ;
            endFramePhysicsSystem = World.GetExistingSystem <EndFramePhysicsSystem> () ;

            // efsecb = World.GetExistingSystem <EndFixedStepSimulationEntityCommandBufferSystem>() ;

            // becb  = World.GetOrCreateSystem <BeginInitializationEntityCommandBufferSystem> () ;
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
            PhysicsWorld physicsWorld = buildPhysicsWorldSystem.PhysicsWorld ;
            CollisionWorld collisionWorld = physicsWorld.CollisionWorld;
            // EntityCommandBuffer commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();
            // Dependency = JobHandle.CombineDependencies(Dependency, m_EndFramePhysicsSystem.GetOutputDependency());
            
            Dependency = JobHandle.CombineDependencies ( Dependency, endFramePhysicsSystem.GetOutputDependency() ) ;

            /*
            Entities
                .WithName ( "RaycastWithCustomCollectorJob" )
                .WithBurst ()
            .ForEach ( ( Entity entity, ref Translation position, ref Rotation rotation ) =>
            {
                

                // var collector = new IgnoreTransparentClosestHitCollector ( collisionWorld ) ;

                // collisionWorld.CastRay ( raycastInput, ref collector ) ;

                //if ( collisionWorld.Bodies [hit.RigidBodyIndex].Collider, hit.ColliderKey )
                //{
                //    return false;
                //}

                // var collector = new IgnoreTransparentClosestHitCollector ( collisionWorld ) ;
               
                
                var raycastLength = 200 ;

                // Perform the Raycast
                var raycastInput = new RaycastInput
                {
                    Start = position.Value,
                    End = position.Value + ( math.forward (rotation.Value) * raycastLength ),
                    Filter = CollisionFilter.Default
                };

                collisionWorld.CastRay(raycastInput, ref collector);

                var hit = collector.ClosestHit;
                var hitDistance = raycastLength * hit.Fraction;

                Debug.LogWarning ( "Hit dist: " + hitDistance ) ;
                
                
            }).Schedule () ;
            */

            
            // Debug.Log ( "runs" ) ;
            // EntityCommandBuffer.ParallelWriter ecbp = becb.CreateCommandBuffer ().AsParallelWriter () ;

            
            // CollisionWorld collisionWorld = buildPhysicsWorldSystem.PhysicsWorld.CollisionWorld ;
            // Debug.DrawLine ( pointerRay.origin, pointerRay.direction * 200, Color.blue ) ;
            // RaycastInput raycastInput = new RaycastInput () { Start = pointerRay.origin, End = pointerRay.direction * 200, Filter = CollisionFilter.Default } ;

            // UnityEngine.Ray pointerRay = Camera.main.ScreenPointToRay ( Input.mousePosition ) ;
            Vector3 V3_point = Camera.main.ScreenToWorldPoint ( Input.mousePosition ) ;

            Debug.DrawLine ( V3_point, V3_point -Vector3.up * 200, Color.blue ) ;

            CollisionFilter collisionFilter = new CollisionFilter () 
            { 
                BelongsTo = default, // 1, 
                CollidesWith = 3 // 2 
            } ;
            RaycastInput raycastInput = new RaycastInput () 
            { 
                Start  = V3_point, 
                End    = V3_point -Vector3.up * 200, 
                Filter = collisionFilter 
            } ; 

            

            Unity.Physics.RaycastHit rayResult ;
            
            // Physics.Raycast
            if ( physicsWorld.CastRay ( raycastInput, out rayResult ) )
            {
                Unity.Transforms.Translation tr = EntityManager.GetComponentData <Unity.Transforms.Translation> ( rayResult.Entity ) ;

                Debug.LogWarning ( "Hit. " + rayResult.Entity + "; ray pos: " + rayResult.Position + "; entity pos: " + tr.Value ) ;
            }
            


            /*
            Entities
                .WithName ( "CreateCollidersJob" )
                .WithAll <CreateColliderTag> ()
                .ForEach ( ( Entity entity, int entityInQueryIndex, in Translation position, in CompositeScale scale, in Rotation q ) =>
            {

                float3 f3_scale = new float3 ( scale.Value.c0.x, scale.Value.c1.y, scale.Value.c2.z ) ;
                BlobAssetReference <Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create ( new BoxGeometry { Center = position.Value, Size = f3_scale, Orientation = q.Value }) ;

                PhysicsCollider physicsCollider = new PhysicsCollider () { Value = collider } ;

                ecbp.AddComponent ( entityInQueryIndex, entity, physicsCollider ) ;
                ecbp.RemoveComponent <CreateColliderTag> ( entityInQueryIndex, entity) ;

            }).Schedule ();

            becb.AddJobHandleForProducer ( Dependency ) ;
            */

        }

    }

    /*
    // This collector filters out bodies with transparent custom tag
    public struct IgnoreTransparentClosestHitCollector : ICollector <RaycastHit>
    {
        public bool EarlyOutOnFirstHit => false;

        public float MaxFraction {get; private set;}

        public int NumHits { get; private set; }

        public RaycastHit ClosestHit;

        private CollisionWorld m_World;
        private const int k_TransparentCustomTag = (1 << 1);

        public IgnoreTransparentClosestHitCollector ( CollisionWorld world )
        {
            m_World     = world;

            MaxFraction = 1.0f;
            ClosestHit  = default;
            NumHits     = 0;
        }
                
        private static bool IsTransparent ( BlobAssetReference<Collider> collider, ColliderKey key )
        {
            bool bIsTransparent = false;
            unsafe
            {
                // Only Convex Colliders have Materials associated with them. So base on CollisionType
                // we'll need to cast from the base Collider type, hence, we need the pointer.
                var c = (Collider*)collider.GetUnsafePtr();
                {
                    var cc = ((ConvexCollider*)c);

                    // We also need to check if our Collider is Composite (i.e. has children).
                    // If it is then we grab the actual leaf node hit by the ray.
                    // Checking if our collider is composite
                    if (c->CollisionType != CollisionType.Convex)
                    {
                        // If it is, get the leaf as a Convex Collider
                        c->GetLeaf(key, out ChildCollider child);
                        cc = (ConvexCollider*)child.Collider;
                    }

                    // Now we've definitely got a ConvexCollider so can check the Material.
                    // bIsTransparent = (cc->Material.CustomTags & k_TransparentCustomTag) != 0;

                    return true ;
                }
            }

            return false ;
            // return bIsTransparent;
        }
        
        public bool AddHit(RaycastHit hit)
        {
            
            if (IsTransparent(m_World.Bodies[hit.RigidBodyIndex].Collider, hit.ColliderKey))
            {
                return false;
            }

            MaxFraction = hit.Fraction;
            ClosestHit = hit;
            NumHits = 1;
            
            return true;
        }
        
    }
    */
}