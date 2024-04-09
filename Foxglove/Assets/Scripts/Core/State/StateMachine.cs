﻿using System;
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
        /// Extension method implemented for all systems implementing Unity.Entities.ISystem
        /// Transition a system from it's current state to the requested state
        /// </summary>
        [BurstCompile]
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

        /// <summary>
        /// Same as above but for systems that are subtypes of Unity.Entities.SystemBase
        /// </summary>
        [BurstCompile]
        public static void Transition<TState>(this IStateMachineSystem<TState> system, ref SystemState ecs)
            where TState : unmanaged, Enum {
            TState current = GetState<TState>(ecs).Current;
            TState next = GetNextState<TState>(ecs).Value;

            ecs.EntityManager.SetComponentEnabled<NextState<TState>>(ecs.SystemHandle, false);

            system.OnExit(ref ecs, current);
            system.OnEnter(ref ecs, next);
            SetState(ecs, next);
        }

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
        private static NextState<T> GetNextState<T>(in SystemState ecs)
            where T : unmanaged, Enum => ecs.EntityManager.GetComponentData<NextState<T>>(ecs.SystemHandle);

        /// <summary>
        /// Set the current state of a system
        /// </summary>
        [BurstCompile]
        private static void SetState<T>(in SystemState ecs, in T state)
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
