using SideFX.Events;

namespace Foxglove.Maps {
    public readonly struct MapReadyEvent : IEvent {
        public readonly Room PlayerSpawnRoom { get; init; }
        public readonly Room TeleporterSpawnRoom { get; init; }
    }

    public readonly struct BuildMapEvent : IEvent { }

    public readonly struct DespawnMapCommand : IEvent { }
}
