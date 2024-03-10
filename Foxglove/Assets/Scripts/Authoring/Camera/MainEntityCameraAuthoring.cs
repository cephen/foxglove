using Foxglove.Camera;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Camera {
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
