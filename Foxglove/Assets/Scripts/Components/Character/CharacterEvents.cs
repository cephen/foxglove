using Foxglove.Combat;
using SideFX.Events;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Character {
    /// <summary>
    /// Raised when the player dies, contains the entity representing the player character
    /// </summary>
    public readonly struct PlayerDied : IEvent {
        public readonly Entity Entity;
        public PlayerDied(Entity entity) => Entity = entity;
    }

    /// <summary>
    /// Raised whenever the player's health changes.
    /// </summary>
    public readonly struct PlayerHealthChanged : IEvent {
        public readonly Health Health;
        public PlayerHealthChanged(Health health) => Health = health;
    }


    /// <summary>
    /// One variant for each entity type that can be spawned
    /// </summary>
    public enum Spawnable { Player, Wisp, Teleporter }

    /// <summary>
    /// Raised to request an entity be spawned at a given position.
    /// </summary>
    public readonly struct SpawnRequest : IEvent {
        public Spawnable Spawnable { get; init; }
        public float3 Position { get; init; }
    }
}
