using UnityEngine ;
using System ;
// using System.Collections ;

using Unity.Assertions ;
// using Unity.Collections.LowLevel.Unsafe;

using Unity.Jobs ;
using Unity.Burst ;
using Unity.Physics ;
using Unity.Physics.Systems ;
using Unity.Entities ;
using Unity.Transforms ;
using Unity.Collections ;
using Unity.Mathematics ;

using Antypodish.DOTS ;
using Antypodish.NueralNetwork.DOTS ;

namespace Antypodish.CopsAI.DOTS
{
     // Describes the overlap state.
    // OverlapState in StatefulTriggerEvent is set to:
    //    1) EventOverlapState.Enter, when 2 bodies are overlapping in the current frame,
    //    but they did not overlap in the previous frame
    //    2) EventOverlapState.Stay, when 2 bodies are overlapping in the current frame,
    //    and they did overlap in the previous frame
    //    3) EventOverlapState.Exit, when 2 bodies are NOT overlapping in the current frame,
    //    but they did overlap in the previous frame
    public enum EventOverlapState : byte
    {
        Enter,
        Stay,
        Exit
    }

    // Trigger Event that is stored inside a DynamicBuffer
    public struct StatefulTriggerEvent : IBufferElementData, IComparable<StatefulTriggerEvent>
    {
        internal EntityPair Entities;
        internal BodyIndexPair BodyIndices;
        internal ColliderKeyPair ColliderKeys;

        public EventOverlapState State;
        public Entity EntityA => Entities.EntityA;
        public Entity EntityB => Entities.EntityB;
        public int BodyIndexA => BodyIndices.BodyIndexA;
        public int BodyIndexB => BodyIndices.BodyIndexB;
        public ColliderKey ColliderKeyA => ColliderKeys.ColliderKeyA;
        public ColliderKey ColliderKeyB => ColliderKeys.ColliderKeyB;

        public StatefulTriggerEvent(Entity entityA, Entity entityB, int bodyIndexA, int bodyIndexB,
                                    ColliderKey colliderKeyA, ColliderKey colliderKeyB)
        {
            Entities = new EntityPair
            {
                EntityA = entityA,
                EntityB = entityB
            };
            BodyIndices = new BodyIndexPair
            {
                BodyIndexA = bodyIndexA,
                BodyIndexB = bodyIndexB
            };
            ColliderKeys = new ColliderKeyPair
            {
                ColliderKeyA = colliderKeyA,
                ColliderKeyB = colliderKeyB
            };
            State = default;
        }

        // Returns other entity in EntityPair, if provided with one
        public Entity GetOtherEntity(Entity entity)
        {
            Assert.IsTrue((entity == EntityA) || (entity == EntityB));
            int2 indexAndVersion = math.select(new int2(EntityB.Index, EntityB.Version),
                new int2(EntityA.Index, EntityA.Version), entity == EntityB);
            return new Entity
            {
                Index = indexAndVersion[0],
                Version = indexAndVersion[1]
            };
        }

        public int CompareTo(StatefulTriggerEvent other)
        {
            var cmpResult = EntityA.CompareTo(other.EntityA);
            if (cmpResult != 0)
            {
                return cmpResult;
            }

            cmpResult = EntityB.CompareTo(other.EntityB);
            if (cmpResult != 0)
            {
                return cmpResult;
            }

            if (ColliderKeyA.Value != other.ColliderKeyA.Value)
            {
                return ColliderKeyA.Value < other.ColliderKeyA.Value ? -1 : 1;
            }

            if (ColliderKeyB.Value != other.ColliderKeyB.Value)
            {
                return ColliderKeyB.Value < other.ColliderKeyB.Value ? -1 : 1;
            }

            return 0;
        }
    }

    [DisableAutoCreation]
    public class CollisionTriggersSystem : SystemBase
    {
        
        // BeginInitializationEntityCommandBufferSystem becb ;

        BuildPhysicsWorld buildPhysicsWorldSystem ;
        StepPhysicsWorld stepPhysicsWorld ;
 
        private NativeList <StatefulTriggerEvent> previousFrameTriggerEvents ;
        private NativeList <StatefulTriggerEvent> currentFrameTriggerEvents ;

        // private EndSimulationEntityCommandBufferSystem commandBufferSystem;
        
    // private ExportPhysicsWorld m_ExportPhysicsWorld;
    // private TriggerEventConversionSystem m_TriggerSystem;
        private EndFramePhysicsSystem endFramePhysicsSystem;
        
