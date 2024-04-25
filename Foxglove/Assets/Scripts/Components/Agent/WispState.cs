using Unity.Entities;

namespace Foxglove.Agent {
    /// <summary>
    /// Represents the current state of an individual wisp.
    /// This is managed by the WispSystem, via scheduling a WispStateMachineJob
    /// </summary>
    public struct WispState : IComponentData {
        public enum State : byte {
            Spawn, // Reset stats and transition to patrol
            Patrol, // Hunt down the player
            Attack, // Launch a projectile if in range, transition to Patrol
            Dying, // Ragdoll, despawn in one second
        }

        public State Current;

        public static WispState Default() => new() { Current = State.Spawn };

        public void TransitionTo(State next) => Current = next;
    }
}
