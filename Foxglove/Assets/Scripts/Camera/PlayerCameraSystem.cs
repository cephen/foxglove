using Foxglove.Characters;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Camera {
    [BurstCompile]
    public partial class PlayerCameraSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<PlayerTag>();
            RequireForUpdate<CameraPosition>();
        }

        [BurstCompile]
        protected override void OnUpdate() {
            if (UnityEngine.Camera.main == null)
                // Log Error
                return;

            UnityEngine.Camera camera = UnityEngine.Camera.main;
            Entity player = SystemAPI.GetSingletonEntity<PlayerTag>();
            var cameraPosition = SystemAPI.GetComponent<CameraPosition>(player);
            var cameraTarget = SystemAPI.GetComponent<CameraTarget>(player);
            camera.transform.position = cameraPosition.Value;
            camera.transform.rotation = quaternion.LookRotation(cameraTarget.Value - cameraPosition.Value, math.up());
        }
    }
}
