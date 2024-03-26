using Foxglove.Player;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Character {
    /// <summary>
    /// Authoring component that tags an entity as the player's controlled character
    /// </summary>
    public sealed class PlayerCharacterAuthoring : MonoBehaviour {
        public sealed class Baker : Baker<PlayerCharacterAuthoring> {
            public override void Bake(PlayerCharacterAuthoring authoring) {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerCharacterTag>(entity);
            }
        }
    }
}
