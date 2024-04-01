using Foxglove.Camera;
using Foxglove.Input;
using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Player {
    /// <summary>
    /// Apply inputs that need to be read at a variable rate
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PlayerVariableStepSystemGroup))]
    internal partial struct PlayerVariableStepControlSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<InputState>();
            state.RequireForUpdate<LookSensitivity>();
            state.RequireForUpdate<PlayerController>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var input = SystemAPI.GetSingleton<InputState>();
            var sensitivity = SystemAPI.GetSingleton<LookSensitivity>();
            var playerController = SystemAPI.GetSingleton<PlayerController>();

            if (playerController.ControlledCharacter == Entity.Null) return;
            Entity controlledCharacter = playerController.ControlledCharacter;

            if (playerController.ControlledCamera == Entity.Null) return;
            Entity controlledCamera = playerController.ControlledCamera;

            if (!SystemAPI.HasComponent<OrbitCameraControl>(controlledCamera)) return;
            var cameraControl = SystemAPI.GetComponent<OrbitCameraControl>(controlledCamera);

            cameraControl.FollowedCharacterEntity = controlledCharacter;
            cameraControl.LookDegreesDelta = input.Aim.IsMouseAim switch {
                true => input.Aim.Value * sensitivity.Mouse / 10,
                false => input.Aim.Value * sensitivity.Gamepad,
            };

            SystemAPI.SetComponent(controlledCamera, cameraControl);
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
