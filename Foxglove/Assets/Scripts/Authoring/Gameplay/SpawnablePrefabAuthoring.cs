using Foxglove.Gameplay;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Gameplay {
    /// <summary>
    /// Bakes spawnable prefabs and caches them in a singleton component
    /// </summary>
    internal sealed class SpawnablePrefabAuthoring : MonoBehaviour {
        public GameObject OrbitCamera;
        public GameObject PlayerPrefab;
        public GameObject WispPrefab;
        public GameObject TeleporterPrefab;

        private sealed class Baker : Baker<SpawnablePrefabAuthoring> {
            public override void Bake(SpawnablePrefabAuthoring authoring) {
                Entity e = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(
                    e,
                    new SpawnablePrefabs {
                        // TransformUsageFlags.Dynamic for objects that can move
                        OrbitCamera = GetEntity(authoring.OrbitCamera, TransformUsageFlags.Dynamic),
                        Player = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic),
                        Wisp = GetEntity(authoring.WispPrefab, TransformUsageFlags.Dynamic),
                        // TransformUsageFlags.Renderable for objects that can be rendered but do not move
                        Teleporter = GetEntity(authoring.TeleporterPrefab, TransformUsageFlags.Renderable),
                    }
                );
            }
        }
    }
}
