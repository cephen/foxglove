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
                Entity character = GetEntity(authoring.ControlledCharacter, TransformUsageFlags.Dynamic);
                AddComponent<PlayerCharacterTag>(character);
                AddComponent(
                    controller,
                    new PlayerController {
                        ControlledCharacter = character,
                        ControlledCamera = GetEntity(authoring.ControlledCamera, TransformUsageFlags.Dynamic),
                    }
                );
            }
        }
    }
}
