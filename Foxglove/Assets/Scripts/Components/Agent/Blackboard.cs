using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Agent {
    /// <summary>
    /// Container for data used for AI decision making
    /// Only one of these exists at runtime, so it can be fetched and modified like a singleton
    /// </summary>
    public struct Blackboard : IComponentData {
        public Entity PlayerEntity;
        public float3 PlayerPosition;

        public static Blackboard Default() => new() {
            PlayerEntity = Entity.Null,
            PlayerPosition = float3.zero,
        };
    }
}
