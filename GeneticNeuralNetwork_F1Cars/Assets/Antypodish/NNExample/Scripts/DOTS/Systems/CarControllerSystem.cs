using UnityEngine ;

using Unity.Jobs ;
using Unity.Physics ;
using Unity.Entities ;
using Unity.Transforms ;
using Unity.Mathematics ;

using Antypodish.DOTS ;
using Antypodish.GeneticNueralNetwork.DOTS ;

namespace Antypodish.AI.DOTS
{
    
    public class CarControllerSystem : SystemBase
    {

        protected override void OnCreate ( )
        {
        }

        // Update is called once per frame
        protected override void OnUpdate ( )
        {
            
            Entities
                .WithName ( "AssignCarContollsFromNeuralNetworkOutoutsJob" )
                .WithAll <IsAliveTag, CarTag> ()
                .WithNone <NNIsFinishedTag> ()
                .ForEach ( ( ref VehicleControllsComponent vehicleControlls, in DynamicBuffer <NNOutputNeuronsValuesBuffer> nnOutputsLayer ) =>
            {
                vehicleControlls.f_throtle  = nnOutputsLayer [0].f ;
                vehicleControlls.f_steering = nnOutputsLayer [1].f ;

            }).ScheduleParallel () ;

            float f_accelartionFactor = 0.0001f ; // 2
            float f_steeringFactor    = 1.5f ; // 10

            Entities
                .WithName ( "CarControllerJob" )
                .WithAll <IsAliveTag, CarTag> ()
                .WithNone <NNIsFinishedTag> ()
                .ForEach ( ( ref Translation position, ref Rotation q, ref VehicleVelocityComponent velocity, in VehicleControllsComponent vehicleControlls ) =>
            {

                // range -1f to 1f.
                float f_thortle              = vehicleControlls.f_throtle * 2 - 1 ;
                float f_steering             = vehicleControlls.f_steering * 2 - 1 ;


                float3 f3_forward            = math.mul ( q.Value, Vector3.forward ) ;
                float3 f3_left               = math.mul ( q.Value, Vector3.left ) ;
                velocity.f_acceleration += f_thortle * f_accelartionFactor ;
                float3 f3_acceleration       = f3_forward * velocity.f_acceleration ;

                
                velocity.f3_speed       *= 0.7f ; // Apply friction
                // Speed vector allows also for skidding.
                velocity.f3_speed       += f3_acceleration ;
                // velocity.f_forwardSpeed   = 

                position.Value          += velocity.f3_speed ;
                
                float3 f3_speedNorm          = math.normalize ( velocity.f3_speed ) ;

                velocity.isOnReverse         = math.dot ( f3_speedNorm, f3_forward ) > 0 ? true : false ; 
                
                velocity.f_forwardSpeed      = math.dot ( velocity.f3_speed, f3_forward ) ;
                velocity.f_sideWaySkiddSpeed = math.dot ( velocity.f3_speed, f3_left ) * 100 ;
// Debug.DrawLine ( position.Value, position.Value + f3_forward * velocity.f_forwardSpeed * 10, Color.blue ) ;
// Debug.DrawLine ( position.Value, position.Value + f3_left * velocity.f_sideWaySkiddSpeed * 10, Color.white ) ;

                // Debug.DrawLine ( position.Value, position.Value + velocity.f3_speed * 100, Color.red ) ;
                // Steering
                // float f_forward          = math.length ( velocity.f3_speed ) ;
                q.Value                  = math.mul ( q.Value, quaternion.RotateY ( f_steering * f_steeringFactor * velocity.f_forwardSpeed ) ) ;

            }).Schedule ();

        }
    }
}