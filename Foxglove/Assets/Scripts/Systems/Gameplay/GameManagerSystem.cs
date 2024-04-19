using System;
using Foxglove.Character;
using Foxglove.Core;
using Foxglove.Core.State;
using Foxglove.Input;
using Foxglove.Maps;
using SideFX.Events;
using SideFX.SceneManagement;
using SideFX.SceneManagement.Events;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Foxglove.Gameplay {
    internal enum GameState {
        Waiting,
        Startup,
        WaitForMap,
        MapReady,
        Playing,
        Paused,
    }

    internal sealed partial class GameManagerSystem : SystemBase, IStateMachineSystem<GameState> {
        private EventBinding<SceneReady> _sceneReadyBinding;
        private EventBinding<MapReadyEvent> _mapReadyBinding;
        private EventBinding<ResumeGame> _resumeBinding;
        private EventBinding<PauseGame> _pauseBinding;
        private EventBinding<Shutdown> _shutdownBinding;

        private Random _rng;

        protected override void OnCreate() {
            StateMachine.Init(CheckedStateRef, GameState.Waiting);
            _rng = new Random((uint)DateTimeOffset.UtcNow.GetHashCode());

            // Initialize event bindings
            _mapReadyBinding = new EventBinding<MapReadyEvent>(OnMapReady);
            _sceneReadyBinding = new EventBinding<SceneReady>(OnSceneReady);
            _resumeBinding = new EventBinding<ResumeGame>(OnResume);
            _pauseBinding = new EventBinding<PauseGame>(OnPause);
            _shutdownBinding = new EventBinding<Shutdown>(OnShutdown);

            // Register event bindings
            EventBus<MapReadyEvent>.Register(_mapReadyBinding);
            EventBus<SceneReady>.Register(_sceneReadyBinding);
            EventBus<ResumeGame>.Register(_resumeBinding);
            EventBus<PauseGame>.Register(_pauseBinding);
            EventBus<Shutdown>.Register(_shutdownBinding);
        }

        protected override void OnDestroy() {
            EventBus<MapReadyEvent>.Deregister(_mapReadyBinding);
            EventBus<SceneReady>.Deregister(_sceneReadyBinding);
            EventBus<ResumeGame>.Deregister(_resumeBinding);
            EventBus<PauseGame>.Deregister(_pauseBinding);
            EventBus<Shutdown>.Deregister(_shutdownBinding);
        }

        protected override void OnUpdate() {
            CheckIfShouldPause();
            if (StateMachine.IsTransitionQueued<GameState>(CheckedStateRef)) Transition(ref CheckedStateRef);
        }

        private void CheckIfShouldPause() {
            var tick = SystemAPI.GetSingleton<Tick>();
            var inputState = SystemAPI.GetSingleton<InputState>();

            State<GameState> state = StateMachine.GetState<GameState>(CheckedStateRef);
            // If in a state where pressing pause matters
            if (state.Current is GameState.Playing or GameState.Paused
                // and the pause button was pressed this frame
                && inputState.Pause.IsSet(tick)
            )
                switch (state.Current) {
                    case GameState.Playing:
                        EventBus<PauseGame>.Raise(new PauseGame());
                        break;
                    case GameState.Paused:
                        EventBus<ResumeGame>.Raise(new ResumeGame());
                        break;
                    default:
                        return;
                }
        }

        private void SpawnPlayer() {
            // Select a room to spawn the player in
            Entity mapEntity = SystemAPI.GetSingletonEntity<Map>();
            DynamicBuffer<Room> rooms = SystemAPI.GetBuffer<Room>(mapEntity);
            int roomIndex = _rng.NextInt(0, rooms.Length);
            Room room = rooms[roomIndex];

            // Request spawning of the player
            var spawnRequest = new SpawnCharacterEvent {
                Character = SpawnableCharacter.Player,
                Position = new float3(room.Center.x, 1f, room.Center.y),
            };
            EventBus<SpawnCharacterEvent>.Raise(spawnRequest);
        }

#region EventBus bindings

        private void OnMapReady() {
            if (StateMachine.GetState<GameState>(CheckedStateRef).Current is not GameState.WaitForMap) return;

            StateMachine.SetNextState(CheckedStateRef, GameState.MapReady);
        }

        private void OnSceneReady(SceneReady e) {
            if (e.Scene is not GameplayScene) return;

            if (StateMachine.GetState<GameState>(CheckedStateRef).Current is GameState.Waiting)
                StateMachine.SetNextState(CheckedStateRef, GameState.Startup);
        }

        private void OnPause(PauseGame _) {
            if (StateMachine.GetState<GameState>(CheckedStateRef).Current is GameState.Playing)
                StateMachine.SetNextState(CheckedStateRef, GameState.Paused);
        }

        private void OnResume(ResumeGame _) {
            if (StateMachine.GetState<GameState>(CheckedStateRef).Current is GameState.Paused)
                StateMachine.SetNextState(CheckedStateRef, GameState.Playing);
        }

        private void OnShutdown(ShutdownEvent _) {
        private void OnShutdown(Shutdown _) {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

#endregion

#region IStateMachine Implementation

        public void OnEnter(ref SystemState ecsState, State<GameState> gameState) {
            switch (gameState.Current) {
                case GameState.Startup:
                    EventBus<BuildMapEvent>.Raise(new BuildMapEvent());
                    StateMachine.SetNextState(CheckedStateRef, GameState.WaitForMap);
                    return;
                case GameState.MapReady:
                    SpawnPlayer();
                    StateMachine.SetNextState(CheckedStateRef, GameState.Playing);
                    return;
                case GameState.Playing:
                    if (gameState.Previous is GameState.MapReady)
                        EventBus<StartGame>.Raise(new StartGame());
                    Cursor.lockState = CursorLockMode.Locked;
                    // Activate simulation for player & enemies
                    return;
                case GameState.Paused:
                    EventBus<PauseGame>.Raise(new PauseGame());
                    Cursor.lockState = CursorLockMode.Confined;
                    // TODO: Deactivate simulation for player & enemies
                    return;
                default:
                    return;
            }
        }

        public void OnExit(ref SystemState ecsState, State<GameState> gameState) { }

        public void Transition(ref SystemState ecsState) {
            GameState current = StateMachine.GetState<GameState>(ecsState).Current;
            GameState next = StateMachine.GetNextState<GameState>(ecsState).Value;

            SystemAPI.SetComponentEnabled<NextState<GameState>>(ecsState.SystemHandle, false);

            OnExit(ref ecsState, current);
            OnEnter(ref ecsState, next);
            StateMachine.SetState(ecsState, next);
        }

#endregion
    }
}
