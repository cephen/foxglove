using System;
using Foxglove.Character;
using Foxglove.Core.State;
using Foxglove.Maps;
using SideFX.Events;
using SideFX.SceneManagement;
using SideFX.SceneManagement.Events;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

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
        private Random _rng;

        protected override void OnCreate() {
            StateMachine.Init(CheckedStateRef, GameState.Waiting);
            _rng = new Random((uint)DateTimeOffset.UtcNow.GetHashCode());

            // Initialize event bindings
            _mapReadyBinding = new EventBinding<MapReadyEvent>(OnMapReady);
            EventBus<MapReadyEvent>.Register(_mapReadyBinding);
            _sceneReadyBinding = new EventBinding<SceneReady>(OnSceneReady);
            EventBus<SceneReady>.Register(_sceneReadyBinding);
        }

        protected override void OnDestroy() {
            EventBus<MapReadyEvent>.Deregister(_mapReadyBinding);
            EventBus<SceneReady>.Deregister(_sceneReadyBinding);
        }

        protected override void OnUpdate() {
            if (StateMachine.IsTransitionQueued<GameState>(CheckedStateRef)) Transition(ref CheckedStateRef);

            State<GameState> state = StateMachine.GetState<GameState>(CheckedStateRef);
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
                Position = new float3(room.Center.x, 10f, room.Center.y),
            };
            EventBus<SpawnCharacterEvent>.Raise(spawnRequest);
        }

        private void OnMapReady() {
            if (StateMachine.GetState<GameState>(CheckedStateRef).Current is not GameState.WaitForMap) return;

            StateMachine.SetNextState(CheckedStateRef, GameState.MapReady);
        }

        private void OnSceneReady(SceneReady e) {
            if (e.Scene is not GameplayScene gameplay) return;

            if (StateMachine.GetState<GameState>(CheckedStateRef).Current is GameState.Waiting)
                StateMachine.SetNextState(CheckedStateRef, GameState.Startup);
        }

#region IStateMachine Implementation

        public void OnEnter(ref SystemState ecsState, State<GameState> fsmState) {
            switch (fsmState.Current) {
                case GameState.Startup:
                    EventBus<BuildMapEvent>.Raise(default);
                    StateMachine.SetNextState(CheckedStateRef, GameState.WaitForMap);
                    return;
                case GameState.MapReady:
                    SpawnPlayer();
                    StateMachine.SetNextState(CheckedStateRef, GameState.Playing);
                    return;
                case GameState.Playing:
                    EventBus<ToggleSpawnersEvent>.Raise(new ToggleSpawnersEvent { Enabled = true });
                    return;
                case GameState.Paused:
                    EventBus<ToggleSpawnersEvent>.Raise(new ToggleSpawnersEvent { Enabled = false });
                    return;
                default:
                    return;
            }
        }

        public void OnExit(ref SystemState ecsState, State<GameState> fsmState) { }

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
