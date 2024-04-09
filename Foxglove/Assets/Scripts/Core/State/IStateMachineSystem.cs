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
        /// Called when transitioning into a state
        /// </summary>
        /// <param name="ecsState">ECS information about the system undergoing a state change</param>
        /// <param name="fsmState">The state being transitioned into</param>
        void OnEnter(ref SystemState ecsState, State<T> fsmState);

        /// <summary>
        /// Called when transitioning out of a state
        /// </summary>
        /// <param name="ecsState">ECS information about the system undergoing a state change</param>
        /// <param name="fsmState">The state being transitioned out of</param>
        void OnExit(ref SystemState ecsState, State<T> fsmState);

        void Transition(ref SystemState ecsState);
    }
}
