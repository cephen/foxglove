using Foxglove.Character;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Character {
    internal sealed class CharacterPrefabStoreAuthoring : MonoBehaviour {
        public GameObject PlayerPrefab;
        public GameObject WispPrefab;

        private sealed class Baker : Baker<CharacterPrefabStoreAuthoring> {
            public override void Bake(CharacterPrefabStoreAuthoring authoring) {
                Entity e = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(
                    e,
                    new CharacterPrefabStore {
                        PlayerPrefab = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic),
                        WispPrefab = GetEntity(authoring.WispPrefab, TransformUsageFlags.Dynamic),
                    }
                );
            }
        }
    }
}
