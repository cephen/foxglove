using Foxglove.Gameplay;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Gameplay {
    internal sealed class SpawnablePrefabAuthoring : MonoBehaviour {
        public GameObject OrbitCamera;
        public GameObject PlayerPrefab;
        public GameObject WispPrefab;

        private sealed class Baker : Baker<SpawnablePrefabAuthoring> {
            public override void Bake(SpawnablePrefabAuthoring authoring) {
                Entity e = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(
                    e,
                    new SpawnablePrefabs {
                        OrbitCamera = GetEntity(authoring.OrbitCamera, TransformUsageFlags.Dynamic),
                        PlayerPrefab = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.WorldSpace),
                        WispPrefab = GetEntity(authoring.WispPrefab, TransformUsageFlags.WorldSpace),
                    }
                );
            }
        }
    }
}
