using System;
using Unity.Entities;

namespace Foxglove.State {
    public static class StateMachine {
        public static void Init<T>(ref SystemState state, T initialState)
            where T : unmanaged, Enum {
            state.EntityManager.AddComponent<State<T>>(state.SystemHandle);
            state.EntityManager.AddComponent<NextState<T>>(state.SystemHandle);
            state.EntityManager.GetComponentDataRW<NextState<T>>(state.SystemHandle).ValueRW.Value = initialState;
            state.EntityManager.SetComponentEnabled<NextState<T>>(state.SystemHandle, true);
        }

        public static bool IsTransitionQueued<T>(ref SystemState state)
            where T : unmanaged, Enum =>
            state.EntityManager.IsComponentEnabled<NextState<T>>(state.SystemHandle);

        public static State<T> GetState<T>(ref SystemState state)
            where T : unmanaged, Enum =>
            state.EntityManager.GetComponentData<State<T>>(state.SystemHandle);

        public static NextState<T> GetNextState<T>(ref SystemState state)
            where T : unmanaged, Enum =>
            state.EntityManager.GetComponentData<NextState<T>>(state.SystemHandle);

        public static void SetState<T>(ref SystemState state, T current)
            where T : unmanaged, Enum {
            SystemHandle tickSystem = state.World.GetOrCreateSystem<FixedTickSystem>();
            var tick = state.EntityManager.GetComponentData<Tick>(tickSystem);
            state.EntityManager.GetComponentDataRW<State<T>>(state.SystemHandle).ValueRW.Set(current, tick);
        }

        public static void SetNextState<T>(ref SystemState state, T next)
            where T : unmanaged, Enum {
            state.EntityManager.GetComponentDataRW<NextState<T>>(state.SystemHandle).ValueRW = next;
            state.EntityManager.SetComponentEnabled<NextState<T>>(state.SystemHandle, true);
        }
    }
}
