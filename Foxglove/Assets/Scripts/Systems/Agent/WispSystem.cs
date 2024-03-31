using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Agent {
    [BurstCompile]
    [UpdateInGroup(typeof(AgentSimulationGroup))]
    internal partial struct WispSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<RandomNumberGenerators>();
            state.RequireForUpdate<Blackboard>();
            state.RequireForUpdate<Tick>();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            EntityCommandBuffer commands = SystemAPI
                .GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new WispStateMachineJob {
                Commands = commands.AsParallelWriter(),
                PlayerPosition = SystemAPI.GetSingleton<Blackboard>().PlayerPosition,
                Tick = SystemAPI.GetSingleton<Tick>().Value,
                Rng = SystemAPI.GetSingleton<RandomNumberGenerators>().Base,
            }.ScheduleParallel(state.Dependency);
        }
    }
}
