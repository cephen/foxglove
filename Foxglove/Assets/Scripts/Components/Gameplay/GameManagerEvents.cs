using SideFX.Events;

namespace Foxglove.Gameplay {
    public enum GameState {
        Waiting,
        Startup,
        WaitForMap,
        MapReady,
        Playing,
        Paused,
        ExitToMenu,
    }

    public readonly struct StartGame : IEvent { }

    public readonly struct PauseGame : IEvent { }

    public readonly struct ResumeGame : IEvent { }

    public readonly struct ExitToMainMenu : IEvent { }

    public readonly struct Shutdown : IEvent { }
}
