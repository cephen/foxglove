# Foxglove.Gameplay

Systems in this assembly implement the game loop of foxglove

## Game Manager State Diagram

```mermaid
stateDiagram-v2
    direction TB
    [*] --> MainMenu : Initialize
    MainMenu --> CreateGame: Start Game Button Pressed

    state CreateGame {
        [*] --> GenerateMap : [Send] Build Map Request
        GenerateMap --> SpawnPlayer : [Receive] Map Ready event
        SpawnPlayer --> EnableCombatDirector
        EnableCombatDirector --> [*]
    }

    CreateGame --> MainGameLoop : [Send] Game Ready Event

    state MainGameLoop {
        direction LR
        [*] --> Playing
        Playing --> Paused
        Paused --> Playing
        Playing --> LevelComplete : Teleporter Used
        BuildNextLevel --> Playing : [Receive] Map Ready event
        LevelComplete --> BuildNextLevel : Player Clicks continue\n[Send] Build Map Request
        Playing --> [*] : [Receive] Player Died event
        Paused --> [*] : Quit game
        LevelComplete --> [*] : Quit Game

    }

    MainGameLoop --> MainMenu
    MainMenu --> [*] : Shut down

```
