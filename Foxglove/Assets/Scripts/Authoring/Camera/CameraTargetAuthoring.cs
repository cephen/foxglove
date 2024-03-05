using System;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Camera {
    [Serializable]
    public struct CameraTarget : IComponentData {
        public Entity TargetEntity;
    }

    [DisallowMultipleComponent]
    public sealed class CameraTargetAuthoring : MonoBehaviour {
        public GameObject Target;

        public sealed class Baker : Baker<CameraTargetAuthoring> {
            public override void Bake(CameraTargetAuthoring authoring) {
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
