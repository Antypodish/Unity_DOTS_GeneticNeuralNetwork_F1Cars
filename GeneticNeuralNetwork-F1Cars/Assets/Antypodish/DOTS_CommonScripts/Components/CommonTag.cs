using Unity.Entities;

namespace Antypodish.DOTS
{

    public struct IsAliveTag : IComponentData {}

    public struct IsInitializedTag : IComponentData {}
    
    public struct IsSpawningCompleteTag : IComponentData {}

    public struct IsSpawningTag : IComponentData {}
    
}