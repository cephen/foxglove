using Foxglove.Camera;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Camera {
    /// <summary>
    /// For the main camera prefab, adds a tag component that marks the entity as the main camera
    /// </summary>
    internal sealed class MainEntityCameraAuthoring : MonoBehaviour {
        private sealed class Baker : Baker<MainEntityCameraAuthoring> {
            public override void Bake(MainEntityCameraAuthoring authoring) {
                // The camera can move at runtime so a dynamic transform is needed.
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<MainCameraTag>(entity);
            }
        }
    }
}
