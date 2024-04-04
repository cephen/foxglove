using System;
using Foxglove.Maps;
using Foxglove.State;
using Unity.Burst;
using Unity.Entities;
using Unity.Logging;
using Random = Unity.Mathematics.Random;

[assembly: RegisterGenericComponentType(typeof(State<GameState>))]
[assembly: RegisterGenericComponentType(typeof(NextState<GameState>))]

namespace Foxglove.State {
    public enum GameState : byte { Initialize, Generate, Play }

    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    internal partial struct GameManagerSystem : ISystem, IStateMachineSystem<GameState> {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Tick>();
            StateMachine.Init(ref state, GameState.Initialize);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (StateMachine.IsTransitionQueued<GameState>(ref state)) Transition(ref state);
            RunStateUpdate(ref state);
        }

        public void OnDestroy(ref SystemState state) { }

        public void Transition(ref SystemState ecs) {
            SystemAPI.SetComponentEnabled<NextState<GameState>>(ecs.SystemHandle, false);

            OnExit(ref ecs, StateMachine.GetState<GameState>(ref ecs));
            StateMachine.SetState(ref ecs, StateMachine.GetNextState<GameState>(ref ecs).Value);
            OnEnter(ref ecs, StateMachine.GetState<GameState>(ref ecs));
        }

        public void OnEnter(ref SystemState ecs, State<GameState> state) {
            switch (state.Current) {
                case GameState.Initialize:
                    Log.Debug("[GameManager] Initializing Foxglove");
                    StateMachine.SetNextState(ref ecs, GameState.Generate);
                    break;
                case GameState.Generate:
                    Log.Debug("[GameManager] Starting Map generation");

                    MapConfig config = new() {
                        Seed = new Random((uint)DateTimeOffset.UtcNow.GetHashCode()).NextUInt(),
                        Radius = 50,
                        MinRoomSize = 5,
                        MaxRoomSize = 10,
                        RoomsToGenerate = 30,
                    };

                    SystemHandle mapGenSystem =
                        ecs.WorldUnmanaged.GetExistingUnmanagedSystem<MapGeneratorSystem>();
                    SystemAPI.SetComponent<ShouldGenerateMap>(mapGenSystem, config);
                    SystemAPI.SetComponentEnabled<ShouldGenerateMap>(mapGenSystem, true);

                    StateMachine.SetNextState(ref ecs, GameState.Play);
                    break;
                case GameState.Play:
                    Log.Debug("[GameManager] Starting gameplay");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnExit(ref SystemState ecs, State<GameState> state) {
            switch (state.Current) {
                case GameState.Initialize:
                    Log.Debug("[GameManager] Initialization complete");
                    break;
                case GameState.Generate:
                    Log.Debug("[GameManager] Map generation complete");
                    break;
                case GameState.Play:
                    Log.Debug("[GameManager] Game finished");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [BurstCompile]
        private void RunStateUpdate(ref SystemState state) {
            var currentState = SystemAPI.GetComponent<State<GameState>>(state.SystemHandle);

            switch (currentState.Current) {
                case GameState.Initialize: return; // Nothing to do in update loop for this state
                case GameState.Generate:
                    // Monitor generator system and transition to Play state when it's complete
                    SystemState generatorEcsState = state.WorldUnmanaged.GetExistingSystemState<MapGeneratorSystem>();
                    State<GeneratorState> generatorState = StateMachine.GetState<GeneratorState>(ref generatorEcsState);
                    if (generatorState.Current is GeneratorState.Idle
                        && generatorState.Previous is GeneratorState.Cleanup)
                        StateMachine.SetNextState(ref state, GameState.Play);
                    return;
                case GameState.Play:
                    uint currentTick = SystemAPI.GetSingleton<Tick>();
                    if (currentTick - currentState.ChangedAt > 50)
                        StateMachine.SetNextState(ref state, GameState.Generate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
