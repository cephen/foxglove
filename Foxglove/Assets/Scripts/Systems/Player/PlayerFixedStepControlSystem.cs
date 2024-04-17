using Foxglove.Camera;
using Foxglove.Character;
using Foxglove.Core;
using Foxglove.Input;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Player {
    /// <summary>
    /// Apply inputs that need to be read at a fixed rate (specifically, movement related inputs)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PlayerFixedStepSystemGroup), OrderFirst = true)]
    internal partial struct PlayerFixedStepControlSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Tick>();
            state.RequireForUpdate<InputState>();
            state.RequireForUpdate<PlayerController>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            uint tick = SystemAPI.GetSingleton<Tick>().Value;
            var input = SystemAPI.GetSingleton<InputState>();
            var playerController = SystemAPI.GetSingleton<PlayerController>();

            if (playerController.ControlledCharacter == Entity.Null) {
                Log.Error("[PlayerFixedStepControlSystem] - playerController.ControlledCharacter is null");
                return;
            }

            Entity controlledCharacter = playerController.ControlledCharacter;

            if (playerController.ControlledCamera == Entity.Null) {
                Log.Error("[PlayerFixedStepControlSystem] - playerController.ControlledCamera is null");
                return;
            }

            Entity controlledCamera = playerController.ControlledCamera;

            if (!SystemAPI.HasComponent<CharacterController>(controlledCharacter)) {
                Log.Error("[PlayerFixedStepControlSystem] - controlledCharacter has no CharacterController component");
                return;
            }

            var control = SystemAPI.GetComponent<CharacterController>(controlledCharacter);

            var transform = SystemAPI.GetComponent<LocalTransform>(controlledCharacter);
            float3 characterUp = MathUtilities.GetUpFromRotation(transform.Rotation);

            // player movement should be relative to camera rotation.
            quaternion cameraRotation = quaternion.identity;

            if (SystemAPI.HasComponent<OrbitCamera>(controlledCamera)) {
                var camera = SystemAPI.GetComponent<OrbitCamera>(controlledCamera);
                cameraRotation = OrbitCameraUtilities.CalculateCameraRotation(
                    characterUp,
                    camera.PlanarForward,
                    camera.PitchAngle
                );
            }

            float3 cameraForwardOnUpPlane = math.normalizesafe(
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

            SystemAPI.SetComponent(controlledCharacter, control);
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
