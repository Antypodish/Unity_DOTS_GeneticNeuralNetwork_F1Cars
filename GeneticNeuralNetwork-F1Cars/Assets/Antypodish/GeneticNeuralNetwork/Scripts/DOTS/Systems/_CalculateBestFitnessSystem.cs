using System.Collections.Generic ;

using UnityEngine ;

using Unity.Jobs ;
using Unity.Entities ;
using Unity.Collections ;

using Antypodish.DOTS ;

namespace Antypodish.GeneticNueralNetwork.DOTS
{
    
    [DisableAutoCreation]
    [UpdateInGroup ( typeof ( ManagerPreSimulationSystemGroup ) )]  
    public class CalculateBestFitnessSystem : SystemBase
    {

        EntityQuery group_currentPopulation ;

        NativeArray <int> na_scores ;
        NativeArray <Entity> na_entity ;

        private int i_lastBestFitness ;
        
        private List <NNManagerSharedComponent> l_managerSharedData = new List <NNManagerSharedComponent> ( 1000 ) ;


        protected override void OnCreate ( )
        {
            
            group_currentPopulation = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),
               ComponentType.ReadOnly <NNBrainTag> (),
               ComponentType.ReadOnly <IsSpawningCompleteTag> (),

               ComponentType.Exclude <IsInitializedTag> (),
               ComponentType.Exclude <NNIsPreviousGenerationTag> (),

               ComponentType.ReadWrite <NNManagerSharedComponent> ()
            ) ;

            na_scores = new NativeArray <int> ( 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory ) ;
            na_entity = new NativeArray <Entity> ( 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory ) ;
        }
        

        protected override void OnDestroy ( )
        {
            na_scores.Dispose () ;
            na_entity.Dispose () ;
        }

        protected override void OnUpdate ( )
        {

            ComponentDataFromEntity <NNManagerBestFitnessComponent> a_bestFitness = GetComponentDataFromEntity <NNManagerBestFitnessComponent> ( false ) ;
            ComponentDataFromEntity <IsAliveTag> a_isAliveTag                     = GetComponentDataFromEntity <IsAliveTag> ( true ) ;
            ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore          = GetComponentDataFromEntity <NNBrainScoreComponent> ( true ) ;
            ComponentDataFromEntity <IsTimeUpTag> a_isTimeUpTag                   = GetComponentDataFromEntity <IsTimeUpTag> ( true ) ;
            

            l_managerSharedData.Clear () ;
            EntityManager.GetAllUniqueSharedComponentData ( l_managerSharedData ) ;

            // Ignores default manager entity ( index = 0, version = 0 ), taken from prefab entity.
            for ( int i = 0; i < l_managerSharedData.Count; i++ )
            {
                
                NNManagerSharedComponent mangerSharedComponent = l_managerSharedData [i] ;
                
                Entity managerEntity                           = new Entity () { Index = mangerSharedComponent.i_entityIndex, Version = mangerSharedComponent.i_entityVersion } ;
                
                // Entity manager must be valid and active.
                if ( ManagerMethods._SkipInvalidManager ( managerEntity, ref a_isAliveTag ) ) continue ;
                if ( !a_isTimeUpTag.HasComponent ( managerEntity ) ) continue ;

                NNManagerBestFitnessComponent managerBestFitness = a_bestFitness [managerEntity] ; 
                
                
                group_currentPopulation.SetSharedComponentFilter ( mangerSharedComponent ) ;

                NativeArray <Entity> na_brainsEntities = group_currentPopulation.ToEntityArray ( Allocator.TempJob ) ;
                
Debug.Log ( i + "; Best Fit run: " + na_brainsEntities.Length ) ;
                if ( na_brainsEntities.Length == 0 ) 
                {
                    na_brainsEntities.Dispose () ;
                    continue ;
                }
Debug.Log ( i + " Best fit: " + group_currentPopulation.CalculateEntityCount () ) ;
                this.na_scores [0]                     = managerBestFitness.i ;
                this.na_entity [0]                     = managerBestFitness.entity ;

                NativeArray <int> na_scores            = this.na_scores ;
                NativeArray <Entity> na_entity         = this.na_entity ;


                Dependency = new CalculateBestFitnessJob ()
                {

                    na_scores         = na_scores,
                    na_entity         = na_entity,

                    na_brainsEntities = na_brainsEntities,

                    a_brainScore      = a_brainScore

                }.Schedule ( na_brainsEntities.Length, Dependency ) ;

                Dependency.Complete () ;

                managerBestFitness.i          = na_scores [0] ;
                managerBestFitness.entity     = na_entity [0] ;
                a_bestFitness [managerEntity] = managerBestFitness ;

                na_brainsEntities.Dispose () ;

                // na_managersEntities.Dispose () ;
                

                if ( i_lastBestFitness < managerBestFitness.i )
                {
                    i_lastBestFitness = managerBestFitness.i ;
Debug.LogWarning ( ">> Best fitness brain enttity is: " + managerBestFitness.entity + " with score: " + managerBestFitness.i ) ;
                }

            }

            // na_managersEntities.Dispose () ;

        }

        
        struct CalculateBestFitnessJob : IJobFor
        {

            public NativeArray <int> na_scores ;
            public NativeArray <Entity> na_entity ;

            [ReadOnly]
            public NativeArray <Entity> na_brainsEntities ;

            [ReadOnly]
            public ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore ;

            public void Execute ( int i )
            {
                
                Entity brainEntity               = na_brainsEntities [i] ;
                NNBrainScoreComponent brainScore = a_brainScore [brainEntity] ;

Debug.Log ( "Best fit: " + brainEntity + "; score: " + brainScore.i + "; best fit so far: " + na_scores [0] ) ;

                // Calculate best fitness
                if ( brainScore.i > na_scores [0] )
                {
                    na_scores [0] = brainScore.i ;
                    na_entity [0] = brainEntity ;
                }

            }

        }

    }

}