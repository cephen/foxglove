using Unity.Entities;
using UnityEngine;

namespace Foxglove.Camera {
    public struct MainCameraTag : IComponentData { }

    [DisallowMultipleComponent]
    public sealed class MainEntityCameraAuthoring : MonoBehaviour {
        public sealed class Baker : Baker<MainEntityCameraAuthoring> {
            public override void Bake(MainEntityCameraAuthoring authoring) {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<MainCameraTag>(entity);
            }
        }
    }
}
