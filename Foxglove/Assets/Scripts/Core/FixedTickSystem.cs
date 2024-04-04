using Unity.Burst;
using Unity.Entities;

namespace Foxglove {
    /// <summary>
    /// This system tracks how many fixed updates have happened since the start of the game
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
    public partial struct FixedTickSystem : ISystem {
        public void OnCreate(ref SystemState state) => state.EntityManager.AddComponent<Tick>(state.SystemHandle);

        [BurstCompile]
        public void OnUpdate(ref SystemState state) =>
            SystemAPI.GetComponentRW<Tick>(state.SystemHandle).ValueRW.Value++;

        public void OnDestroy(ref SystemState state) { }
    }

    public struct Tick : IComponentData {
        public uint Value;
        public static implicit operator uint(Tick t) => t.Value;
        public static implicit operator Tick(uint t) => new() { Value = t };
    }
}
