using UnityEngine ;
using System.IO ;
using System.Collections.Generic ;
// using System.Collections;

using Unity.Jobs ;
using Unity.Burst ;
using Unity.Physics ;
using Unity.Entities ;
using Unity.Transforms ;
using Unity.Collections ;
using Unity.Mathematics ;

using Antypodish.DOTS ;
using Antypodish.GeneticNueralNetwork.DOTS ;


namespace Antypodish.GNNExample.DOTS
{

    [AlwaysUpdateSystem]
    [UpdateInGroup ( typeof ( GeneticNueralNetwork.DOTS.ManagerPreSimulationSystemGroup ) )]  
    [UpdateBefore ( typeof ( GeneticNueralNetwork.DOTS.ManagerTimerSystem ))]
    // [UpdateBefore ( typeof ( DNACrossOvereSystem ))]
    public class NNManagerSystem : SystemBase
    {
        
        string s_path = "Assets/Antypodish/NNExample/Resources/NNManagerBrains01.txt";
        
        BeginInitializationEntityCommandBufferSystem becb ;
        EndInitializationEntityCommandBufferSystem eecb ;

        EntityManager em ;

        EntityQuery group_MMMamager ;
        EntityQuery group_MMMamagerNotYetActive ;
        EntityQuery group_prefabs ;

        EntityQuery group_allPopulation ;
        EntityQuery group_firstPopulation ;
        EntityQuery group_need2InitializePopulation ;
        EntityQuery group_currentPopulation ;
        // EntityQuery group_currentPopulationNotYetMarkedAsFinished ;
        EntityQuery group_previousGeneration ;
        EntityQuery group_finishedPopulation ;

        EntityQuery group_carSpawnerPoint ;


        public struct Spawner
        {
            public float3 f3_position ;
            public quaternion q_rotation ;
        }

        LayersNeuronCounts layersNeuronCounts ;


        
        public ManagerMethods.JsonNeuralNetworkMangers jsonNeuralNetworkMangers ;

        
        private List <NNManagerSharedComponent> l_managerSharedData = new List <NNManagerSharedComponent> ( 1000 ) ;
        

        Unity.Mathematics.Random random ;




        protected override void OnCreate ( )
        {
            Debug.LogWarning ( "On Genetic Neural Network manager start." ) ;

            becb = World.GetOrCreateSystem <BeginInitializationEntityCommandBufferSystem> () ;
            eecb = World.GetOrCreateSystem <EndInitializationEntityCommandBufferSystem> () ;

            em   = World.EntityManager ;

            group_MMMamager = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),
               ComponentType.ReadOnly <NNManagerComponent> (),

