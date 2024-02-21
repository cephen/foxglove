using Foxglove.Camera;
using Foxglove.Characters;
using Foxglove.Input;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;

namespace Foxglove.Motion {
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsInitializeGroup))]
    [UpdateBefore(typeof(AccelerationSystem))]
    public partial class PlayerOrientationSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<PlayerTag>();
            RequireForUpdate<InputState>();
            RequireForUpdate<CameraPosition>();
            RequireForUpdate<CameraTarget>();
        }

        [BurstCompile]
        protected override void OnUpdate() {
            var input = SystemAPI.GetSingleton<InputState>();

            Entities
                .WithAll<PlayerTag>()
                .ForEach((ref Heading heading, in CameraPosition cameraPosition, in CameraTarget cameraTarget) => {
                    float3 lookDirection =
                        math.normalizesafe(cameraTarget.Value - cameraPosition.Value, math.forward());
                    float forwardDotLook = math.dot(math.forward(), lookDirection);
                    float multiplied = forwardDotLook / math.length(lookDirection);
                    heading.Radians = math.acos(multiplied);
                    // Log.Info("Look Direction: {Direction}, Dot Forward {DotF}, Player Heading: {HeadingDegrees}",
                    //     lookDirection, forwardDotLook, heading.Degrees);
                })
                .Run();


            Entities
                .WithAll<PlayerTag>()
                .ForEach((ref TargetVelocity targetVelocity, in Heading heading, in MotionSettings settings) => {
                    float3 forward = math.forward() * input.Move.y;
                    float3 right = math.right() * input.Move.x;
                    float3 totalMove = (forward + right) * settings.MaxHorizontalSpeed;

                    quaternion rotation = quaternion.RotateY(heading.Radians);
                    targetVelocity.Value = math.rotate(rotation, totalMove);
                })
                .Run();
        }
    }
}
