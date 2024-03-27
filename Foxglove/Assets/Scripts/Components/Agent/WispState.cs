using Unity.Entities;

namespace Foxglove.Agent {
    public struct WispState : IComponentData {
        public enum State : byte {
            Inactive,
            Spawn,
            Patrol,
            Attack,
            Die,
            Despawn,
        }

        public State Current;
        public State Previous;

        public static WispState Default() => new() {
            Current = State.Spawn,
            Previous = State.Inactive,
        };
    }
}
