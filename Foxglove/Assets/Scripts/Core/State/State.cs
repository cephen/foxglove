using System;
using Unity.Entities;

namespace Foxglove.Core.State {
    /// <summary>
    /// Wraps an enum that represents one of the codebases states
    /// Represents the current state of some subset of systems
    /// </summary>
    public struct State<T> : IComponentData
        where T : Enum {
        public T Previous;
        public T Current;

        public Tick EnteredAt { get; private set; }

        public State(T current) {
            Current = current;
            Previous = current;
            EnteredAt = 0;
        }

        /// <summary>
        /// Set Previous to Current
        /// Set Current to Next
        /// Set EnteredAt to current tick
        /// </summary>
        public void Set(T next, Tick tick) => (Previous, Current, EnteredAt) = (Current, next, tick);

        // Allow CurrentState<T> to be converted to T without an explicit cast
        public static implicit operator T(State<T> t) => t.Current;

        // And T to be converted to CurrentState<T>
        public static implicit operator State<T>(T t) => new(t);
    }

    /// <summary>
    /// When attached to a system implementing IStateSystem,
    /// This component can be enabled to request a state transition
    /// </summary>
    public struct NextState<T> : IComponentData, IEnableableComponent
        where T : Enum {
        public T Value;
        public NextState(T value) => Value = value;

        public static implicit operator T(NextState<T> t) => t.Value;
        public static implicit operator NextState<T>(T t) => new(t);
    }
}
