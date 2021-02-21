using Unity.Entities ;

using Antypodish.DOTS ;

namespace Antypodish.GeneticNueralNetwork.DOTS
{


    public class InitializeManagerSystem : SystemBase
    {

        BeginSimulationEntityCommandBufferSystem becb ;

        protected override void OnCreate ( )
        {
            becb = World.GetOrCreateSystem <BeginSimulationEntityCommandBufferSystem> () ;
        }

        protected override void OnUpdate ( )
        {

            EntityCommandBuffer.ParallelWriter ecbp = becb.CreateCommandBuffer ().AsParallelWriter () ;

            Entities
                .WithName ( "NNResizeFirstGenerationBuffersJob" )
                .WithAll <NNManagerComponent> ()
                .WithNone <IsAliveTag, IsInitializedTag> ()
                .ForEach ( ( Entity entity, int entityInQueryIndex ) =>
            {
                
                ecbp.AddComponent ( entityInQueryIndex, entity, new NNManagerBestFitnessComponent () { i = 0, i_previousGeneration = 0, entity = default, previousGenerationEntity = default } ) ;
                ecbp.AddComponent ( entityInQueryIndex, entity, new NNScoreComponent () { i = 0, i_previousGeneration = 0 } ) ;
                
                ecbp.AddComponent ( entityInQueryIndex, entity, new NNTimerComponent () { f = 0 } ) ;
                ecbp.AddComponent ( entityInQueryIndex, entity, new NNGenerationCountComponent () { i = 0 } ) ;
                ecbp.AddComponent ( entityInQueryIndex, entity, new NNLayersNeuronsCountComponent () { i_inputLayerNeuronsCount = 0, i_hiddenLayerNeuronsCount = 0, i_outputLayerNeuronsCount = 0 } ) ;

                DynamicBuffer <NNINdexProbabilityBuffer> ecb = ecbp.AddBuffer <NNINdexProbabilityBuffer> ( entityInQueryIndex, entity ) ;
                ecb.ResizeUninitialized ( 2048 ) ; // Suspected minimum reaseanable capacity. May be lower, or bigger, depending on application.
                ecb.ResizeUninitialized ( 0 ) ; // Then set back to 0.

                ecbp.AddComponent <IsInitializedTag> ( entityInQueryIndex, entity ) ;

            }).ScheduleParallel () ;
            
            becb.AddJobHandleForProducer ( Dependency ) ;
        }

    }

}