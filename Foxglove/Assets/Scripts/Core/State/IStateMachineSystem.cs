using System;
using Unity.Entities;

namespace Foxglove.State {
    /// <summary>
    /// Interface systems can implement to handle transitions between states.
    /// States are modeled as enum variants that represent one of the codebases states
    /// </summary>
    public interface IStateMachineSystem<T>
        where T : unmanaged, Enum {
        void OnEnter(ref SystemState ecs, State<T> state);
        void OnExit(ref SystemState ecs, State<T> state);
        void Transition(ref SystemState ecs);
    }
}
