using SideFX.Events;

namespace Foxglove.Gameplay {
    public readonly struct StartGameEvent : IEvent { }

    public readonly struct PauseEvent : IEvent { }

    public readonly struct ResumeEvent : IEvent { }
}
