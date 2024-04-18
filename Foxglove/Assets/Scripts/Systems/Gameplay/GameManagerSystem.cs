using System;
using Foxglove.Character;
using Foxglove.Core.State;
using Foxglove.Maps;
using SideFX.Events;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Foxglove.Gameplay {
    internal enum GameState {
        Startup,
        WaitForMap,
        MapReady,
        Playing,
    }

    internal sealed partial class GameManagerSystem : SystemBase, IStateMachineSystem<GameState> {
        private bool _mapIsReady;
        private EventBinding<MapReadyEvent> _mapReadyBinding;
        private Random _rng;

        protected override void OnCreate() {
            StateMachine.Init(CheckedStateRef, GameState.Startup);
            _rng = new Random((uint)DateTimeOffset.UtcNow.GetHashCode());
            _mapReadyBinding = new EventBinding<MapReadyEvent>(OnMapReady);
            EventBus<MapReadyEvent>.Register(_mapReadyBinding);
        }

        protected override void OnDestroy() {
            EventBus<MapReadyEvent>.Deregister(_mapReadyBinding);
        }

        protected override void OnUpdate() {
            if (StateMachine.IsTransitionQueued<GameState>(CheckedStateRef)) Transition(ref CheckedStateRef);

            State<GameState> state = StateMachine.GetState<GameState>(CheckedStateRef);


            if (state.Current is GameState.WaitForMap) {
                if (!_mapIsReady) return;

                StateMachine.SetNextState(CheckedStateRef, GameState.MapReady);
                _mapIsReady = false;
            }
        }

        private void SpawnPlayer() {
            Entity mapEntity = SystemAPI.GetSingletonEntity<Map>();
            DynamicBuffer<Room> rooms = SystemAPI.GetBuffer<Room>(mapEntity);
            int roomIndex = _rng.NextInt() % rooms.Length;
            Room room = rooms[roomIndex];
            var spawnRequest = new SpawnCharacterEvent {
                Character = SpawnableCharacter.Player,
                Position = new float3(room.Center.x, 0.1f, room.Center.y),
            };
            EventBus<SpawnCharacterEvent>.Raise(spawnRequest);
        }

        private void OnMapReady() {
            if (StateMachine.GetState<GameState>(CheckedStateRef).Current is GameState.WaitForMap)
                StateMachine.SetNextState(CheckedStateRef, GameState.MapReady);
        }

#region IStateMachine Implementation

        public void OnEnter(ref SystemState ecsState, State<GameState> fsmState) {
            switch (fsmState.Current) {
                case GameState.Startup:
                    EventBus<BuildMapEvent>.Raise(default);
                    StateMachine.SetNextState(CheckedStateRef, GameState.WaitForMap);
                    return;
                case GameState.WaitForMap:
                    return;
                case GameState.MapReady:
                    SpawnPlayer();
                    StateMachine.SetNextState(CheckedStateRef, GameState.Playing);
                    return;
                case GameState.Playing:
                    return;
                default:
                    return;
            }
        }

        public void OnExit(ref SystemState ecsState, State<GameState> fsmState) {
            switch (fsmState.Current) {
                case GameState.Startup:
                    return;
                case GameState.WaitForMap:
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
