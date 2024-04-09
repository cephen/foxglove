using System;
using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Core.State {
    /// <summary>
    /// A collection of helper functions used to manage systems that are state machines.
    /// Any system can be turned into a state machine by adding the State and NextState components to it.
    /// </summary>
    [BurstCompile]
    public static class StateMachine {
        /// <summary>
        /// Attach State and NextState components to a system
        /// </summary>
        /// <typeparam name="T">Any enum that can be used to represent states </typeparam>
        [BurstCompile]
        public static void Init<T>(in SystemState ecs, T initialState)
            where T : unmanaged, Enum {
            ecs.EntityManager.AddComponent<State<T>>(ecs.SystemHandle);
            ecs.EntityManager.AddComponent<NextState<T>>(ecs.SystemHandle);
            ecs.EntityManager.GetComponentDataRW<NextState<T>>(ecs.SystemHandle).ValueRW.Value = initialState;
            ecs.EntityManager.SetComponentEnabled<NextState<T>>(ecs.SystemHandle, true);
        }

        /// <summary>
        /// Helper function that checks whether a system has a state transition requested
        /// This is done by checking if the NextState component is enabled
        /// </summary>
        [BurstCompile]
        public static bool IsTransitionQueued<T>(in SystemState ecs)
            where T : unmanaged, Enum => ecs.EntityManager.IsComponentEnabled<NextState<T>>(ecs.SystemHandle);

        /// <summary>
        /// Get the current State of a system.
        /// The generic parameter T must be specified
        /// because a system may have more than one kind of State attached to it.
        /// </summary>
        [BurstCompile]
        public static State<T> GetState<T>(in SystemState ecs)
            where T : unmanaged, Enum => ecs.EntityManager.GetComponentData<State<T>>(ecs.SystemHandle);

        /// <summary>
        /// Get the NextState(T) component attached to a system.
        /// This method does not check if the NextState component is enabled.
        /// </summary>
        [BurstCompile]
        public static NextState<T> GetNextState<T>(in SystemState ecs)
            where T : unmanaged, Enum => ecs.EntityManager.GetComponentData<NextState<T>>(ecs.SystemHandle);

        /// <summary>
        /// Set the current state of a system
        /// </summary>
        [BurstCompile]
        public static void SetState<T>(in SystemState ecs, in T state)
            where T : unmanaged, Enum {
            SystemHandle tickSystem = ecs.WorldUnmanaged.GetExistingUnmanagedSystem<FixedTickSystem>();
            var tick = ecs.EntityManager.GetComponentData<Tick>(tickSystem);
            ecs.EntityManager.GetComponentDataRW<State<T>>(ecs.SystemHandle).ValueRW.Set(state, tick);
        }

        /// <summary>
        /// Set the next state of a system, and enable the NextState component to request a transition
        /// </summary>
        [BurstCompile]
        public static void SetNextState<T>(in SystemState ecs, in T state)
            where T : unmanaged, Enum {
            ecs.EntityManager.GetComponentDataRW<NextState<T>>(ecs.SystemHandle).ValueRW = state;
            ecs.EntityManager.SetComponentEnabled<NextState<T>>(ecs.SystemHandle, true);
        }
    }
}
