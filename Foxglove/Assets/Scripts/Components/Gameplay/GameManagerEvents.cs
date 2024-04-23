using SideFX.Events;

namespace Foxglove.Gameplay {
    public enum GameState {
        MainMenu,
        CreateGame,
        BuildNextLevel,
        Playing,
        Paused,
        LevelComplete,
        GameOver,
    }

    public readonly struct GameReady : IEvent { }

    public readonly struct TeleporterTriggered : IEvent { }

    public readonly struct PauseGame : IEvent { }

    public readonly struct ResumeGame : IEvent { }

    public readonly struct QuitToMenu : IEvent { }

    public readonly struct Shutdown : IEvent { }
}
