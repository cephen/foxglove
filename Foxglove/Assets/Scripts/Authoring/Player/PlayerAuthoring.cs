using Unity.Entities;
using UnityEngine;

namespace Foxglove.Player {
    [DisallowMultipleComponent]
    public sealed class PlayerAuthoring : MonoBehaviour {
        public GameObject ControlledCharacter;
        public GameObject ControlledCamera;

        public sealed class Baker : Baker<PlayerAuthoring> {
            public override void Bake(PlayerAuthoring authoring) {
                Entity controller = GetEntity(TransformUsageFlags.None);
                AddComponent(
                    controller,
                    new PlayerController {
                        ControlledCharacter = GetEntity(authoring.ControlledCharacter, TransformUsageFlags.Dynamic),
                        ControlledCamera = GetEntity(authoring.ControlledCamera, TransformUsageFlags.Dynamic),
                    }
                );
            }
        }
    }
}
