using Foxglove.Camera;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Camera {
    /// <summary>
    /// For the main camera prefab, adds a tag component to the entity that represents the main camera
    /// </summary>
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
