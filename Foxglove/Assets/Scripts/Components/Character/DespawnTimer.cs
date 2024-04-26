using Unity.Entities;

namespace Foxglove.Character {
    /// <summary>
    /// When attached to an entity and enabled, the despawn system will monitor the entity
    /// and despawn it when the current game tick has reached the tick specified
    /// </summary>
    public struct DespawnTimer : IComponentData {
        public uint TickToDestroy;
        public DespawnTimer(uint tickToDestroy) => TickToDestroy = tickToDestroy;
    }
}
