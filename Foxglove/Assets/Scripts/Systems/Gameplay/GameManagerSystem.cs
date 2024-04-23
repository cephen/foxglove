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
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    internal sealed partial class GameManagerSystem : SystemBase, IStateMachineSystem<GameState> {
        private EventBinding<SceneReady> _sceneReadyBinding;
        private EventBinding<MapReadyEvent> _mapReadyBinding;
        private EventBinding<ResumeGame> _resumeBinding;
        private EventBinding<PauseGame> _pauseBinding;
        private EventBinding<ExitGame> _exitGameBinding;
        private EventBinding<Shutdown> _shutdownBinding;

        private Random _rng;

        private State<GameState> GenState => StateMachine.GetState<GameState>(CheckedStateRef);


        protected override void OnCreate() {
            StateMachine.Init(CheckedStateRef, GameState.MainMenu);
            _rng = new Random((uint)DateTimeOffset.UtcNow.GetHashCode());

            // Initialize event bindings
            _mapReadyBinding = new EventBinding<MapReadyEvent>(OnMapReady);
            _sceneReadyBinding = new EventBinding<SceneReady>(OnSceneReady);
            _resumeBinding = new EventBinding<ResumeGame>(OnResume);
            _pauseBinding = new EventBinding<PauseGame>(OnPause);
            _shutdownBinding = new EventBinding<Shutdown>(OnShutdown);
            _exitGameBinding = new EventBinding<ExitGame>(OnExitGame);

            // Register event bindings
            EventBus<MapReadyEvent>.Register(_mapReadyBinding);
            EventBus<SceneReady>.Register(_sceneReadyBinding);
            EventBus<ResumeGame>.Register(_resumeBinding);
            EventBus<PauseGame>.Register(_pauseBinding);
            EventBus<Shutdown>.Register(_shutdownBinding);
            EventBus<ExitGame>.Register(_exitGameBinding);
        }

        protected override void OnDestroy() {
            EventBus<MapReadyEvent>.Deregister(_mapReadyBinding);
            EventBus<SceneReady>.Deregister(_sceneReadyBinding);
            EventBus<ResumeGame>.Deregister(_resumeBinding);
            EventBus<PauseGame>.Deregister(_pauseBinding);
            EventBus<Shutdown>.Deregister(_shutdownBinding);
            EventBus<ExitGame>.Deregister(_exitGameBinding);
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
            if (state.Current is (GameState.Playing or GameState.Paused)
                // and the pause button was pressed this tick
                && inputState.Pause.IsSet(tick)) {
                // Toggle pause by sending an event depending on current state
                if (state.Current == GameState.Playing) EventBus<PauseGame>.Raise(new PauseGame());
                else if (state.Current == GameState.Paused) EventBus<ResumeGame>.Raise(new ResumeGame());
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

        private void OnSceneReady(SceneReady e) {
            switch (e.Scene) {
                case MainMenuScene:
                    StateMachine.SetNextState(CheckedStateRef, GameState.MainMenu);
                    return;
                case GameplayScene:
                    StateMachine.SetNextState(CheckedStateRef, GameState.CreateGame);
                    return;
                default: return;
            }
        }

        private void OnMapReady(MapReadyEvent _) {
            if (GenState.Current is GameState.CreateGame or GameState.BuildNextLevel)
                StateMachine.SetNextState(CheckedStateRef, GameState.Playing);
        }

        private void OnPause(PauseGame _) {
            if (GenState.Current is GameState.Playing)
                StateMachine.SetNextState(CheckedStateRef, GameState.Paused);
        }

        private void OnResume(ResumeGame _) {
            if (GenState.Current is GameState.Paused)
                StateMachine.SetNextState(CheckedStateRef, GameState.Playing);
        }

        private void OnExitGame(ExitGame _) {
            if (GenState.Current is GameState.Playing or GameState.Paused)
                StateMachine.SetNextState(CheckedStateRef, GameState.GameOver);
        }

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
                case GameState.MainMenu:
                    Cursor.lockState = CursorLockMode.None;

                    if (gameState.Previous is not GameState.GameOver) return;

                    return;
                case GameState.CreateGame:
                    Entity mapEntity = SystemAPI.GetSingletonEntity<Map>();
                    SystemAPI.SetComponentEnabled<ShouldBuild>(mapEntity, true);

                    return;
                case GameState.Playing:
                    Cursor.lockState = CursorLockMode.Locked;
                    return;
                case GameState.GameOver:
                    EventBus<DespawnMapCommand>.Raise(new DespawnMapCommand());
                    StateMachine.SetNextState(CheckedStateRef, GameState.MainMenu);
                    return;
                default:
                    return;
            }
        }

        public void OnExit(ref SystemState ecsState, State<GameState> gameState) {
            switch (gameState.Current) {
                case GameState.CreateGame:
                    SpawnPlayer();
                    EventBus<GameReady>.Raise(new GameReady());
                    return;
                case GameState.Playing:
                    Cursor.lockState = CursorLockMode.Confined;
                    return;
                default:
                    return;
            }
        }

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
