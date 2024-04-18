using SideFX.Events;
using Unity.Mathematics;

namespace Foxglove.Character {
    public enum SpawnableCharacter { Player, Wisp }

    public readonly struct SpawnCharacterEvent : IEvent {
        public SpawnableCharacter Character { get; init; }
        public float3 Position { get; init; }
    }
}
