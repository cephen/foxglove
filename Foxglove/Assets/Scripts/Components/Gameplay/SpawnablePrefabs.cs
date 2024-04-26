using Unity.Entities;

namespace Foxglove.Gameplay {
    /// <summary>
    /// Stores baked prefab entities that can be instantiated.
    /// ---
    /// At runtime a single instance of this component is created when the gameplay scene is loaded,
    /// and destroyed when the scene is unloaded.
    /// </summary>
    public struct SpawnablePrefabs : IComponentData {
        public Entity OrbitCamera { get; init; }
        public Entity Player { get; init; }
        public Entity Wisp { get; init; }
        public Entity Teleporter { get; init; }
    }
}
