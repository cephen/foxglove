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
        private EventBinding<ResumeEvent> _resumeBinding;
        private EventBinding<ShutdownEvent> _shutdownBinding;

        private Random _rng;

        protected override void OnCreate() {
            StateMachine.Init(CheckedStateRef, GameState.Waiting);
            _rng = new Random((uint)DateTimeOffset.UtcNow.GetHashCode());

            // Initialize event bindings
            _mapReadyBinding = new EventBinding<MapReadyEvent>(OnMapReady);
            _sceneReadyBinding = new EventBinding<SceneReady>(OnSceneReady);
            _resumeBinding = new EventBinding<ResumeEvent>(OnResume);
            _shutdownBinding = new EventBinding<ShutdownEvent>(OnShutdown);

            // Register event bindings
            EventBus<MapReadyEvent>.Register(_mapReadyBinding);
            EventBus<SceneReady>.Register(_sceneReadyBinding);
            EventBus<ResumeEvent>.Register(_resumeBinding);
            EventBus<ShutdownEvent>.Register(_shutdownBinding);
        }

        protected override void OnDestroy() {
            EventBus<MapReadyEvent>.Deregister(_mapReadyBinding);
            EventBus<SceneReady>.Deregister(_sceneReadyBinding);
            EventBus<ResumeEvent>.Deregister(_resumeBinding);
            EventBus<ShutdownEvent>.Deregister(_shutdownBinding);
        }

        protected override void OnUpdate() {
            if (StateMachine.IsTransitionQueued<GameState>(CheckedStateRef)) Transition(ref CheckedStateRef);
            var tick = SystemAPI.GetSingleton<Tick>();
            var inputState = SystemAPI.GetSingleton<InputState>();

            State<GameState> state = StateMachine.GetState<GameState>(CheckedStateRef);
            if (state.Current is GameState.Playing
                && inputState.Pause.IsSet(tick)
            ) StateMachine.SetNextState(CheckedStateRef, GameState.Paused);
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

        private void OnResume(ResumeEvent _) {
            if (StateMachine.GetState<GameState>(CheckedStateRef).Current is GameState.Paused)
                StateMachine.SetNextState(CheckedStateRef, GameState.Playing);
        }

        private void OnShutdown(ShutdownEvent _) {
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
                        EventBus<StartGameEvent>.Raise(new StartGameEvent());
                    Cursor.lockState = CursorLockMode.Locked;
                    // Activate simulation for player & enemies
                    return;
                case GameState.Paused:
                    EventBus<PauseEvent>.Raise(new PauseEvent());
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
