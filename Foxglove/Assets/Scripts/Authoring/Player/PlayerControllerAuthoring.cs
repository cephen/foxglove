using Foxglove.Player;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Player {
    /// <summary>
    /// Used to configure the player controller,
    /// which is a simple component containing references to the camera and character the player controls
    /// a gameobject with this component lives in the main gameplay scene, and has the initial reference to the player.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class PlayerControllerAuthoring : MonoBehaviour {
        public GameObject ControlledCharacter;
        public GameObject ControlledCamera;

        private sealed class Baker : Baker<PlayerControllerAuthoring> {
            public override void Bake(PlayerControllerAuthoring authoring) {
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
