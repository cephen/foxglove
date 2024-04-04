using System;
using Unity.Entities;

namespace Foxglove.State {
    /// <summary>
    /// Wraps an enum that represents one of the codebases states
    /// Represents the current state of some subset of systems
    /// </summary>
    public struct State<T> : IComponentData
        where T : Enum {
        public T Previous;
        public T Current;

        public uint ChangedAt { get; private set; }

        public State(T current) {
            Current = current;
            Previous = current;
            ChangedAt = 0;
        }

        public void Set(T next, Tick tick) => (Previous, Current, ChangedAt) = (Current, next, tick);

        // Allow CurrentState<T> to be converted to T
        public static implicit operator T(State<T> t) => t.Current;

        // And T to be converted to CurrentState<T>
        public static implicit operator State<T>(T t) => new(t);
    }
}
