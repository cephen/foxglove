using System;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Camera {
    /// <summary>
    /// Component that specifies a target for the camera
    /// </summary>
    [Serializable]
    public struct CameraTarget : IComponentData {
        public Entity TargetEntity;
    }

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
