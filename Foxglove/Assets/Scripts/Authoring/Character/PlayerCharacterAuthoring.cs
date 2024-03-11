using Foxglove.Player;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Character {
    public sealed class PlayerCharacterAuthoring : MonoBehaviour {
        public sealed class Baker : Baker<PlayerCharacterAuthoring> {
            public override void Bake(PlayerCharacterAuthoring authoring) {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerCharacterTag>(entity);
            }
        }
    }
}
