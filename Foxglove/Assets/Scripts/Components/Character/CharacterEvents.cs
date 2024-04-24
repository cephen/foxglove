using SideFX.Events;
using Unity.Entities;

namespace Foxglove.Character {
    public readonly struct PlayerDied : IEvent {
        public Entity Entity { get; init; }

        public PlayerDied(Entity entity) => Entity = entity;
    }

    public readonly struct PlayerDamaged : IEvent {
        public float Damage { get; init; }

        public PlayerDamaged(float damage) => Damage = damage;
    }
}
