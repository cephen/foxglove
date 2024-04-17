using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Camera {
    /// <summary>
    /// Sync the ECS simulated camera's transform to the main camera GameObject
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public sealed partial class MainCameraSystem : SystemBase {
        protected override void OnUpdate() {
            if (MainGameObjectCamera.Instance == null
                || !SystemAPI.HasSingleton<MainCameraTag>())
                return;

            Entity mainCameraEntity = SystemAPI.GetSingletonEntity<MainCameraTag>();
            var camTransform = SystemAPI.GetComponent<LocalToWorld>(mainCameraEntity);

            MainGameObjectCamera
                .Instance
                .transform
                .SetPositionAndRotation(camTransform.Position, camTransform.Rotation);
        }
    }
}
