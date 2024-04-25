using SideFX.Events;

namespace Foxglove.Gameplay {
    /// <summary>
    /// Represents the states the game can be in.
    /// </summary>
    public enum GameState {
        MainMenu, // Default - The game is starting up or is in the main menu
        CreateGame, // Gameplay systems initialize, map is built
        Playing, // The game is running
        Paused, // The game is paused
        LevelComplete, // The player touched the teleporter
        BuildNextLevel, // Clear the current map, create a new map, spawn the player
        GameOver, // Player died, return to main menu
    }

    /// <summary>
    /// Sent by the game manager when the map is ready,
    /// and all gameplay entities (player, teleporter) have been spawned
    /// </summary>
    public readonly struct GameReady : IEvent { }

    /// <summary>
    /// Sent by the teleporter trigger system when the player touches the teleporter
    /// </summary>
    public readonly struct TeleporterTriggered : IEvent { }

    /// <summary>
    /// Triggered by user input
    /// </summary>
    public readonly struct PauseGame : IEvent { }

    /// <summary>
    /// Triggered by user input or by resume button on pause menu
    /// </summary>
    public readonly struct ResumeGame : IEvent { }

    /// <summary>
    /// Triggered by exit button on pause menu, or by game manager after player death
    /// </summary>
    public readonly struct QuitToMenu : IEvent { }

    /// <summary>
    /// Triggered by exit button on main menu, causes app to close
    /// </summary>
    public readonly struct Shutdown : IEvent { }
}
