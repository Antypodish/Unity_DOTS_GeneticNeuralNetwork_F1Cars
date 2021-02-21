using System.Collections.Generic ;
using Unity.Entities ;

using Antypodish.GeneticNueralNetwork.DOTS ;

using UnityEngine ;

namespace Antypodish.AI.DOTS
{    
    
    public struct SpawnerPrefabs_FromEntityData : IComponentData
    {
        public Entity prefabCarEntity ;
    }


    // [RequiresEntityConversion] // Obsolete
    public class GOToEntitiesAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {

        public GameObject prefabPolice ;
       
        void Start ()
        {
        }

        // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
        public void DeclareReferencedPrefabs ( List<GameObject> gameObjects )
        {
            gameObjects.Add ( prefabPolice ) ;
        }

        public void Convert ( Entity spawnerEntity, EntityManager em, GameObjectConversionSystem conversionSystem )
        {

            Debug.Log ( "Prefabs authoring: GO to entities conversion." ) ;

            var spawnerData = new SpawnerPrefabs_FromEntityData
            {
                // The referenced prefab will be converted due to DeclareReferencedPrefabs.
                // So here we simply map the game object to an entity reference to that prefab.
                
                prefabCarEntity  = conversionSystem.GetPrimaryEntity ( prefabPolice ),

            } ;
            
            
            em.AddComponent <Prefab> ( spawnerData.prefabCarEntity ) ;
            
            em.AddComponent <VehicleControllsComponent> ( spawnerData.prefabCarEntity ) ;
            em.AddComponent <VehicleVelocityComponent> ( spawnerData.prefabCarEntity ) ;
                                  

            em.AddComponent <CarTag> ( spawnerData.prefabCarEntity ) ;
            
            em.AddComponent <LIDARTag> ( spawnerData.prefabCarEntity ) ;

            em.AddComponent <NNBrainTag> ( spawnerData.prefabCarEntity ) ;
            // em.AddComponent <IsSpawningTag> ( spawnerData.prefabPoliceEntity ) ;
            em.AddSharedComponentData ( spawnerData.prefabCarEntity, new NNManagerSharedComponent () { i_entityIndex = 0, i_entityVersion = 0 } ) ;
            em.AddComponentData ( spawnerData.prefabCarEntity, new NNBrainScoreComponent () { i = 0, f = 0 } ) ;

            em.AddComponent <NNAssignedToManagerComponent> ( spawnerData.prefabCarEntity ) ;
            em.AddComponent <NNInputNeuronsValuesBuffer> ( spawnerData.prefabCarEntity ) ;
            em.AddComponent <NNHiddenNeuronsValuesBuffer> ( spawnerData.prefabCarEntity ) ;
            em.AddComponent <NNOutputNeuronsValuesBuffer> ( spawnerData.prefabCarEntity ) ;
            em.AddComponent <NNInput2HiddenLayersWeightsBuffer> ( spawnerData.prefabCarEntity ) ;
            em.AddComponent <NNHidden2OutputLayersWeightsBuffer> ( spawnerData.prefabCarEntity ) ;
            // em.AddComponent <NNHiddenLayersNeuronsBiasBuffer> ( spawnerData.prefabCarEntity ) ;

            em.AddComponent <Unity.Physics.Stateful.StatefulTriggerEvent> ( spawnerData.prefabCarEntity ) ;


            em.AddComponentData ( spawnerEntity, spawnerData ) ;
            

        }
      
    }

}
