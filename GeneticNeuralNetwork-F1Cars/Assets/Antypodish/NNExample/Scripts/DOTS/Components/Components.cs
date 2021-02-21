using Unity.Entities ;
using Unity.Mathematics ;

namespace Antypodish.AI.DOTS
{

    public struct VehicleControllsComponent : IComponentData 
    {
        /// <summary>
        /// Range 0 to 1
        /// </summary>
        public float f_throtle ;
        /// <summary>
        /// Range 0 to 1
        /// </summary>
        public float f_steering ;
    }
    
    public struct VehicleVelocityComponent : IComponentData 
    {
        public float3 f3_speed ;
        public float f_forwardSpeed ;
        public float f_sideWaySkiddSpeed ;
        public float f_acceleration ;
        public bool isOnReverse ;
    }

    public struct CarTag : IComponentData {}
    public struct BadGuyTag : IComponentData {}

    public struct LIDARTag : IComponentData {}


}