using Foxglove.Core.State;
using Foxglove.Maps;
using SideFX.Events;
using Unity.Entities;

namespace Foxglove.Gameplay {
    internal enum GameState { Startup, WaitForMap, MapReady }

    internal sealed partial class GameManagerSystem : SystemBase, IStateMachineSystem<GameState> {
        private bool _mapIsReady;
        private EventBinding<MapReadyEvent> _mapReadyBinding;

        protected override void OnCreate() {
            _mapReadyBinding = new EventBinding<MapReadyEvent>(OnMapReady);
            EventBus<MapReadyEvent>.Register(_mapReadyBinding);
            StateMachine.Init(CheckedStateRef, GameState.Startup);
        }

        protected override void OnDestroy() {
            EventBus<MapReadyEvent>.Deregister(_mapReadyBinding);
        }

        protected override void OnUpdate() {
            if (StateMachine.IsTransitionQueued<GameState>(CheckedStateRef)) Transition(ref CheckedStateRef);

            State<GameState> state = StateMachine.GetState<GameState>(CheckedStateRef);

            switch (state.Current) {
                case GameState.Startup:
                    return;
                case GameState.WaitForMap:
                    if (!_mapIsReady) return;

                    StateMachine.SetNextState(CheckedStateRef, GameState.MapReady);
                    _mapIsReady = false;

                    return;
                case GameState.MapReady:
                    return;
                default:
                    return;
            }
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
