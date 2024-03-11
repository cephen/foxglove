using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Agent {
    public struct Blackboard : IComponentData {
        public Entity PlayerEntity;
        public float3 PlayerPosition;

        public static Blackboard Default() => new() {
            PlayerEntity = Entity.Null,
            PlayerPosition = float3.zero,
        };
    }
}
