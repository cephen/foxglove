using Foxglove.Camera;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Camera {
    /// <summary>
    /// Added to the a child of the player character prefab around which the camera orbits,
    /// when instantiated this component is converted to a <see cref="CameraTarget" />
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraTargetAuthoring : MonoBehaviour {
        public GameObject Target;

        public sealed class Baker : Baker<CameraTargetAuthoring> {
            public override void Bake(CameraTargetAuthoring authoring) {
                // The camera target can move at runtime so a dynamic transform is needed.
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(
                    entity,
                    new CameraTarget {
                        TargetEntity = GetEntity(authoring.Target, TransformUsageFlags.Dynamic),
                    }
                );
            }
        }
    }
}
