using Foxglove.Character;
using Foxglove.Core;
using Foxglove.Core.State;
using Foxglove.Input;
using Foxglove.Maps;
using SideFX.Events;
using SideFX.SceneManagement;
using SideFX.SceneManagement.Events;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Foxglove.Gameplay {
    /// <summary>
    /// A state machine which manages the core game loop.
    /// States are represented by the <see cref="Foxglove.Gameplay.GameState" /> enum,
    /// which can be found at `Scripts/Components/Gameplay/GameState.cs`.
    /// ------------------------------------------------------------------------------
    /// State behaviour for this system happens during transitions between states,
    /// the transitions themselves are triggered by events from other systems.
    /// For example, when moving from GameState.MainMenu to GameState.CreateGame, this system:
    /// - Tells the map generator to generate a new map by enabling a component on the map singleton entity
    /// - Schedules a transition to GameState.WaitForMap, which waits to receive a <see cref="MapReadyEvent" />
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    internal sealed partial class GameManagerSystem : SystemBase, IStateMachineSystem<GameState> {
        private EventBinding<SceneReady> _sceneReadyBinding;
        private EventBinding<MapReadyEvent> _mapReadyBinding;
        private EventBinding<PlayerDied> _playerDiedBinding;
        private EventBinding<ResumeGame> _resumeBinding;
        private EventBinding<PauseGame> _pauseBinding;
        private EventBinding<QuitToMenu> _quitToMenuBinding;
        private EventBinding<Shutdown> _shutdownBinding;

        private GameState CurrentState => StateMachine.GetState<GameState>(CheckedStateRef).Current;

        /// <summary>
        /// Called once when the game starts.
        /// Hook up event bindings and initialize system state.
        /// </summary>
        protected override void OnCreate() {
            StateMachine.Init(CheckedStateRef, GameState.MainMenu);

            // Initialize event bindings
            _mapReadyBinding = new EventBinding<MapReadyEvent>(OnMapReady);
            _sceneReadyBinding = new EventBinding<SceneReady>(OnSceneReady);
            _playerDiedBinding = new EventBinding<PlayerDied>(OnPlayerDied);
            _resumeBinding = new EventBinding<ResumeGame>(OnResume);
            _pauseBinding = new EventBinding<PauseGame>(OnPause);
            _shutdownBinding = new EventBinding<Shutdown>(OnShutdown);
            _quitToMenuBinding = new EventBinding<QuitToMenu>(OnQuitToMenu);

            // Register event bindings
            EventBus<MapReadyEvent>.Register(_mapReadyBinding);
            EventBus<SceneReady>.Register(_sceneReadyBinding);
            EventBus<PlayerDied>.Register(_playerDiedBinding);
            EventBus<ResumeGame>.Register(_resumeBinding);
            EventBus<PauseGame>.Register(_pauseBinding);
            EventBus<Shutdown>.Register(_shutdownBinding);
            EventBus<QuitToMenu>.Register(_quitToMenuBinding);
        }

        /// <summary>
        /// Called once when the game ends.
        /// </summary>
        protected override void OnDestroy() {
            EventBus<MapReadyEvent>.Deregister(_mapReadyBinding);
            EventBus<SceneReady>.Deregister(_sceneReadyBinding);
            EventBus<PlayerDied>.Deregister(_playerDiedBinding);
            EventBus<ResumeGame>.Deregister(_resumeBinding);
            EventBus<PauseGame>.Deregister(_pauseBinding);
            EventBus<Shutdown>.Deregister(_shutdownBinding);
            EventBus<QuitToMenu>.Deregister(_quitToMenuBinding);
        }

        /// <summary>
        /// Called during fixed update
        /// </summary>
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
                && inputState.Pause.IsSet(tick)
            ) {
                // Toggle pause by sending an event depending on current state
                if (state.Current == GameState.Playing) EventBus<PauseGame>.Raise(new PauseGame());
                else if (state.Current == GameState.Paused) EventBus<ResumeGame>.Raise(new ResumeGame());
            }
        }

#region IStateMachine Implementation

        public void OnEnter(ref SystemState ecsState, State<GameState> gameState) {
            switch (gameState.Current) {
                case GameState.MainMenu:
                    Cursor.lockState = CursorLockMode.None;

                    return;
                case GameState.CreateGame:
                    Entity mapEntity = SystemAPI.GetSingletonEntity<Map>();
                    SystemAPI.SetComponentEnabled<ShouldBuild>(mapEntity, true);

                    return;
                case GameState.Playing:
                    Cursor.lockState = CursorLockMode.Locked;

                    return;
                case GameState.GameOver:
                    StateMachine.SetNextState(CheckedStateRef, GameState.MainMenu);
                    return;
                default:
                    return;
            }
        }

        public void OnExit(ref SystemState ecsState, State<GameState> gameState) {
            switch (gameState.Current) {
                case GameState.CreateGame:
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

#region Event Handlers

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

        private void OnMapReady(MapReadyEvent ready) {
            if (CurrentState is GameState.CreateGame or GameState.BuildNextLevel) {
                EventBus<SpawnRequest>.Raise(
                    new SpawnRequest {
                        Spawnable = Spawnable.Player,
                        Position = ready.PlayerLocation,
                    }
                );
                EventBus<SpawnRequest>.Raise(
                    new SpawnRequest {
                        Spawnable = Spawnable.Teleporter,
                        Position = ready.TeleporterLocation,
                    }
                );
                StateMachine.SetNextState(CheckedStateRef, GameState.Playing);
            }
        }

        private void OnPause(PauseGame _) {
            if (CurrentState is GameState.Playing)
                StateMachine.SetNextState(CheckedStateRef, GameState.Paused);
        }

        private void OnResume(ResumeGame _) {
            if (CurrentState is GameState.Paused)
                StateMachine.SetNextState(CheckedStateRef, GameState.Playing);
        }

        private void OnQuitToMenu(QuitToMenu _) {
            if (CurrentState is GameState.Playing or GameState.Paused) {
                EventBus<DespawnMapCommand>.Raise(new DespawnMapCommand());
                StateMachine.SetNextState(CheckedStateRef, GameState.MainMenu);
            }
        }

        private void OnPlayerDied(PlayerDied _) {
            if (CurrentState is not GameState.Playing) return;
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
    }
}
