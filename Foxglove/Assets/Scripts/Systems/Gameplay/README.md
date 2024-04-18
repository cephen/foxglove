# Foxglove.Gameplay

Systems in this assembly implement the game loop of foxglove

## Game Manager State Diagram

```mermaid
stateDiagram-v2
direction TB
    [*] --> Startup
    Startup --> WaitForMap : Send BuildMapEvent
    WaitForMap --> MapReady : Received MapReadyEvent
    MapReady --> Playing
    Playing --> Paused
    Paused --> Playing
    Playing --> GameOver : Player Dies
    Playing --> NextLevel : Player uses teleporter
    NextLevel --> WaitForMap
    state MapReady {
        SpawnPlayer --> EnableMobSpawners
    }
```