        protected override void OnCreate ( )
        {
            buildPhysicsWorldSystem       = World.GetOrCreateSystem <BuildPhysicsWorld> () ;
            stepPhysicsWorld              = World.GetOrCreateSystem <StepPhysicsWorld> () ;
            
            previousFrameTriggerEvents = new NativeList<StatefulTriggerEvent> ( Allocator.Persistent ) ;
            currentFrameTriggerEvents  = new NativeList<StatefulTriggerEvent> ( Allocator.Persistent ) ;

            endFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();

        // commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            // becb  = World.GetOrCreateSystem <BeginInitializationEntityCommandBufferSystem> () ;
            base.OnCreate ( ); 
        }

        // Use this for initialization
        // protected override void OnStartRunning ( )
        //{
        //    base.OnStartRunning ( );
        //}

        protected override void OnDestroy ( )
        {
            previousFrameTriggerEvents.Dispose();
            currentFrameTriggerEvents.Dispose();
        }

        // Update is called once per frame
        protected override void OnUpdate ( )
        {

// Debug.LogWarning ( "Does it run?" ) ;

            PhysicsWorld physicsWorld = buildPhysicsWorldSystem.PhysicsWorld ;            
            BufferFromEntity <StatefulTriggerEvent> a_triggerEvent = GetBufferFromEntity <StatefulTriggerEvent> () ;
            NativeHashMap<Entity, byte> nhm_entitiesWithBuffersMap = new NativeHashMap<Entity, byte> (0, Allocator.TempJob ) ;

            /*
            CollisionFilter collisionFilter = new CollisionFilter () 
            { 
                BelongsTo    = 1, 
                CollidesWith = 1 
            } ;
            */
            
            
            Entities
                .WithName( "ClearTriggerEventDynamicBuffersJobParallelJob" )
                .WithBurst()
                // .WithNone <ExcludeFromTriggerEventConversion> ()
                .ForEach ( ( ref DynamicBuffer <StatefulTriggerEvent> buffer ) =>
            {
                buffer.Clear();
            }).ScheduleParallel () ;

            SwapTriggerEventStates();

            // Dependency = JobHandle.CombineDependencies(m_ExportPhysicsWorld.GetOutputDependency(), Dependency ) ;
            // Dependency = JobHandle.CombineDependencies( stepPhysicsWorld.FinalSimulationJobHandle, Dependency ) ;

            
            var nl_currentFrameTriggerEvents = currentFrameTriggerEvents ;
            var nl_previousFrameTriggerEvents = previousFrameTriggerEvents ;

            JobHandle collectJobHandle = new TriggerCheckJob ()
            {
                nl_triggerEvents = nl_currentFrameTriggerEvents
            }.Schedule ( stepPhysicsWorld.Simulation, ref physicsWorld, Dependency ) ;

            // Dependency.Complete () ;
            JobHandle collectTriggerBuffersHandle = Entities
                .WithName ( "CollectTriggerBufferJob" )
                .WithBurst()
                // .WithNone <ExcludeFromTriggerEventConversion> ()
                .ForEach((Entity e, ref DynamicBuffer<StatefulTriggerEvent> buffer) =>
                {
                    nhm_entitiesWithBuffersMap.Add(e, 0);
                }).Schedule ( Dependency ) ;

            Dependency = JobHandle.CombineDependencies ( collectJobHandle, collectTriggerBuffersHandle ) ;


            Job
                .WithName ( "ConvertTriggerEventStreamToDynamicBufferJob" )
                .WithBurst()
                // .WithoutBurst ()
                .WithCode ( () =>
            {
                nl_currentFrameTriggerEvents.Sort ();

                var triggerEventsWithStates = new NativeList<StatefulTriggerEvent> ( nl_currentFrameTriggerEvents.Length, Allocator.Temp ) ;

                UpdateTriggerEventState ( nl_previousFrameTriggerEvents, nl_currentFrameTriggerEvents, triggerEventsWithStates ) ;
                AddTriggerEventsToDynamicBuffers ( triggerEventsWithStates, ref a_triggerEvent, nhm_entitiesWithBuffersMap ) ;
            }).Schedule () ;
            
            // Debug.Log ( "nl_triggerEvents: " + nl_currentFrameTriggerEvents.Length ) ;
            endFramePhysicsSystem.AddInputDependency ( Dependency ) ;
            nhm_entitiesWithBuffersMap.Dispose ( Dependency ) ;

            Dependency.Complete () ;
            

            // becb.AddJobHandleForProducer ( Dependency ) ;
            

        }

