using Unity.Entities;
using UnityEngine;

namespace Foxglove.Camera {
    /// <summary>
    /// Tag component for the main camera, there should only ever be one entity with this tag.
    /// </summary>
    public struct MainCameraTag : IComponentData { }

    [DisallowMultipleComponent]
    public sealed class MainEntityCameraAuthoring : MonoBehaviour {
        public sealed class Baker : Baker<MainEntityCameraAuthoring> {
            public override void Bake(MainEntityCameraAuthoring authoring) {
                // The camera can move at runtime so a dynamic transform is needed.
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<MainCameraTag>(entity);
            }
        }
    }
}
