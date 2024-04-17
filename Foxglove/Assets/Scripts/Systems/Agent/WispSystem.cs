using System;
using Foxglove.Core;
using Unity.Burst;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace Foxglove.Agent {
    [BurstCompile]
    [UpdateInGroup(typeof(AgentSimulationGroup))]
    internal partial struct WispSystem : ISystem {
        private Random _rng;

        public void OnCreate(ref SystemState state) {
            _rng = new Random((uint)DateTimeOffset.UtcNow.GetHashCode());
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
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
                Rng = new Random(_rng.NextUInt()),
            }.ScheduleParallel(state.Dependency);
        }
    }
}