               ComponentType.Exclude <NNMangerIsSpawningNewGenerationTag> ()
            ) ;
            
            
            group_MMMamagerNotYetActive = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsInitializedTag> (),
               ComponentType.ReadOnly <NNManagerComponent> (),

               
               ComponentType.Exclude <IsAliveTag> ()
            ) ;
            

            group_prefabs = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <SpawnerPrefabs_FromEntityData> ()
            ) ;

            group_carSpawnerPoint = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <CarSpawnerTag> ()
            ) ;
            
            group_allPopulation = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <NNBrainTag> (),

               ComponentType.ReadWrite <NNManagerSharedComponent> ()
            ) ;
            
            group_firstPopulation = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),

               ComponentType.ReadOnly <NNBrainTag> (),
               ComponentType.ReadOnly <NNIsFirstGenerationTag> (),

               ComponentType.ReadWrite <NNManagerSharedComponent> ()
            ) ;
            
            group_need2InitializePopulation = EntityManager.CreateEntityQuery
            (
               // ComponentType.ReadOnly <NNIsFinishedTag> (), ...

               ComponentType.ReadOnly <NNBrainTag> (),

               ComponentType.Exclude <IsAliveTag> (),
               ComponentType.Exclude <IsSpawningTag> (),
               ComponentType.Exclude <NNIsPreviousGenerationTag> (),

               ComponentType.ReadWrite <NNManagerSharedComponent> ()
            ) ;

            group_currentPopulation = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),
               // ComponentType.ReadOnly <NNIsFinishedTag> (), ...

               ComponentType.ReadOnly <NNBrainTag> (),
               ComponentType.Exclude <NNIsPreviousGenerationTag> (),

               ComponentType.ReadWrite <NNManagerSharedComponent> ()
            ) ;
            
            group_finishedPopulation = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),
               // ComponentType.ReadOnly <NNIsFinishedTag> (), ...

               ComponentType.ReadOnly <NNBrainTag> (),
               ComponentType.ReadOnly <NNIsFinishedTag> (),
               // ComponentType.ReadOnly <NNIsFinishedTag> (),
               ComponentType.Exclude <NNIsPreviousGenerationTag> (),
               ComponentType.Exclude <NNIsFirstGenerationTag> (),

               ComponentType.ReadWrite <NNManagerSharedComponent> ()
            ) ;

            group_previousGeneration = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),
               // ComponentType.ReadOnly <NNIsFinishedTag> (),

               ComponentType.ReadOnly <NNBrainTag> (),
               ComponentType.ReadOnly <NNIsPreviousGenerationTag> (),
               
               ComponentType.ReadWrite <NNManagerSharedComponent> ()
            ) ;

            
            _DefineLayersNuronsCount ( ref layersNeuronCounts ) ;
            
            random                                       = new Unity.Mathematics.Random ( (uint) System.DateTime.UtcNow.Millisecond + 5000 ) ;

            jsonNeuralNetworkMangers                     = new ManagerMethods.JsonNeuralNetworkMangers () ;

        }


        protected override void OnDestroy ( )
        {
        }




        // Update is called once per frame
        protected override void OnUpdate ( )
        {
                    
            // EntityCommandBuffer ecb0                 = becb.CreateCommandBuffer () ;
            // EntityCommandBuffer.ParallelWriter ecbp0 = ecb0.AsParallelWriter () ;
            
            // EntityCommandBuffer ecb1                 = eecb.CreateCommandBuffer () ;
            // EntityCommandBuffer.ParallelWriter ecbp1 = ecb1.AsParallelWriter () ;
            

            ComponentDataFromEntity <NNManagerComponent> a_manager ;
            
            
            if ( group_prefabs.CalculateChunkCount () == 0 ) return ; // Early exit.

            // Get prefabs.
            NativeArray <Entity> na_entities                                             = group_prefabs.ToEntityArray ( Allocator.Temp ) ;
            Entity prefabsEntity                                                         = na_entities [0] ;
            na_entities.Dispose () ;
            SpawnerPrefabs_FromEntityData spawner                                        = EntityManager.GetComponentData <SpawnerPrefabs_FromEntityData> ( prefabsEntity ) ;


            // Check none active managers.
            _CheckNoneActiveMangers ( this, Dependency, ref em, ref becb, ref jsonNeuralNetworkMangers, in spawner, in group_MMMamagerNotYetActive, layersNeuronCounts ) ;

            _ExecuteActiveManager ( this, Dependency, ref em, ref becb, ref eecb, ref random, ref l_managerSharedData, ref jsonNeuralNetworkMangers, in s_path, in spawner, in group_MMMamager, in group_carSpawnerPoint, in group_finishedPopulation, in group_allPopulation, in group_firstPopulation, in group_previousGeneration, in group_currentPopulation, in group_need2InitializePopulation, layersNeuronCounts ) ;

        }
        

        static private void _DefineLayersNuronsCount ( ref LayersNeuronCounts layersNeuronCounts )
        {
            layersNeuronCounts.i_inputLayerNeuronsCount  = 11 ; // Lidar inputs - ( lenght - 2 ). // length - 2 = forward speed // lenght - 1 = side wawy speed, skid.
            layersNeuronCounts.i_outputLayerNeuronsCount = 2 ; // Throtle, steering
            layersNeuronCounts.i_hiddenLayerNeuronsCount = (int) ( math.max ( layersNeuronCounts.i_inputLayerNeuronsCount, layersNeuronCounts.i_outputLayerNeuronsCount ) * 1f ) ;
            
        }


        static private void _ExecuteActiveManager ( SystemBase systemBase, JobHandle jobHandle, ref EntityManager em, ref BeginInitializationEntityCommandBufferSystem becb, ref EndInitializationEntityCommandBufferSystem eecb, ref Unity.Mathematics.Random random, ref List <NNManagerSharedComponent> l_managerSharedData, ref ManagerMethods.JsonNeuralNetworkMangers jsonNeuralNetworkMangers, in string s_path, in SpawnerPrefabs_FromEntityData spawner, in EntityQuery group_MMMamager, in EntityQuery group_carSpawnerPoint, in EntityQuery group_finishedPopulation, in EntityQuery group_allPopulation, in EntityQuery group_firstPopulation, in EntityQuery group_previousGeneration, in EntityQuery group_currentPopulation, in EntityQuery group_need2InitializePopulation, LayersNeuronCounts layersNeuronCounts )
        {

// Debug.LogWarning ( "Example managers: " + group_MMMamager.CalculateEntityCount () ) ;      
            if ( group_MMMamager.CalculateChunkCount () == 0 )
            {
                // Debug.LogWarning ( "There is no active managers yet." ) ;
                return ;
            }
      
            
            EntityCommandBuffer ecb0                 = becb.CreateCommandBuffer () ;
            EntityCommandBuffer.ParallelWriter ecbp0 = ecb0.AsParallelWriter () ;
            
            EntityCommandBuffer ecb1                 = eecb.CreateCommandBuffer () ;
            EntityCommandBuffer.ParallelWriter ecbp1 = ecb1.AsParallelWriter () ;



            random.NextUInt2 () ;
            // Unity.Mathematics.Random random = this.random ;

            // InvalidOperationException: 
            // The previously scheduled job ExportPhysicsWorld:ExportDynamicBodiesJob writes to the ComponentDataFromEntity<Unity.Transforms.Translation> ExportDynamicBodiesJob.JobData.PositionType. 
            // You must call JobHandle.Complete() on the job ExportPhysicsWorld:ExportDynamicBodiesJob, before you can read from the ComponentDataFromEntity<Unity.Transforms.Translation> safely.
            // Dependency.Complete () ;

// Debug.LogWarning ( "Test shared manger data count 1: " + l_managerSharedData.Count ) ;
            
            ComponentDataFromEntity <NNManagerComponent> a_manager                       = systemBase.GetComponentDataFromEntity <NNManagerComponent> ( false ) ;
            // NNManagerComponent manager ;
            // ComponentDataFromEntity <NNTimerComponent> a_managerTimer                    = GetComponentDataFromEntity <NNTimerComponent> ( false ) ;
            ComponentDataFromEntity <NNManagerBestFitnessComponent> a_managerBestFitness = systemBase.GetComponentDataFromEntity <NNManagerBestFitnessComponent> ( false ) ;
            ComponentDataFromEntity <NNScoreComponent> a_managerScore                    = systemBase.GetComponentDataFromEntity <NNScoreComponent> ( false ) ;
            ComponentDataFromEntity <IsTimeUpTag> a_isTimeUpTag                          = systemBase.GetComponentDataFromEntity <IsTimeUpTag> ( true ) ;
            
            NativeArray <Entity> na_managers                                             = group_MMMamager.ToEntityArray ( Allocator.Temp ) ;
            

            // Get cars spawner.    
            NativeArray <Spawner> na_spawnerPoints                                       = new NativeArray <Spawner> ( 1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory ) ;

            ComponentDataFromEntity <Translation> a_position                             = systemBase.GetComponentDataFromEntity <Translation> ( false ) ;
            ComponentDataFromEntity <Rotation> a_rotation                                = systemBase.GetComponentDataFromEntity <Rotation> ( false ) ;
            ComponentDataFromEntity <IsAliveTag> a_isAliveTag                            = systemBase.GetComponentDataFromEntity <IsAliveTag> ( true ) ;

            ComponentDataFromEntity <ShaderAlphaComponent> a_shaderAlpha                 = systemBase.GetComponentDataFromEntity <ShaderAlphaComponent> ( false ) ;
      
                        
            NativeArray <int> na_totalScore                                              = new NativeArray <int> ( 1, Allocator.Persistent, NativeArrayOptions.ClearMemory ) ;

            // BufferFromEntity <NNPNewPopulationBuffer> newPopulationBuffer                = GetBufferFromEntity <NNPNewPopulationBuffer> ( false ) ;
            
            int i_activeManager  = 0 ;
            bool isManagerActive = false ;

 // Debug.LogWarning ( "Example managers: " + l_managerSharedData.Count ) ;      

            l_managerSharedData.Clear () ;
            em.GetAllUniqueSharedComponentData ( l_managerSharedData ) ;        
            NativeArray <Entity> na_spawnerPointEntities = group_carSpawnerPoint.ToEntityArray ( Allocator.TempJob ) ;

            na_spawnerPoints.Dispose () ;
            na_spawnerPoints                             = new NativeArray <Spawner> ( na_spawnerPointEntities.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory ) ;


            // Ignore default manager entity ( index = 0, version = 0 ), taken from prefab entity.
            for ( int i = 0; i < l_managerSharedData.Count; i++ )
            {
                
                NNManagerSharedComponent mangerSharedComponent = l_managerSharedData [i] ;
                Entity managerEntity                           = new Entity () { Index = mangerSharedComponent.i_entityIndex, Version = mangerSharedComponent.i_entityVersion } ;
                
// Debug.Log ( "nnManagerEntity: " + managerEntity + "; " + a_isTimeUpTag.HasComponent ( managerEntity ) + "; " +  ManagerMethods._SkipInvalidManager ( managerEntity, ref a_isAliveTag ) ) ;

                // Entity manager must be valid and active.
                if ( ManagerMethods._SkipInvalidManager ( managerEntity, ref a_isAliveTag ) ) continue ;
                if ( !a_isTimeUpTag.HasComponent ( managerEntity ) ) continue ;


                NNManagerBestFitnessComponent managerBestFitness  = a_managerBestFitness [managerEntity] ;
                NNScoreComponent managerScore                     = a_managerScore [managerEntity] ;
                // NNTimerComponent managerTimer                     = a_managerTimer [managerEntity] ;
                
                NNManagerComponent manager                        = a_manager [managerEntity] ;
                

                group_finishedPopulation.SetSharedComponentFilter ( mangerSharedComponent ) ;

                

                group_allPopulation.SetSharedComponentFilter ( mangerSharedComponent ) ;
                group_firstPopulation.SetSharedComponentFilter ( mangerSharedComponent ) ;

                group_previousGeneration.SetSharedComponentFilter ( mangerSharedComponent ) ;
                group_currentPopulation.SetSharedComponentFilter ( mangerSharedComponent ) ;
                                    
                NativeArray <Entity> na_spawningNewGenerationEntities ;
                bool isNewGenerationValid = true ;


                if ( !isManagerActive )
                {
                    isManagerActive = true ;

                    
                    jobHandle = new GetCarsSpawnersJob ()
                    {

                        na_spawnerPointEntities = na_spawnerPointEntities,
                        na_spawnerPoints        = na_spawnerPoints,
                        
                        a_position              = a_position,
                        a_rotation              = a_rotation


                    }.Schedule ( na_spawnerPointEntities.Length, 256, jobHandle ) ;
            
                }

Debug.LogWarning ( "-------- group_need2InitializePopulation: " + group_need2InitializePopulation.CalculateEntityCount () ) ;


                if ( group_need2InitializePopulation.CalculateChunkCount () > 0 )
                {

Debug.Log ( "-------- Spawning first gen: " + group_need2InitializePopulation.CalculateEntityCount () ) ;
                    na_spawningNewGenerationEntities = group_need2InitializePopulation.ToEntityArray ( Allocator.TempJob ) ;

                }
                else if ( group_firstPopulation.CalculateChunkCount () > 0 )
                {
                        
Debug.Log ( "-------- First parents of second gen: " + group_firstPopulation.CalculateEntityCount () ) ;

                    _FirstParent ( systemBase, jobHandle, ref em, ref becb, ref ecbp0, ref a_shaderAlpha, ref jsonNeuralNetworkMangers, in spawner, in group_firstPopulation, in manager, layersNeuronCounts, i_activeManager, s_path, out na_spawningNewGenerationEntities ) ;
                  
                }
                else if ( group_currentPopulation.CalculateChunkCount () > 0 )
                {


Debug.Log ( "-------- Else next gen: " + group_firstPopulation.CalculateEntityCount () + "; current pop: " + group_currentPopulation.CalculateEntityCount ()  ) ;

                    // NN manager has already parents population.

                    _NextGeneration ( systemBase, jobHandle, ref becb, ref ecbp0, ref ecb0, ref random, ref a_shaderAlpha, ref jsonNeuralNetworkMangers, ref na_totalScore, ref a_manager, ref manager, ref managerScore, ref managerBestFitness, in group_previousGeneration, in group_finishedPopulation, in group_currentPopulation, i_activeManager, managerEntity, s_path, out na_spawningNewGenerationEntities ) ;

                }
                else
                {
                    // Expected to be never executed.

                    na_spawningNewGenerationEntities = new NativeArray<Entity> ( 1, Allocator.TempJob )  ;

Debug.LogWarning ( "Default" ) ;

                    isNewGenerationValid = false ;

                }  

                if ( isNewGenerationValid )
                {
                
                    NNGenerationCountComponent generationCount = em.GetComponentData <NNGenerationCountComponent> ( managerEntity ) ;
                    
                    generationCount.i ++ ;

                    ecb1.SetComponent ( managerEntity, generationCount ) ;
                    ecb1.SetComponent ( managerEntity, managerBestFitness ) ;
                    ecb1.SetComponent ( managerEntity, managerScore ) ;
                    ecb1.AddComponent <NNMangerIsSpawningNewGenerationTag> ( managerEntity ) ;

                    random.NextInt2 () ;

                    jobHandle = new SpawnCarsAtRandomPositionJob ( )
                    {
                        na_populationEntities       = na_spawningNewGenerationEntities,
                        a_position                  = systemBase.GetComponentDataFromEntity <Translation> ( false ),
                        a_rotation                  = systemBase.GetComponentDataFromEntity <Rotation> ( false ),
                        a_vehicleVelocity           = systemBase.GetComponentDataFromEntity <VehicleVelocityComponent> ( false ),
                        outputNeuronsValuesBuffer   = systemBase.GetBufferFromEntity <NNOutputNeuronsValuesBuffer> ( false ),
                        random                      = random, 
                        na_spawnerPoints            = na_spawnerPoints,
                        i_populationCount           = na_spawningNewGenerationEntities.Length
                        // f3_spawnPosition      = spawnerPosition.Value
                            
                    }.Schedule ( na_spawningNewGenerationEntities.Length, 256, jobHandle ) ;
                    

                    GeneticNueralNetwork.DOTS.ManagerJobsMethods._SpawningGeneration ( ref systemBase, ref jobHandle, ref ecbp1, in na_spawningNewGenerationEntities, managerEntity ) ;

                        
                    becb.AddJobHandleForProducer ( jobHandle ) ;
                    eecb.AddJobHandleForProducer ( jobHandle ) ;

                    jobHandle.Complete () ;

// Debug.LogWarning ( "Generation: " + generationCount.i + " with best fitness: " + managerBestFitness.i + "; for entity: " + managerBestFitness.entity + "; total socre: " + managerScore.i ) ;

                        
                    na_spawningNewGenerationEntities.Dispose () ;
                    
                }
                
                i_activeManager ++ ;

            } // for
            
            na_managers.Dispose () ;
            na_spawnerPointEntities.Dispose () ;
            na_spawnerPoints.Dispose () ;
            na_totalScore.Dispose () ;

        }
        
        
        static private void _FirstParent ( SystemBase systemBase, JobHandle jobHandle, ref EntityManager em, ref BeginInitializationEntityCommandBufferSystem becb, ref EntityCommandBuffer.ParallelWriter ecbp, ref ComponentDataFromEntity <ShaderAlphaComponent> a_shaderAlpha, ref ManagerMethods.JsonNeuralNetworkMangers jsonNeuralNetworkMangers, in SpawnerPrefabs_FromEntityData spawner, in EntityQuery group_firstPopulation, in NNManagerComponent manager, LayersNeuronCounts layersNeuronCounts, int i_activeManager, string s_path, out NativeArray <Entity> na_spawningNewGenerationEntities )
        {

            jobHandle = new GeneticNueralNetwork.DOTS.ManagerJobs.SetFirstGenerationAsAncestorsJob ()
            {
                ecbp                  = ecbp,
                na_populationEntities = group_firstPopulation.ToEntityArray ( Allocator.TempJob )

            }.Schedule ( group_firstPopulation.CalculateEntityCount (), 256, jobHandle ) ;
                        
            jobHandle = new DisableCarJob ()
            {
                ecbp                  = ecbp,
                na_populationEntities = group_firstPopulation.ToEntityArray ( Allocator.TempJob ),
                a_shaderAlpha         = a_shaderAlpha

            }.Schedule ( group_firstPopulation.CalculateEntityCount (), 256, jobHandle ) ;
                        
            becb.AddJobHandleForProducer ( jobHandle ) ;
            jobHandle.Complete () ;
                        
            ManagerMethods._ReadDNAFromFile ( systemBase, ref jsonNeuralNetworkMangers, in layersNeuronCounts, in group_firstPopulation, in manager, i_activeManager, s_path ) ;


            na_spawningNewGenerationEntities = em.Instantiate ( spawner.prefabCarEntity, manager.i_populationSize, Allocator.TempJob ) ;

        }


        static private void _NextGeneration ( SystemBase systemBase, JobHandle jobHandle, ref BeginInitializationEntityCommandBufferSystem becb, ref EntityCommandBuffer.ParallelWriter ecbp, ref EntityCommandBuffer ecb, ref Unity.Mathematics.Random random, ref ComponentDataFromEntity <ShaderAlphaComponent> a_shaderAlpha, ref ManagerMethods.JsonNeuralNetworkMangers jsonNeuralNetworkMangers, ref NativeArray <int> na_totalScore, ref ComponentDataFromEntity <NNManagerComponent> a_manager, ref NNManagerComponent manager, ref NNScoreComponent managerScore, ref NNManagerBestFitnessComponent managerBestFitness, in EntityQuery group_previousGeneration, in EntityQuery group_finishedPopulation, in EntityQuery group_currentPopulation, int i_activeManager, Entity managerEntity, string s_path, out NativeArray <Entity> na_spawningNewGenerationEntities )
        {

            // Increase life time duration, if applicable.
            if ( group_finishedPopulation.CalculateEntityCount () < manager.i_populationSize ) 
            {   
                manager.i_startLifeTime  += manager.i_incrementLifeTime ;
                manager.i_startLifeTime   = math.min ( manager.i_startLifeTime, manager.i_maxLifeTime ) ;
                a_manager [managerEntity] = manager ; // Set back ;
            }


            BufferFromEntity <NNINdexProbabilityBuffer> indexProbabilityBuffer = systemBase.GetBufferFromEntity <NNINdexProbabilityBuffer> ( false ) ;
            ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore       = systemBase.GetComponentDataFromEntity <NNBrainScoreComponent> ( true ) ;


            NativeArray <Entity> na_parentPopulationEntities                   = group_previousGeneration.ToEntityArray ( Allocator.TempJob ) ;
            NativeArray <Entity> na_currentPopulationEntities                  = group_currentPopulation.ToEntityArray ( Allocator.TempJob ) ; 
                    
            // NativeHashMap <int, bool> nhm_checkedEliteEntities              = new NativeHashMap <int, bool> ( i_perentageOfElites, Allocator.TempJob ) ;
                  

            GeneticNueralNetwork.DOTS.ManagerJobsMethods._NextGeneration ( ref systemBase, ref jobHandle, ref becb, ref ecbp, ref ecb, ref random, ref jsonNeuralNetworkMangers, ref na_totalScore, ref na_parentPopulationEntities, ref na_currentPopulationEntities, ref indexProbabilityBuffer, ref manager, ref managerBestFitness, ref managerScore, in a_brainScore, managerEntity, i_activeManager, s_path ) ;


            na_spawningNewGenerationEntities = new NativeArray <Entity> ( na_currentPopulationEntities.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory ) ;
            // Utilize exisiting entities. Prevent physics from regenerating colliders.
            // a_newPopulation.CopyFrom ( na_currentPopulationEntities.Reinterpret <> ) ;
            na_spawningNewGenerationEntities.CopyFrom ( na_currentPopulationEntities ) ;
                    
            na_currentPopulationEntities.Dispose () ;

            _ReuseBrains ( ref jobHandle, ref ecbp, ref a_shaderAlpha, in na_spawningNewGenerationEntities ) ;
     
            becb.AddJobHandleForProducer ( jobHandle ) ;
            jobHandle.Complete () ;

            // na_elities.Dispose () ;
            // na_parentSortedKeysWithDuplicates.Dispose () ;
            // na_currentSortedKeysWithDuplicates.Dispose () ; 

            // nhm_checkedEliteEntities.Dispose () ;
            // na_currentPopulationEntities.Dispose () ;
            // nmhm_parentEntitiesScore.Dispose () ;
            // nmhm_currentEntitiesScore.Dispose () ;

        }

        static private void _CheckNoneActiveMangers ( NNManagerSystem managerSystem, JobHandle jobHandle, ref EntityManager em, ref BeginInitializationEntityCommandBufferSystem becb, ref ManagerMethods.JsonNeuralNetworkMangers jsonNeuralNetworkMangers, in SpawnerPrefabs_FromEntityData spawner, in EntityQuery group_MMMamagerNotYetActive, LayersNeuronCounts layersNeuronCounts )
        {


            // Check none active managers.
            if ( group_MMMamagerNotYetActive.CalculateChunkCount () > 0 )
            {
                
                SystemBase systemBase = managerSystem ;

                EntityCommandBuffer ecb                 = becb.CreateCommandBuffer () ;
                EntityCommandBuffer.ParallelWriter ecbp = ecb.AsParallelWriter () ;
                
                NativeArray <Entity> na_notActiveManagers = group_MMMamagerNotYetActive.ToEntityArray ( Allocator.Temp ) ;
                
                // var layersNeuronCounts = layersNeuronCounts ;

                jobHandle = managerSystem.Entities
                    .WithName ( "NNResizeFirstGenerationBuffersJob" )
                    .WithAll <NNManagerComponent, IsInitializedTag> ()
                    .WithNone <IsAliveTag> ()
                    // .WithReadOnly ( layersNeuronCounts )
                    .ForEach ( ( ref NNLayersNeuronsCountComponent layersNeuronsCount ) =>
                {
                
                    layersNeuronsCount.i_inputLayerNeuronsCount  = layersNeuronCounts.i_inputLayerNeuronsCount ;
                    layersNeuronsCount.i_outputLayerNeuronsCount = layersNeuronCounts.i_outputLayerNeuronsCount ;
                    layersNeuronsCount.i_hiddenLayerNeuronsCount = layersNeuronCounts.i_hiddenLayerNeuronsCount ; 

                }).ScheduleParallel ( jobHandle ) ;
                


                becb.AddJobHandleForProducer ( jobHandle ) ;

                // InvalidOperationException: 
                // The previously scheduled job NNManagerSystem:<>c__DisplayClass_NNResizeFirstGenerationBuffersJob reads from the Unity.Entities.EntityTypeHandle <>c__DisplayClass_NNResizeFirstGenerationBuffersJob.safety. You must call JobHandle.Complete() on the job NNManagerSystem:<>c__DisplayClass_NNResizeFirstGenerationBuffersJob, before you can deallocate the Unity.Entities.EntityTypeHandle safely.
                jobHandle.Complete () ;

                
                for ( int i = 0; i < na_notActiveManagers.Length; i ++ )
                {
                    
                    Entity managerEntity                                   = na_notActiveManagers [i] ;
                    ComponentDataFromEntity <NNManagerComponent> a_manager = systemBase.GetComponentDataFromEntity <NNManagerComponent> ( true ) ;
                    NNManagerComponent manager                             = a_manager [managerEntity] ;

                    NativeArray <Entity> na_spawningNewGenerationEntities  = em.Instantiate ( spawner.prefabCarEntity, manager.i_populationSize, Allocator.TempJob ) ;
                    

                    DynamicBuffer <NNPNewPopulationBuffer> a_newPopulation = em.GetBuffer <NNPNewPopulationBuffer> ( managerEntity ) ;
                    a_newPopulation.ResizeUninitialized ( manager.i_populationSize ) ;

                    a_newPopulation.CopyFrom ( na_spawningNewGenerationEntities.Reinterpret <NNPNewPopulationBuffer> () ) ;

                    na_spawningNewGenerationEntities.Dispose () ;

                } // for
                
                
                BufferFromEntity <NNPNewPopulationBuffer> newPopulationBuffer = systemBase.GetBufferFromEntity <NNPNewPopulationBuffer> ( false ) ;
// UnityEngine.Debug.LogWarning ( "First gen 1: " + na_notActiveManagers.Length ) ;

                
                for ( int i = 0; i < na_notActiveManagers.Length; i ++ )
                {

// Debug.Log ( i + "; all population: " + group_allPopulation.CalculateEntityCount () ) ;

                        
                    Entity managerEntity                                   = na_notActiveManagers [i] ;
                    ComponentDataFromEntity <NNManagerComponent> a_manager = systemBase.GetComponentDataFromEntity <NNManagerComponent> ( true ) ;
                    NNManagerComponent manager                             = a_manager [managerEntity] ;
                    
                    
                    jsonNeuralNetworkMangers._AddAndInitializeManger ( 
                        ManagerMethods._ElitesCount ( in manager ), 
                        NewGenerationkIsSpawingSystem._Input2HiddenLayerWeightsCount ( layersNeuronCounts.i_inputLayerNeuronsCount, layersNeuronCounts.i_hiddenLayerNeuronsCount ), 
                        NewGenerationkIsSpawingSystem._Hidden2OutputLayerWeightsCount ( layersNeuronCounts.i_outputLayerNeuronsCount, layersNeuronCounts.i_hiddenLayerNeuronsCount ), 
                        layersNeuronCounts.i_hiddenLayerNeuronsCount,
                        ref jsonNeuralNetworkMangers.l_managers 
                    ) ;

                    ecb.AddComponent <IsAliveTag> ( managerEntity ) ;

                    DynamicBuffer <NNPNewPopulationBuffer> a_newPopulation = newPopulationBuffer [managerEntity] ;
                    
                    GeneticNueralNetwork.DOTS.ManagerJobsMethods._SetFirstGeneration ( ref systemBase, ref jobHandle, ref ecbp, in a_newPopulation, managerEntity ) ;
                    /*
            jobHandle = new ManagerJobs.SetFirstGenerationJob ()
            {

                ecbp                  = ecbp,
                na_populationEntities = a_newPopulation.Reinterpret <Entity> ().AsNativeArray ()

            }.Schedule ( a_newPopulation.Length, 256, jobHandle ) ;
                        
            jobHandle = new ManagerJobs.AssignManager2BrainJob ()
            {

                ecbp                   = ecbp,
                na_populationEntities = a_newPopulation.Reinterpret <Entity> ().AsNativeArray (),
                a_assignedToManager   = systemBase.GetComponentDataFromEntity <NNAssignedToManagerComponent> ( false ),
                nnManagerEntity       = managerEntity

            }.Schedule  ( a_newPopulation.Length, 256, jobHandle ) ;
                    */

// Debug.Log ( "new generation: " + na_spawningNewGenerationEntities.Length ) ;
                 
                } // for

                becb.AddJobHandleForProducer ( jobHandle ) ;
                jobHandle.Complete () ;

                na_notActiveManagers.Dispose () ;

            } 

        }


        static private void _ReuseBrains ( ref JobHandle jobHandle, ref EntityCommandBuffer.ParallelWriter ecbp, ref ComponentDataFromEntity <ShaderAlphaComponent> a_shaderAlpha, in NativeArray <Entity> na_spawningNewGenerationEntities )
        {

            
            jobHandle = new GeneticNueralNetwork.DOTS.ManagerJobs.ReuseBrainsJob ()
            {
                ecbp                  = ecbp,
                na_populationEntities = na_spawningNewGenerationEntities
                            
            }.Schedule ( na_spawningNewGenerationEntities.Length, 256, jobHandle ) ;
                        

            jobHandle = new ReuseBrainsJob ()
            {
                na_populationEntities = na_spawningNewGenerationEntities,
                a_shaderAlpha         = a_shaderAlpha
                            
            }.Schedule ( na_spawningNewGenerationEntities.Length, 256, jobHandle ) ;
                 

        }

    
        [BurstCompile]
        public struct GetCarsSpawnersJob : IJobParallelFor
        {

            [ReadOnly]
            public NativeArray <Entity> na_spawnerPointEntities ;
            
            [NativeDisableParallelForRestriction]
            public NativeArray <Spawner> na_spawnerPoints ;

            [ReadOnly]
            public ComponentDataFromEntity <Translation> a_position ;
            [ReadOnly]
            public ComponentDataFromEntity <Rotation> a_rotation ;

            public void Execute ( int i )
            {

                Entity spawnerEntity        = na_spawnerPointEntities [i] ;
                Translation spawnerPosition = a_position [spawnerEntity] ;
                Rotation spawnerRotation    = a_rotation [spawnerEntity] ;

                na_spawnerPoints [i]        = new Spawner () { f3_position = spawnerPosition.Value, q_rotation = spawnerRotation.Value } ;

            }

        }

        
        [BurstCompile]
        public struct DisableCarJob : IJobParallelFor
        {

            [NativeDisableParallelForRestriction]
            public EntityCommandBuffer.ParallelWriter ecbp ;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray <Entity> na_populationEntities ;
            
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity <ShaderAlphaComponent> a_shaderAlpha ;

            public void Execute ( int i )
            {

                Entity populationEntity          = na_populationEntities [i] ;

                a_shaderAlpha [populationEntity] = new ShaderAlphaComponent () { f = 0.2f } ; // Fade

                ecbp.AddComponent <PhysicsExclude> ( i, populationEntity ) ;

            }

        }


        [BurstCompile]
        struct SpawnCarsAtRandomPositionJob : IJobParallelFor
        {

            [ReadOnly]
            public NativeArray <Entity> na_populationEntities ;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity <Translation> a_position ;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity <Rotation> a_rotation ;
            
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity <VehicleVelocityComponent> a_vehicleVelocity ;
            
            [NativeDisableParallelForRestriction]
            public BufferFromEntity <NNOutputNeuronsValuesBuffer> outputNeuronsValuesBuffer ;
            

            public Unity.Mathematics.Random random ;
            
            [ReadOnly]
            public NativeArray <Spawner> na_spawnerPoints ;
            public int i_populationCount ;
            // public float3 f3_spawnPosition ;

            public void Execute ( int i )
            {

                Entity entity                                                     = na_populationEntities [i] ;

                int i_populationDistributionPerSpawn                              = (int) ( i_populationCount / (float) na_spawnerPoints.Length ) ;

                int i_spawnIndex                                                  = (int) ( i / (float) i_populationDistributionPerSpawn ) ;

                if ( i_spawnIndex >= na_spawnerPoints.Length ) return ; // Too many spawn points. Or uneaven number. Spawned entities count, should be multiplier, of spawners count.

                Spawner spawner                                                   = na_spawnerPoints [i_spawnIndex] ;
                float3 f3_spawnPosition                                           = spawner.f3_position ;

                a_position [entity]                                               = new Translation () { Value = f3_spawnPosition + new float3 ( random.NextFloat ( -0.3f, 0.3f ), 0, random.NextFloat ( -0.3f, 0.3f ) ) } ;
                a_rotation [entity]                                               = new Rotation () { Value = math.mul ( quaternion.RotateY ( math.radians ( -90 + random.NextFloat ( -10, 10) ) ), spawner.q_rotation ) } ; // rand.NextFloat ( math.PI * 2 ) ) ;
                a_vehicleVelocity [entity]                                        = new VehicleVelocityComponent () ; // Reset
                DynamicBuffer <NNOutputNeuronsValuesBuffer> a_outputNeuronsValues = outputNeuronsValuesBuffer [entity] ;

                for ( int j = 0; j < a_outputNeuronsValues.Length; j++ )
                { 
                    a_outputNeuronsValues [j] = new NNOutputNeuronsValuesBuffer () { f = 0 } ;
                } // for

            }

        }
        
        [BurstCompile]
        public struct ReuseBrainsJob : IJobParallelFor
        {
            
            [ReadOnly]
            public NativeArray <Entity> na_populationEntities ;
            
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity <ShaderAlphaComponent> a_shaderAlpha ;

            public void Execute ( int i )
            {

                Entity populationEntity          = na_populationEntities [i] ;

                a_shaderAlpha [populationEntity] = new ShaderAlphaComponent () { f = 1f } ; // Reset fade.

            }

        }

    }

}