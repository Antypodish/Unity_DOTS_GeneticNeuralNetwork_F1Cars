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


namespace Antypodish.AI.DOTS
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
        
        NativeArray <int> na_totalScore ;

        Unity.Mathematics.Random random ;




        protected override void OnCreate ( )
        {
            Debug.LogWarning ( "On Genetic Neural Network manager start." ) ;

            becb = World.GetOrCreateSystem <BeginInitializationEntityCommandBufferSystem> () ;
            eecb = World.GetOrCreateSystem <EndInitializationEntityCommandBufferSystem> () ;

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
            
            na_totalScore                                = new NativeArray <int> ( 1, Allocator.Persistent, NativeArrayOptions.ClearMemory ) ;

            random                                       = new Unity.Mathematics.Random ( (uint) System.DateTime.UtcNow.Millisecond + 5000 ) ;

            jsonNeuralNetworkMangers                     = new ManagerMethods.JsonNeuralNetworkMangers () ;

        }


        protected override void OnDestroy ( )
        {
            na_totalScore.Dispose () ;
        }


        // Update is called once per frame
        protected override void OnUpdate ( )
        {
            
// Debug.LogWarning ( "Run" ) ;   

            EntityCommandBuffer ecb0                 = becb.CreateCommandBuffer () ;
            EntityCommandBuffer.ParallelWriter ecbp0 = ecb0.AsParallelWriter () ;
            
            EntityCommandBuffer ecb1                 = eecb.CreateCommandBuffer () ;
            EntityCommandBuffer.ParallelWriter ecbp1 = ecb1.AsParallelWriter () ;
            

            ComponentDataFromEntity <NNManagerComponent> a_manager ;
            
            
            // Get prefabs.
            NativeArray <Entity> na_entities                                             = group_prefabs.ToEntityArray ( Allocator.Temp ) ;
            Entity prefabsEntity                                                         = na_entities [0] ;
            na_entities.Dispose () ;
            SpawnerPrefabs_FromEntityData spawner                                        = EntityManager.GetComponentData <SpawnerPrefabs_FromEntityData> ( prefabsEntity ) ;



            // Check managers.
            {
                
                var layersNeuronCounts = this.layersNeuronCounts ;

                Entities
                    .WithName ( "NNResizeFirstGenerationBuffersJob" )
                    .WithAll <NNManagerComponent, IsInitializedTag> ()
                    .WithNone <IsAliveTag> ()
                    // .WithReadOnly ( layersNeuronCounts )
                    .ForEach ( ( ref NNLayersNeuronsCountComponent layersNeuronsCount ) =>
                {
                
                    layersNeuronsCount.i_inputLayerNeuronsCount  = layersNeuronCounts.i_inputLayerNeuronsCount ;
                    layersNeuronsCount.i_outputLayerNeuronsCount = layersNeuronCounts.i_outputLayerNeuronsCount ;
                    layersNeuronsCount.i_hiddenLayerNeuronsCount = layersNeuronCounts.i_hiddenLayerNeuronsCount ; 

                }).ScheduleParallel () ;
                


                becb.AddJobHandleForProducer ( Dependency ) ;

                Dependency.Complete () ;


                NativeArray <Entity> na_notActiveManagers = group_MMMamagerNotYetActive.ToEntityArray ( Allocator.Temp ) ;
                

// UnityEngine.Debug.LogWarning ( "First gen 1: " + na_notActiveManagers.Length ) ;

                
                for ( int i = 0; i < na_notActiveManagers.Length; i ++ )
                {

Debug.Log ( i + "; all population: " + group_allPopulation.CalculateEntityCount () ) ;

                        
                    Entity managerEntity       = na_notActiveManagers [i] ;
                    a_manager                  = GetComponentDataFromEntity <NNManagerComponent> ( true ) ;
                    NNManagerComponent manager = a_manager [managerEntity] ;
                    
                    
                    jsonNeuralNetworkMangers._AddAndInitializeManger ( 
                        ManagerMethods._ElitesCount ( in manager ), 
                        NewGenerationkIsSpawingSystem._Input2HiddenLayerWeightsCount ( layersNeuronCounts.i_inputLayerNeuronsCount, layersNeuronCounts.i_hiddenLayerNeuronsCount ), 
                        NewGenerationkIsSpawingSystem._Hidden2OutputLayerWeightsCount ( layersNeuronCounts.i_outputLayerNeuronsCount, layersNeuronCounts.i_hiddenLayerNeuronsCount ), 
                        layersNeuronCounts.i_hiddenLayerNeuronsCount,
                        ref jsonNeuralNetworkMangers.l_managers 
                    ) ;

                    ecb0.AddComponent <IsAliveTag> ( managerEntity ) ;

                    NativeArray <Entity> na_spawningNewGenerationEntities = EntityManager.Instantiate ( spawner.prefabCarEntity, manager.i_populationSize, Allocator.TempJob ) ;
                       

                    Dependency = new GeneticNueralNetwork.DOTS.ManagerJobs.SetFirstGenerationJob ()
                    {

                        ecbp                  = ecbp0,
                        na_populationEntities = na_spawningNewGenerationEntities

                    }.Schedule ( na_spawningNewGenerationEntities.Length, 256, Dependency ) ;
                        
                    Dependency = new GeneticNueralNetwork.DOTS.ManagerJobs.AssignManager2BrainJob ()
                    {

                        ecbp                   = ecbp0,
                        na_populationEntities = na_spawningNewGenerationEntities,
                        a_assignedToManager   = GetComponentDataFromEntity <NNAssignedToManagerComponent> ( false ),
                        nnManagerEntity       = managerEntity

                    }.Schedule  ( na_spawningNewGenerationEntities.Length, 256, Dependency ) ;
                        

                    becb.AddJobHandleForProducer ( Dependency ) ;
                    Dependency.Complete () ;

// Debug.Log ( "new generation: " + na_spawningNewGenerationEntities.Length ) ;

                    na_spawningNewGenerationEntities.Dispose () ;
                 
                } // for

                na_notActiveManagers.Dispose () ;

                l_managerSharedData.Clear () ;
                EntityManager.GetAllUniqueSharedComponentData ( l_managerSharedData ) ;
            } 
                 



// Debug.LogWarning ( "Example managers: " + group_MMMamager.CalculateEntityCount () ) ;      
            if ( group_MMMamager.CalculateChunkCount () == 0 )
            {
                // Debug.LogWarning ( "There is no active managers yet." ) ;
                return ;
            }

            this.random.NextUInt2 () ;
            Unity.Mathematics.Random random = this.random ;

            // InvalidOperationException: 
            // The previously scheduled job ExportPhysicsWorld:ExportDynamicBodiesJob writes to the ComponentDataFromEntity<Unity.Transforms.Translation> ExportDynamicBodiesJob.JobData.PositionType. 
            // You must call JobHandle.Complete() on the job ExportPhysicsWorld:ExportDynamicBodiesJob, before you can read from the ComponentDataFromEntity<Unity.Transforms.Translation> safely.
            // Dependency.Complete () ;

// Debug.LogWarning ( "Test shared manger data count 1: " + l_managerSharedData.Count ) ;
            
            a_manager                                                                    = GetComponentDataFromEntity <NNManagerComponent> ( false ) ;
            // NNManagerComponent manager ;
            // ComponentDataFromEntity <NNTimerComponent> a_managerTimer                    = GetComponentDataFromEntity <NNTimerComponent> ( false ) ;
            ComponentDataFromEntity <NNManagerBestFitnessComponent> a_managerBestFitness = GetComponentDataFromEntity <NNManagerBestFitnessComponent> ( false ) ;
            ComponentDataFromEntity <NNScoreComponent> a_managerScore                    = GetComponentDataFromEntity <NNScoreComponent> ( false ) ;
            ComponentDataFromEntity <IsTimeUpTag> a_isTimeUpTag                          = GetComponentDataFromEntity <IsTimeUpTag> ( false ) ;
            
            NativeArray <Entity> na_managers                                             = group_MMMamager.ToEntityArray ( Allocator.Temp ) ;
            

            // Get cars spawner.    
            NativeArray <Spawner> na_spawnerPoints                                       = new NativeArray <Spawner> ( 1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory ) ;

            ComponentDataFromEntity <Translation> a_position                             = GetComponentDataFromEntity <Translation> ( false ) ;
            ComponentDataFromEntity <Rotation> a_rotation                                = GetComponentDataFromEntity <Rotation> ( false ) ;
            ComponentDataFromEntity <IsAliveTag> a_isAliveTag                            = GetComponentDataFromEntity <IsAliveTag> ( true ) ;

            ComponentDataFromEntity <ShaderAlphaComponent> a_shaderAlpha                 = GetComponentDataFromEntity <ShaderAlphaComponent> ( false ) ;
            
            
            int i_activeManager  = 0 ;
            bool isManagerActive = false ;

// Debug.LogWarning ( "Example managers: " + l_managerSharedData.Count ) ;      

            l_managerSharedData.Clear () ;
            EntityManager.GetAllUniqueSharedComponentData ( l_managerSharedData ) ;


            // Ignore default manager entity ( index = 0, version = 0 ), taken from prefab entity.
            for ( int i = 0; i < l_managerSharedData.Count; i++ )
            {
                
                NNManagerSharedComponent mangerSharedComponent = l_managerSharedData [i] ;
                Entity managerEntity                           = new Entity () { Index = mangerSharedComponent.i_entityIndex, Version = mangerSharedComponent.i_entityVersion } ;
                
// Debug.Log ( "nnManagerEntity: " + managerEntity ) ;

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

                        
                    // Get cars spawner.    
                    NativeArray <Entity> na_spawnerPointEntities = group_carSpawnerPoint.ToEntityArray ( Allocator.Temp ) ;
                    na_spawnerPoints.Dispose () ;
                    na_spawnerPoints                             = new NativeArray <Spawner> ( na_spawnerPointEntities.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory ) ;
                        
                    for ( int j = 0; j < na_spawnerPoints.Length; j++ )
                    {

                        Entity spawnerEntity                     = na_spawnerPointEntities [j] ;
                        Translation spawnerPosition              = a_position [spawnerEntity] ;
                        Rotation spawnerRotation                 = a_rotation [spawnerEntity] ;

                        na_spawnerPoints [j]                     = new Spawner () { f3_position = spawnerPosition.Value, q_rotation = spawnerRotation.Value } ;

                    }

                    na_spawnerPointEntities.Dispose () ;
            
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

                    Dependency = new GeneticNueralNetwork.DOTS.ManagerJobs.SetFirstGenerationAsAncestorsJob ()
                    {

                            ecbp                   = ecbp0,
                            na_populationEntities = group_firstPopulation.ToEntityArray ( Allocator.TempJob )

                    }.Schedule ( group_firstPopulation.CalculateEntityCount (), 256, Dependency ) ;
                        
                    Dependency = new AddPhysicsTagsJob ()
                    {

                            ecbp                  = ecbp0,
                            na_populationEntities = group_firstPopulation.ToEntityArray ( Allocator.TempJob ),
                            a_shaderAlpha         = a_shaderAlpha

                    }.Schedule ( group_firstPopulation.CalculateEntityCount (), 256, Dependency ) ;
                        
                    becb.AddJobHandleForProducer ( Dependency ) ;
                    Dependency.Complete () ;
                        
                    ManagerMethods._ReadDNAFromFile ( this, ref jsonNeuralNetworkMangers, in layersNeuronCounts, in group_firstPopulation, in manager, i_activeManager, s_path ) ;


                    na_spawningNewGenerationEntities = EntityManager.Instantiate ( spawner.prefabCarEntity, manager.i_populationSize, Allocator.TempJob ) ;

                }
                else if ( group_currentPopulation.CalculateChunkCount () > 0 )
                {


Debug.Log ( "-------- Else next gen: " + group_firstPopulation.CalculateEntityCount () + "; current pop: " + group_currentPopulation.CalculateEntityCount ()  ) ;

                    // NN manager has already parents population.


// Debug.Log ( "finished population: " + group_finishedPopulation.CalculateEntityCount () + " / " + manager.i_populationSize ) ;

                    // Increase life time duration.
                    if ( group_finishedPopulation.CalculateEntityCount () < manager.i_populationSize ) 
                    {   
                        manager.i_startLifeTime += manager.i_incrementLifeTime ;
                        manager.i_startLifeTime           = math.min ( manager.i_startLifeTime, manager.i_maxLifeTime ) ;
                        a_manager [managerEntity]       = manager ; // Set back ;
                    }


                    BufferFromEntity <NNINdexProbabilityBuffer> indexProbabilityBuffer = GetBufferFromEntity <NNINdexProbabilityBuffer> ( false ) ;
                    ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore       = GetComponentDataFromEntity <NNBrainScoreComponent> ( true ) ;


                    NativeArray <Entity> na_parentPopulationEntities                   = group_previousGeneration.ToEntityArray ( Allocator.TempJob ) ;
                    NativeArray <Entity> na_currentPopulationEntities                  = group_currentPopulation.ToEntityArray ( Allocator.TempJob ) ; 
                    
                    // NativeHashMap <int, bool> nhm_checkedEliteEntities              = new NativeHashMap <int, bool> ( i_perentageOfElites, Allocator.TempJob ) ;
                  
                    NativeMultiHashMap <int, EntityIndex> nmhm_parentEntitiesScore    = new NativeMultiHashMap <int, EntityIndex> ( na_currentPopulationEntities.Length, Allocator.TempJob ) ;
                    NativeMultiHashMap <int, EntityIndex> nmhm_currentEntitiesScore   = new NativeMultiHashMap <int, EntityIndex> ( na_parentPopulationEntities.Length, Allocator.TempJob ) ;

Debug.Log ( "Manager scores" ) ;
                    Dependency = new DNACrossOvereSystem.GetPopulationScoreJob ( )
                    {
                        canGetEachScore              = true,
                        na_populationEntities        = na_parentPopulationEntities, 
                        a_brainScore                 = a_brainScore, 

                        nmhm_populationEntitiesScore = nmhm_parentEntitiesScore.AsParallelWriter ()

                    }.Schedule ( na_parentPopulationEntities.Length, 256, Dependency ) ;
                        
                    Dependency = new DNACrossOvereSystem.GetPopulationScoreJob ( )
                    {
                        canGetEachScore              = false,
                        na_populationEntities        = na_currentPopulationEntities, 
                        a_brainScore                 = a_brainScore, 

                        nmhm_populationEntitiesScore = nmhm_currentEntitiesScore.AsParallelWriter ()

                    }.Schedule ( na_currentPopulationEntities.Length, 256, Dependency ) ;

                    Dependency = new GeneticNueralNetwork.DOTS.ManagerJobs.CalculateTotalScoresOfPopulationJob ()
                    {

                        na_populationEntities = na_currentPopulationEntities,
                        na_totalScore         = na_totalScore,

                        a_brainScore          = a_brainScore

                    }.Schedule ( Dependency ) ;


                    Dependency.Complete () ;


                    int i_currentPopulationTotalScore = na_totalScore [0] ;
                    // managerScore.i                 = i_currentPopulationTotalScore ;
                    int i_totalElitesScoreTemp        = (int) ( i_currentPopulationTotalScore * manager.f_eliteSize ) ;
                    int i_currentPopulationTemp       = (int) ( na_currentPopulationEntities.Length * manager.f_eliteSize ) ;
                    int i_totalElitesScore            = i_currentPopulationTotalScore <= i_currentPopulationTemp ? i_currentPopulationTotalScore : i_totalElitesScoreTemp ;
                        
Debug.Log ( "Current total score of pop: " + i_currentPopulationTotalScore + "; elite score: " + i_totalElitesScore ) ;
                        
                    NativeArray <int> na_parentSortedKeysWithDuplicates = nmhm_parentEntitiesScore.GetKeyArray ( Allocator.TempJob ) ;
                    // This stores key keys in order. But keeps first unique keys at the front of an array.
                    // Total array size matches of total elements.
                    na_parentSortedKeysWithDuplicates.Sort () ;
                    // Sorted.
                    int i_parentUniqueKeyCount        = na_parentSortedKeysWithDuplicates.Unique () ;

// Debug.LogError ( "sorted keys: " + na_parentSortedKeysWithDuplicates.Length ) ;

                        
                    NativeArray <int> na_currentSortedKeysWithDuplicates = nmhm_currentEntitiesScore.GetKeyArray ( Allocator.TempJob ) ;
                    // This stores key keys in order. But keeps first unique keys at the front of an array.
                    // Total array size matches of total elements.
                    na_currentSortedKeysWithDuplicates.Sort () ;
                    // Sorted.
                    int i_uniqueKeyCount              = na_currentSortedKeysWithDuplicates.Unique () ;

                    int i_eltieCountTemp              = (int) ( na_currentSortedKeysWithDuplicates.Length * manager.f_eliteSize ) ;
                    // Minimum elite size mus be met.
                    int i_eltiesCount                 = i_eltieCountTemp <= i_totalElitesScoreTemp ? na_currentSortedKeysWithDuplicates.Length : i_eltieCountTemp ;


/*
Debug.LogWarning ( "parent" ) ; // temp: " + i_eltieCountTemp + "; elit count: " + i_eltiesCount ) ;
var valarr = nmhm_parentEntitiesScore.GetValueArray ( Allocator.Temp ) ;

for ( int j = 0; j < valarr.Length; j ++ )
{
Debug.Log ( i + " / " + valarr.Length + "; e: " + valarr [j].entity + "; " + valarr [j].i_index + "; score: " + a_brainScore [valarr [j].entity].f ) ;
} // for

Debug.LogWarning ( "elit temp: " + i_eltieCountTemp + "; elit count: " + i_eltiesCount ) ;
valarr = nmhm_currentEntitiesScore.GetValueArray ( Allocator.Temp ) ;

for ( int j = 0; j < valarr.Length; j ++ )
{
Debug.Log ( i + " / " + valarr.Length + "; e: " + valarr [j].entity + "; " + valarr [j].i_index + "; score: " + a_brainScore [valarr [j].entity].f ) ;
} // for
                        
*/

                    NativeArray <EntityIndex> na_elities                = new NativeArray <EntityIndex> ( i_eltiesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory ) ;

                    Dependency = new DNACrossOvereSystem.GetElitesEntitiesJob ()
                    {

                        i_eltiesCount                      = i_eltiesCount,

                        na_elities                         = na_elities,
                        nmhm_entitiesScore                 = nmhm_currentEntitiesScore,
                        na_currentSortedKeysWithDuplicates = na_currentSortedKeysWithDuplicates

                    }.Schedule () ;

//                    Dependency.Complete () ;
                        

                    DynamicBuffer <NNINdexProbabilityBuffer> a_currentEliteIndexProbability = indexProbabilityBuffer [managerEntity] ;

                    a_currentEliteIndexProbability.ResizeUninitialized ( i_totalElitesScore ) ;



Debug.Log ( "current pop total score: " + i_currentPopulationTotalScore + "; total elite score: " + i_totalElitesScore + "; elites count: " + i_eltiesCount + " of current population: " + na_currentPopulationEntities.Length ) ;
                        
                    // Grab % ellites selection from the probability group.
                    if ( a_currentEliteIndexProbability.Length > 0 )
                    {

// Debug.LogError ( "parent keys with duplicates: " + na_parentSortedKeysWithDuplicates.Length ) ;

                        Dependency = new GeneticNueralNetwork.DOTS.ManagerJobs.InjectEllites2ParrentsJob ( )
                        {

                            ecb                                 = ecb0,

                            // i_currentElitesCount                = i_eltiesCount,
                                
                            na_elities                          = na_elities,
                            na_currentPopulationEntities        = na_currentPopulationEntities,
                            na_parentPopulationEntities         = na_parentPopulationEntities,


                            // nhm_checkedEliteEntities         = new NativeHashMap <int, bool> ( i_perentageOfElites, Allocator.TempJob ),
                            nmhm_parentEntitiesScore            = nmhm_parentEntitiesScore,
                            na_parentKeysWithDuplicates         = na_parentSortedKeysWithDuplicates,

                            na_currentEliteIndexProbability     = a_currentEliteIndexProbability.AsNativeArray (),

                            a_brainScore                        = a_brainScore,
                            
                            random                              = this.random

                        }.Schedule ( Dependency ) ;
                            
                        // Dependency.Complete () ;
                            
                        Dependency = new GeneticNueralNetwork.DOTS.ManagerJobs.SeParentGenerationJob ()
                        {

                            ecbp                  = ecbp0,
                            na_populationEntities = na_parentPopulationEntities
                            
                        }.Schedule ( na_parentPopulationEntities.Length, 256, Dependency ) ;

                        

                    }

                    // becb.AddJobHandleForProducer ( Dependency ) ;

                    // Dependency.Complete () ;

         
                    /*
// Test.                        
{
                            
// group_previousGeneration.SetSharedComponentFilter ( mangerSharedComponent ) ;

// var a = na_parentPopulation ;
    

for ( int j = 0; j < na_parentPopulation.Length; j ++ )
{
    Debug.Log ( j + " / " + na_parentPopulation.Length + "; parent entity " + na_parentPopulation [j] ) ;
}
    
for ( int j = 0; j < na_currentPopulationEntities.Length; j ++ )
{
    Debug.Log ( j + " / " + na_currentPopulationEntities.Length + "; current entity " + na_currentPopulationEntities [j] ) ;
}

}
*/

                    Dependency = new GeneticNueralNetwork.DOTS.ManagerJobs.CalculateTotalScoresOfPopulationJob ()
                    {

                            na_populationEntities = na_parentPopulationEntities,
                            na_totalScore         = na_totalScore,

                            a_brainScore          = a_brainScore

                    }.Schedule ( Dependency ) ;
                        
                        
                    becb.AddJobHandleForProducer ( Dependency ) ;
                    Dependency.Complete () ;

                    // Utilize exisiting entities. Prevent physics from regenerating colliders.

// Debug.Log ( "Score again: " + managerScore.i + " >> " + na_totalScore [0] + " of parents count: " + na_parentPopulation.Length ) ;
                    int i_totalScore     = na_totalScore [0] ;
                    managerScore.i       = i_totalScore ;
                    managerScore.i_elite = i_totalElitesScore ; 
                    
                    int i_bestEntityIndex = na_parentSortedKeysWithDuplicates [i_parentUniqueKeyCount-1] ;
                    nmhm_parentEntitiesScore.TryGetFirstValue ( i_bestEntityIndex, out EntityIndex entityIndex, out NativeMultiHashMapIterator <int> it ) ;

                    managerBestFitness.i = a_brainScore [entityIndex.entity].i ;
                    managerBestFitness.entity = entityIndex.entity ;

                    // Save elite brains.
                    ManagerMethods._SaveDNA2File ( this, in jsonNeuralNetworkMangers, in manager, in na_parentPopulationEntities, i_activeManager, s_path ) ;

                    na_parentPopulationEntities.Dispose () ;


// Debug.Log ( "copy: " + na_currentPopulationEntities.Length ) ;                        
                    na_spawningNewGenerationEntities = new NativeArray <Entity> ( na_currentPopulationEntities.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory ) ;
                    // Utilize exisiting entities. Prevent physics from regenerating colliders.
                    na_spawningNewGenerationEntities.CopyFrom ( na_currentPopulationEntities ) ;

                        
// for ( int j = 0; j < na_spawningNewGenerationEntities.Length; j ++ )
// {
// Debug.Log ( j + " / " + na_spawningNewGenerationEntities.Length + "; parent entity " + na_spawningNewGenerationEntities [j] ) ;

// if ( !EntityManager.Exists ( na_spawningNewGenerationEntities [j] ) ) Debug.LogError ( "Not extists!" ) ;
// } // for


                    Dependency = new GeneticNueralNetwork.DOTS.ManagerJobs.ReuseEntitiesJob ()
                    {

                        ecbp                  = ecbp0,
                        na_populationEntities = na_spawningNewGenerationEntities
                            
                    }.Schedule ( na_spawningNewGenerationEntities.Length, 256, Dependency ) ;
                        

                    Dependency = new ReuseEntitiesJob ()
                    {
                        na_populationEntities = na_spawningNewGenerationEntities,
                        a_shaderAlpha         = a_shaderAlpha
                            
                    }.Schedule ( na_spawningNewGenerationEntities.Length, 256, Dependency ) ;
                        
                    becb.AddJobHandleForProducer ( Dependency ) ;
                    Dependency.Complete () ;
                       
                    na_elities.Dispose () ;
                    na_parentSortedKeysWithDuplicates.Dispose () ;
                    na_currentSortedKeysWithDuplicates.Dispose () ; 
                        
                    // nhm_checkedEliteEntities.Dispose () ;
                    na_currentPopulationEntities.Dispose () ;
                    nmhm_parentEntitiesScore.Dispose () ;
                    nmhm_currentEntitiesScore.Dispose () ;


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
                
                    NNGenerationCountComponent generationCount = EntityManager.GetComponentData <NNGenerationCountComponent> ( managerEntity ) ;
                    
                    generationCount.i ++ ;

                    ecb1.SetComponent ( managerEntity, generationCount ) ;
                    ecb1.SetComponent ( managerEntity, managerBestFitness ) ;
                    ecb1.SetComponent ( managerEntity, managerScore ) ;
                    ecb1.AddComponent <NNMangerIsSpawningNewGenerationTag> ( managerEntity ) ;

                    this.random.NextInt2 () ;

                    Dependency = new SpawnCarsAtRandomPositionJob ( )
                    {

                        na_populationEntities       = na_spawningNewGenerationEntities,
                        a_position                  = GetComponentDataFromEntity <Translation> ( false ),
                        a_rotation                  = GetComponentDataFromEntity <Rotation> ( false ),
                        a_vehicleVelocity           = GetComponentDataFromEntity <VehicleVelocityComponent> ( false ),
                        outputNeuronsValuesBuffer   = GetBufferFromEntity <NNOutputNeuronsValuesBuffer> ( false ),
                        random                      = this.random, 
                        na_spawnerPoints            = na_spawnerPoints,
                        i_populationCount           = na_spawningNewGenerationEntities.Length
                        // f3_spawnPosition      = spawnerPosition.Value
                            
                    }.Schedule ( na_spawningNewGenerationEntities.Length, 256, Dependency ) ;
                    
                    Dependency = new GeneticNueralNetwork.DOTS.ManagerJobs.AssignManager2BrainJob ()
                    {

                        ecbp                  = ecbp1,
                        na_populationEntities = na_spawningNewGenerationEntities,
                        a_assignedToManager   = GetComponentDataFromEntity <NNAssignedToManagerComponent> ( false ),
                        nnManagerEntity       = managerEntity
                            
                    }.Schedule  ( na_spawningNewGenerationEntities.Length, 256, Dependency ) ;
                    
                    Dependency = new GeneticNueralNetwork.DOTS.ManagerJobs.IsSpawningNownJob ()
                    {

                        ecbp                  = ecbp1,
                        na_populationEntities = na_spawningNewGenerationEntities
                            
                    }.Schedule  ( na_spawningNewGenerationEntities.Length, 256, Dependency ) ;

                        
                    becb.AddJobHandleForProducer ( Dependency ) ;
                    eecb.AddJobHandleForProducer ( Dependency ) ;

                    Dependency.Complete () ;



                    

// Debug.LogWarning ( "Generation: " + generationCount.i + " with best fitness: " + managerBestFitness.i + "; for entity: " + managerBestFitness.entity + "; total socre: " + managerScore.i ) ;

                        
                    na_spawningNewGenerationEntities.Dispose () ;
                    
                }
                
                i_activeManager ++ ;

            } // for
            
            na_managers.Dispose () ;
            na_spawnerPoints.Dispose () ;

        }
        
        static private void _DefineLayersNuronsCount ( ref LayersNeuronCounts layersNeuronCounts )
        {
            layersNeuronCounts.i_inputLayerNeuronsCount  = 11 ; // Lidar inputs - ( lenght - 2 ). // length - 2 = forward speed // lenght - 1 = side wawy speed, skid.
            layersNeuronCounts.i_outputLayerNeuronsCount = 2 ; // Throtle, steering
            layersNeuronCounts.i_hiddenLayerNeuronsCount = (int) ( math.max ( layersNeuronCounts.i_inputLayerNeuronsCount, layersNeuronCounts.i_outputLayerNeuronsCount ) * 1f ) ;
            
        }

        
        [BurstCompile]
        public struct AddPhysicsTagsJob : IJobParallelFor
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
        public struct ReuseEntitiesJob : IJobParallelFor
        {
            
            [ReadOnly]
            public NativeArray <Entity> na_populationEntities ;
            
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity <ShaderAlphaComponent> a_shaderAlpha ;

            public void Execute ( int i )
            {

                Entity populationEntity          = na_populationEntities [i] ;

                a_shaderAlpha [populationEntity] = new ShaderAlphaComponent () { f = 10f } ; // Reset fade.

            }

        }


    }

}