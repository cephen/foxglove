using System;
using Unity.Entities;

namespace Foxglove.Core.State {
    /// <summary>
    /// Interface systems can implement to handle transitions between states.
    /// States are modeled as enum variants that represent one of the codebases states
    /// </summary>
    public interface IStateMachineSystem<T>
        where T : unmanaged, Enum {
        void OnEnter(ref SystemState ecsState, State<T> fsmState);
        void OnExit(ref SystemState ecsState, State<T> fsmState);
    }
}
