using System;
using Foxglove.Core;
using Unity.Burst;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace Foxglove.Agent {
    /// <summary>
    /// The system configures and schedules a <see cref="WispStateMachineJob" /> that manages each wisp's state
    /// </summary>
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
            // Command buffers are used to schedule structural changes on entities
            // This includes adding/removing/enabling/disabling components
            EntityCommandBuffer commands = SystemAPI
                // Commands written to this buffer will be played back
                // after all systems in the fixed update group have finished updating
                .GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Configure and schedule WispStateMachineJob
            state.Dependency = new WispStateMachineJob {
                // This job may be split across multiple worker threads
                // using a parallel writer
                Commands = commands.AsParallelWriter(),
                PlayerPosition = SystemAPI.GetSingleton<Blackboard>().PlayerPosition,
                Tick = SystemAPI.GetSingleton<Tick>().Value,
                Rng = new Random(_rng.NextUInt()),
            }.ScheduleParallel(state.Dependency);
        }
    }
}
