using SideFX.Events;
using Unity.Mathematics;

namespace Foxglove.Maps {
    public readonly struct MapReadyEvent : IEvent {
        public float3 PlayerLocation { get; init; }
        public float3 TeleporterLocation { get; init; }
    }

    public readonly struct DespawnMapCommand : IEvent { }
}
