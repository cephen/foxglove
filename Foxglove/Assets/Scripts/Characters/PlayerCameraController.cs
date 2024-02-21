using Foxglove.Camera;
using Foxglove.Input;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Foxglove.Characters {
    [BurstCompile]
    [UpdateAfter(typeof(InputReaderSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PlayerCameraController : ISystem {
        private float3 _accumulatedLook;

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<CameraPosition>();
            state.RequireForUpdate<CameraTarget>();
            state.RequireForUpdate<CameraOffset>();
            state.RequireForUpdate<CameraDistance>();
            state.RequireForUpdate<InputState>();
            _accumulatedLook = float3.zero;
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var input = SystemAPI.GetSingleton<InputState>();
            Entity player = SystemAPI.GetSingletonEntity<PlayerTag>();
            var playerTransform = SystemAPI.GetComponent<LocalTransform>(player);

            var cameraPosition = SystemAPI.GetComponent<CameraPosition>(player);
            var cameraTarget = SystemAPI.GetComponent<CameraTarget>(player);
            var cameraOffset = SystemAPI.GetComponent<CameraOffset>(player);
            var cameraDistance = SystemAPI.GetComponent<CameraDistance>(player);

            float3 targetPos = playerTransform.Position + cameraOffset.Value;
            float2 aimValue = input.Aim.Value;

            _accumulatedLook.xy += input.Aim.IsMouseAim switch {
                // TODO: Add different gamepad & mouse sensitivity scaling
                true => aimValue.yx * SystemAPI.Time.DeltaTime,
                false => aimValue.yx * SystemAPI.Time.DeltaTime,
            };

            _accumulatedLook.x = math.clamp(_accumulatedLook.x, -math.radians(80f), math.radians(50f));
            if (_accumulatedLook.y > math.PI * 2) _accumulatedLook.y -= math.PI * 2;
            if (_accumulatedLook.y < math.PI * -2) _accumulatedLook.y += math.PI * 2;

            quaternion rotation = quaternion.Euler(_accumulatedLook);
            float3 cameraRotation = math.rotate(rotation, math.forward() * cameraDistance.Value);

            Debug.DrawLine(playerTransform.Position, targetPos, Color.cyan);
            Debug.DrawLine(float3.zero, cameraRotation, Color.yellow);
            Debug.DrawLine(targetPos, targetPos + cameraRotation, Color.cyan);

            cameraPosition.Value = targetPos + cameraRotation;
            cameraTarget.Value = targetPos;

            SystemAPI.SetComponent(player, cameraPosition);
            SystemAPI.SetComponent(player, cameraTarget);
        }
    }
}
