using System;
using Unity.Entities;

namespace Foxglove.State {
    /// <summary>
    /// Added to a State System to request a state transition
    /// </summary>
    public struct NextState<T> : IComponentData, IEnableableComponent
        where T : Enum {
        public T Value;
        public NextState(T value) => Value = value;

        // Allow NextState<T> to be converted to T
        public static implicit operator T(NextState<T> t) => t.Value;

        // And T to be converted to NextState<T>
        public static implicit operator NextState<T>(T t) => new(t);
    }
}
