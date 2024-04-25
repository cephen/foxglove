using Foxglove.Core.State;
using Foxglove.Gameplay;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Camera {
    /// <summary>
    /// Sync the ECS simulated camera's transform to the main camera GameObject
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public sealed partial class MainCameraSystem : SystemBase {
        private EntityQuery _mainCamQuery;

        protected override void OnCreate() {
            _mainCamQuery = SystemAPI.QueryBuilder().WithAll<MainCameraTag, LocalToWorld>().Build();
            RequireForUpdate(_mainCamQuery);
            RequireForUpdate<State<GameState>>();
        }

        protected override void OnUpdate() {
            // Only run in Playing state
            if (SystemAPI.GetSingleton<State<GameState>>().Current is not GameState.Playing) return;

            if (MainGameObjectCamera.Instance == null) return; // No main camera

            var camTransform = SystemAPI.GetComponent<LocalToWorld>(_mainCamQuery.GetSingletonEntity());

            MainGameObjectCamera
                .Instance
                .transform
                .SetPositionAndRotation(camTransform.Position, camTransform.Rotation);
        }
    }
}
