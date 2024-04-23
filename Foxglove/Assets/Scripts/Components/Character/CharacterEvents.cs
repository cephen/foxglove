using SideFX.Events;
using Unity.Entities;

namespace Foxglove.Character {
    public readonly struct PlayerDied : IEvent {
        public PlayerDied(Entity entity) => Entity = entity;
        public Entity Entity { get; init; }
    }
}
