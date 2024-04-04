using System;
using Unity.Entities;

namespace Foxglove.State {
    public interface IStateMachineSystem<T>
        where T : unmanaged, Enum {
        void OnEnter(ref SystemState ecs, State<T> state);
        void OnExit(ref SystemState ecs, State<T> state);
        void Transition(ref SystemState ecs);
    }
}
