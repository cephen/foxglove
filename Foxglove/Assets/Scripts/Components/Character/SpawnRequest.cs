using SideFX.Events;
using Unity.Mathematics;

namespace Foxglove.Character {
    public enum Spawnable { Player, Wisp, Teleporter }

    public readonly struct SpawnRequest : IEvent {
        public Spawnable Spawnable { get; init; }
        public float3 Position { get; init; }
    }
}
