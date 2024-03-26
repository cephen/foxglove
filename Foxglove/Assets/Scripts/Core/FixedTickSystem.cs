using Unity.Burst;
using Unity.Entities;

namespace Foxglove {
    /// <summary>
    /// This system tracks how many fixed updates have happened since the start of the game
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
    public partial struct FixedTickSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.EntityManager.CreateOrAddSingleton<State>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) => SystemAPI.GetSingletonRW<State>().ValueRW.Tick++;

        public void OnDestroy(ref SystemState state) { }

        public struct State : IComponentData {
            public uint Tick;
        }
    }
}
