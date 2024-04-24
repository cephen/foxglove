using Foxglove.Combat;
using Foxglove.Core;
using Foxglove.Core.State;
using Foxglove.Gameplay;
using Unity.Burst;
using Unity.Entities;

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
            state.RequireForUpdate<State<GameState>>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            // Only run in playing state
            if (SystemAPI.GetSingleton<State<GameState>>().Current is not GameState.Playing) return;

            var tick = SystemAPI.GetSingleton<Tick>();

            foreach ( // Query for all health components, and the entities that own them
                (RefRO<Health> health, Entity entity) in SystemAPI
                    // Only need read permissions for Health component
                    .Query<RefRO<Health>>()
                    // Entity must have a HealthRegen component
                    .WithPresent<HealthRegen>()
                    // Ignore whether HealthRegen is currently enabled
                    .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                    // Adds the matching entity to the query output
                    .WithEntityAccess()
            ) {
                // Enable health regen if at least <see cref="RegenDelayTicks" /> ticks have passed since taking damage
                SystemAPI.SetComponentEnabled<HealthRegen>(
                    entity,
                    health.ValueRO.LastDamagedAt + RegenDelayTicks < tick
                );
            }

            // Tick health regen
            foreach (
                (RefRW<Health> health, RefRO<HealthRegen> regen)
                in SystemAPI.Query<RefRW<Health>, RefRO<HealthRegen>>()
            ) {
                float regenAmount = SystemAPI.Time.fixedDeltaTime * regen.ValueRO.Rate;
                health.ValueRW.ApplyRegen(regenAmount);
            }
        }
    }
}
