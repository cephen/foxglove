using System;
using Unity.Entities;

namespace Foxglove.Core.State {
    /// <summary>
    /// Interface systems can implement to handle transitions between states.
    /// States are modeled as enum variants that represent one of the codebases states
    /// </summary>
    public interface IStateMachineSystem<T>
        where T : unmanaged, Enum {
        /// <summary>
        /// Called by <see cref="Transition" /> when transitioning into a state
        /// </summary>
        /// <param name="ecsState">ECS information about the system undergoing a state change</param>
        /// <param name="fsmState">The state being transitioned into</param>
        void OnEnter(ref SystemState ecsState, State<T> fsmState);

        /// <summary>
        /// Called by <see cref="Transition" /> when transitioning out of a state
        /// </summary>
        /// <param name="ecsState">ECS information about the system undergoing a state change</param>
        /// <param name="fsmState">The state being transitioned out of</param>
        void OnExit(ref SystemState ecsState, State<T> fsmState);

        /// <summary>
        /// Manages the transition from one state to another.
        /// Calls OnExit & OnEnter, and provides a place to implement inter-state logic.
        /// </summary>
        /// <param name="ecsState">ECS information about the system undergoing a state change</param>
        void Transition(ref SystemState ecsState);
    }
}
