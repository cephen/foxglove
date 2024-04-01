using System;
using Unity.Entities;

namespace Foxglove.State {
    /// <summary>
    /// Wraps an enum that represents one of the codebases states
    /// Represents the current state of some subset of systems
    /// </summary>
    public struct CurrentState<T> : IComponentData
        where T : Enum {
        public T Value;
        public CurrentState(T value) => Value = value;

        // Allow CurrentState<T> to be converted to T
        public static implicit operator T(CurrentState<T> t) => t.Value;

        // And T to be converted to CurrentState<T>
        public static implicit operator CurrentState<T>(T t) => new(t);
    }
}
