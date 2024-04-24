using Foxglove.Combat;
using SideFX.Events;
using Unity.Entities;

namespace Foxglove.Character {
    public readonly struct PlayerDied : IEvent {
        public Entity Entity { get; init; }

        public PlayerDied(Entity entity) => Entity = entity;
    }

    public readonly struct PlayerDamaged : IEvent {
        public Health Health { get; init; }

        public PlayerDamaged(Health health) => Health = health;
    }
}
