using SideFX.Events;

namespace Foxglove.Gameplay {
    public readonly struct ToggleSpawnersEvent : IEvent {
        public bool Enabled { get; init; }
    }
}
