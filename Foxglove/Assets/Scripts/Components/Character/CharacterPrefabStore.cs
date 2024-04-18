using Unity.Entities;

namespace Foxglove.Character {
    public struct CharacterPrefabStore : IComponentData {
        public Entity PlayerPrefab { get; init; }
        public Entity WispPrefab { get; init; }
    }
}
