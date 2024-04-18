using Unity.Entities;

namespace Foxglove.Gameplay {
    public struct SpawnablePrefabs : IComponentData {
        public Entity OrbitCamera { get; init; }
        public Entity PlayerPrefab { get; init; }
        public Entity WispPrefab { get; init; }
    }
}
