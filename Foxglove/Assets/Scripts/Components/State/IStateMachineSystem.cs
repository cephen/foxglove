using System;
using Unity.Entities;

namespace Foxglove.State {
    public interface IStateMachineSystem<T>
        where T : unmanaged, Enum {
        void OnEnter(ref SystemState ecsState, State<T> state);
        void OnExit(ref SystemState ecsState, State<T> state);

        void Transition(ref SystemState state);
    }
}