        [BurstCompile]
        private struct TriggerCheckJob : ITriggerEventsJob
        {
            public NativeList <StatefulTriggerEvent> nl_triggerEvents;

            public void Execute ( TriggerEvent triggerEvent )
            {
                // Debug.Log ( "trigger event: " + triggerEvent.EntityA + "; " + triggerEvent.EntityB ) ;

                nl_triggerEvents.Add (
                    new StatefulTriggerEvent 
                    (
                        triggerEvent.EntityA, 
                        triggerEvent.EntityB, 
                        triggerEvent.BodyIndexA, 
                        triggerEvent.BodyIndexB,
                        triggerEvent.ColliderKeyA, 
                        triggerEvent.ColliderKeyB
                    )
                ) ;

            }
        }

        protected void SwapTriggerEventStates()
        {
            var tmp = previousFrameTriggerEvents ;
            previousFrameTriggerEvents = currentFrameTriggerEvents ;
            currentFrameTriggerEvents = tmp;
            currentFrameTriggerEvents.Clear();
        }

        protected static void AddTriggerEventsToDynamicBuffers ( NativeList <StatefulTriggerEvent> nl_triggerEventList,
            ref BufferFromEntity <StatefulTriggerEvent> a_bufferFromEntity, NativeHashMap <Entity, byte> nhm_entitiesWithTriggerBuffers )
        {

            // Debug.Log ( "nhm: " + nl_triggerEventList.Length + " >> " + nhm_entitiesWithTriggerBuffers.Count () ) ;
            for (int i = 0; i < nl_triggerEventList.Length; i++)
            {
                var triggerEvent = nl_triggerEventList[i] ; 

                if ( nhm_entitiesWithTriggerBuffers.ContainsKey(triggerEvent.EntityA))
                {
                    a_bufferFromEntity[triggerEvent.EntityA].Add(triggerEvent);
                }
                if ( nhm_entitiesWithTriggerBuffers.ContainsKey(triggerEvent.EntityB))
                {
                    a_bufferFromEntity[triggerEvent.EntityB].Add(triggerEvent);
                }
            }
        }

        public static void UpdateTriggerEventState(NativeList<StatefulTriggerEvent> previousFrameTriggerEvents, NativeList<StatefulTriggerEvent> currentFrameTriggerEvents,
            NativeList<StatefulTriggerEvent> resultList)
        {
            int i = 0;
            int j = 0;

            while (i < currentFrameTriggerEvents.Length && j < previousFrameTriggerEvents.Length)
            {
                var currentFrameTriggerEvent = currentFrameTriggerEvents[i];
                var previousFrameTriggerEvent = previousFrameTriggerEvents[j];

                int cmpResult = currentFrameTriggerEvent.CompareTo(previousFrameTriggerEvent);

                // Appears in previous, and current frame, mark it as Stay
                if (cmpResult == 0)
                {
                    currentFrameTriggerEvent.State = EventOverlapState.Stay;
                    resultList.Add(currentFrameTriggerEvent);
                    i++;
                    j++;
                }
                else if (cmpResult < 0)
                {
                    // Appears in current, but not in previous, mark it as Enter
                    currentFrameTriggerEvent.State = EventOverlapState.Enter;
                    resultList.Add(currentFrameTriggerEvent);
                    i++;
                }
                else
                {
                    // Appears in previous, but not in current, mark it as Exit
                    previousFrameTriggerEvent.State = EventOverlapState.Exit;
                    resultList.Add(previousFrameTriggerEvent);
                    j++;
                }
            }

            if (i == currentFrameTriggerEvents.Length)
            {
                while (j < previousFrameTriggerEvents.Length)
                {
                    var triggerEvent = previousFrameTriggerEvents[j++];
                    triggerEvent.State = EventOverlapState.Exit;
                    resultList.Add(triggerEvent);
                }
            }
            else if (j == previousFrameTriggerEvents.Length)
            {
                while (i < currentFrameTriggerEvents.Length)
                {
                    var triggerEvent = currentFrameTriggerEvents[i++];
                    triggerEvent.State = EventOverlapState.Enter;
                    resultList.Add(triggerEvent);
                }
            }
        }
        
    }
}