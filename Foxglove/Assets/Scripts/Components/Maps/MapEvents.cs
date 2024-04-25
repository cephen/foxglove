using SideFX.Events;
using Unity.Mathematics;

namespace Foxglove.Maps {
    /// <summary>
    /// Sent by the map generator system after generation is complete and spawn positions have been chosen
    /// </summary>
    public readonly struct MapReadyEvent : IEvent {
        public float3 PlayerLocation { get; init; }
        public float3 TeleporterLocation { get; init; }
    }
}
