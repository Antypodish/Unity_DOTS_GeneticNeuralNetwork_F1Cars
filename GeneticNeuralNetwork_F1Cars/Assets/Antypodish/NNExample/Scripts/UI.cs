using UnityEngine;
using UnityEngine.UI ;

using Unity.Entities ;
using Unity.Collections ;

using Antypodish.DOTS ;
using Antypodish.GeneticNueralNetwork.DOTS ;

namespace Antypodish.GNNExample
{

    public class UI : MonoBehaviour
    {

        public Text generation ;
        public Text runtime ;
        public Text totalNumberOfBrains ;
        public Text liveBrains ;
        public Text totalScore ;
        public Text bestBrainScore ;
        public Text averageBrainScore ;

        private float f_time ;

        
        EntityQuery group_MMMamager ;
        // EntityQuery group_parentGeneration ;
        EntityQuery group_finishedPopulation ;
        EntityManager em ;

        // Start is called before the first frame update
        void Start()
        {
        
            em = World.DefaultGameObjectInjectionWorld.EntityManager ;

            group_MMMamager = em.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),
               ComponentType.ReadOnly <NNManagerComponent> (),

               ComponentType.Exclude <NNMangerIsSpawningNewGenerationTag> ()
            ) ;
            
            /*
            group_parentGeneration = em.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),
               // ComponentType.ReadOnly <NNIsFinishedTag> (),

               ComponentType.ReadOnly <NNBrainTag> (),
               ComponentType.ReadOnly <NNIsPreviousGenerationTag> (),
               
               ComponentType.ReadWrite <NNManagerSharedComponent> ()
            ) ;
            */

            group_finishedPopulation = em.CreateEntityQuery
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
            
        }

        // Update is called once per frame
        void Update ()
        {
            
            NativeArray <Entity> na_managers = group_MMMamager.ToEntityArray ( Unity.Collections.Allocator.Temp ) ;

            if ( na_managers.Length > 0 )
             {

                Entity nnManagerEntity                     = na_managers [na_managers.Length -1] ;
                NNManagerComponent manager                 = em.GetComponentData <NNManagerComponent> ( nnManagerEntity )  ;

                NNGenerationCountComponent generationCount = em.GetComponentData <NNGenerationCountComponent> ( nnManagerEntity )  ;
                
                NNManagerBestFitnessComponent bestScore    = em.GetComponentData <NNManagerBestFitnessComponent> ( nnManagerEntity )  ;
                NNScoreComponent managerTotalScore         = em.GetComponentData <NNScoreComponent> ( nnManagerEntity )  ;
            
                generation.text                            = generationCount.i.ToString () ;

                totalNumberOfBrains.text                   = manager.i_populationSize.ToString () ;

                f_time                                     = Time.timeSinceLevelLoad ;
                runtime.text                               = ( (int) f_time ).ToString () ;

                liveBrains.text                            = ( manager.i_populationSize - group_finishedPopulation.CalculateEntityCount () ).ToString () ;

                totalScore.text                            = managerTotalScore.i.ToString () ;

                bestBrainScore.text                        = bestScore.i.ToString () ;
                
                averageBrainScore.text                     = ( managerTotalScore.i / (float) manager.i_populationSize ).ToString ( "F2" ) ;

            }

            na_managers.Dispose () ;

        }

    }

}