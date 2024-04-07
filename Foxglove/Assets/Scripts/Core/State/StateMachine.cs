using System;
using Unity.Entities;

namespace Foxglove.Core.State {
    /// <summary>
    /// A collection of helper functions used to manage state machines.
    /// Any system can be turned into a state machine by adding the State and NextState components to it.
    /// </summary>
    public static class StateMachine {
        /// <summary>
        /// Attach State and NextState components to a system
        /// </summary>
        /// <typeparam name="T">Any enum that can be used to represent states </typeparam>
        public static void Init<T>(in SystemState ecs, T initialState)
            where T : unmanaged, Enum {
            ecs.EntityManager.AddComponent<State<T>>(ecs.SystemHandle);
            ecs.EntityManager.AddComponent<NextState<T>>(ecs.SystemHandle);
            ecs.EntityManager.GetComponentDataRW<NextState<T>>(ecs.SystemHandle).ValueRW.Value = initialState;
            ecs.EntityManager.SetComponentEnabled<NextState<T>>(ecs.SystemHandle, true);
        }

        public static bool IsTransitionQueued<T>(in SystemState ecs)
            where T : unmanaged, Enum => ecs.EntityManager.IsComponentEnabled<NextState<T>>(ecs.SystemHandle);

        public static void Transition<TSystem, TState>(this TSystem system, ref SystemState ecs)
            where TState : unmanaged, Enum
            where TSystem : unmanaged, IStateMachineSystem<TState> {
            TState current = GetState<TState>(ecs).Current;
            TState next = GetNextState<TState>(ecs).Value;

            ecs.EntityManager.SetComponentEnabled<NextState<TState>>(ecs.SystemHandle, false);

            system.OnExit(ref ecs, current);
            system.OnEnter(ref ecs, next);
            SetState(ecs, next);
        }
        public static State<T> GetState<T>(in SystemState ecs)
            where T : unmanaged, Enum => ecs.EntityManager.GetComponentData<State<T>>(ecs.SystemHandle);

        public static NextState<T> GetNextState<T>(in SystemState ecs)
            where T : unmanaged, Enum => ecs.EntityManager.GetComponentData<NextState<T>>(ecs.SystemHandle);

        public static void SetState<T>(in SystemState ecs, T state)
            where T : unmanaged, Enum {
            SystemHandle tickSystem = ecs.WorldUnmanaged.GetExistingUnmanagedSystem<FixedTickSystem>();
            var tick = ecs.EntityManager.GetComponentData<Tick>(tickSystem);
            ecs.EntityManager.GetComponentDataRW<State<T>>(ecs.SystemHandle).ValueRW.Set(state, tick);
        }

        public static void SetNextState<T>(in SystemState ecs, T state)
            where T : unmanaged, Enum {
            ecs.EntityManager.GetComponentDataRW<NextState<T>>(ecs.SystemHandle).ValueRW = state;
            ecs.EntityManager.SetComponentEnabled<NextState<T>>(ecs.SystemHandle, true);
        }
    }
}
