using Unity.Entities;
using UnityEngine;

namespace Foxglove.Player {
    [DisallowMultipleComponent]
    public sealed class ThirdPersonPlayerAuthoring : MonoBehaviour {
        public GameObject ControlledCharacter;
        public GameObject ControlledCamera;

        public sealed class Baker : Baker<ThirdPersonPlayerAuthoring> {
            public override void Bake(ThirdPersonPlayerAuthoring authoring) {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(
                    entity,
                    new ThirdPersonPlayer {
                        ControlledCharacter = GetEntity(authoring.ControlledCharacter, TransformUsageFlags.Dynamic),
                        ControlledCamera = GetEntity(authoring.ControlledCamera, TransformUsageFlags.Dynamic),
                    }
                );
            }
        }
    }
}
