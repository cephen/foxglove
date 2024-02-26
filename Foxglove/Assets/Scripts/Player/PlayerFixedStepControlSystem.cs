using Foxglove.Camera.OrbitCamera;
using Foxglove.Character;
using Foxglove.Input;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Player {
    /// <summary>
    /// Apply inputs that need to be read at a fixed rate (specifically, movement related inputs)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    public partial struct PlayerFixedStepControlSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<FoxgloveGameplayInput>();
            state.RequireForUpdate<FixedTickSystem.Singleton>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<ThirdPersonPlayer, Simulate>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            uint tick = SystemAPI.GetSingleton<FixedTickSystem.Singleton>().Tick;
            var input = SystemAPI.GetSingleton<FoxgloveGameplayInput>();

            foreach (ThirdPersonPlayer player in SystemAPI.Query<ThirdPersonPlayer>().WithAll<Simulate>()) {
                // Early exit if player isn't controlling a character
                if (!SystemAPI.HasComponent<FoxgloveCharacterControl>(player.ControlledCharacter)) continue;

                var control = SystemAPI.GetComponent<FoxgloveCharacterControl>(player.ControlledCharacter);
                var transform = SystemAPI.GetComponent<LocalTransform>(player.ControlledCharacter);

                float3 characterUp = MathUtilities.GetUpFromRotation(transform.Rotation);

                // player movement should be relative to camera rotation.
                quaternion cameraRotation = quaternion.identity;
                if (SystemAPI.HasComponent<OrbitCamera>(player.ControlledCamera)) {
                    var camera = SystemAPI.GetComponent<OrbitCamera>(player.ControlledCamera);
                    cameraRotation = OrbitCameraUtilities.CalculateCameraRotation(
                        characterUp,
                        camera.PlanarForward,
                        camera.PitchAngle
                    );
                }

                float3 cameraForwardOnUpPlane =
                    math.normalizesafe(
                        MathUtilities.ProjectOnPlane(
                            MathUtilities.GetForwardFromRotation(cameraRotation),
                            characterUp
                        )
                    );
                float3 cameraRight = MathUtilities.GetRightFromRotation(cameraRotation);

                // Move
                control.MoveVector = input.Move.y * cameraForwardOnUpPlane
                                     + input.Move.x * cameraRight;
                control.MoveVector = MathUtilities.ClampToMaxLength(control.MoveVector, 1f);

                // Jump
                control.Jump = input.Jump.IsSet(tick);

                SystemAPI.SetComponent(player.ControlledCharacter, control);
            }
        }

        void ISystem.OnDestroy(ref SystemState state) { }
    }
}
