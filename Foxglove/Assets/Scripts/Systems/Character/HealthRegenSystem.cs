using Foxglove.Combat;
using Foxglove.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Character {
    /// <summary>
    /// Applies health regen to characters that have avoided damage for at least <see cref="RegenDelayTicks" /> ticks
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    internal partial struct HealthRegenSystem : ISystem {
        // Number of ticks that must pass between taking damage and enabling health regen
        private const uint RegenDelayTicks = 50;
        private EntityQuery _query;

        public void OnCreate(ref SystemState state) {
            _query = SystemAPI.QueryBuilder().WithAllRW<Health, HealthRegen>().Build();
            state.RequireForUpdate(_query);
            state.RequireForUpdate<Tick>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var tick = SystemAPI.GetSingleton<Tick>();

            // Enable/disable health regen based on last damage tick
            foreach ((RefRO<Health> health, Entity entity) in SystemAPI
                    .Query<RefRO<Health>>()
                    .WithPresent<HealthRegen>()
                    .WithEntityAccess()
                    .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
                // Enable health regen if entity took damage more than 1 second ago
            {
                SystemAPI.SetComponentEnabled<HealthRegen>(
                    entity,
                    health.ValueRO.LastDamagedAt + RegenDelayTicks < tick
                );
            }

            // Tick health regen
            foreach ((RefRW<Health> health, RefRO<HealthRegen> regen) in SystemAPI
                .Query<RefRW<Health>, RefRO<HealthRegen>>()) {
                (float current, float max) = (health.ValueRO.Current, health.ValueRO.Max);
                float regenAmount = SystemAPI.Time.fixedDeltaTime * regen.ValueRO.Rate;
                health.ValueRW.Current = math.min(current + regenAmount, max);
            }
        }
    }
}
