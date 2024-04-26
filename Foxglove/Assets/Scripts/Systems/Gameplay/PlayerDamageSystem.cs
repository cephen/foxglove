using Foxglove.Agent;
using Foxglove.Character;
using Foxglove.Combat;
using Foxglove.Core;
using Foxglove.Core.State;
using Foxglove.Player;
using SideFX.Events;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Logging;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Foxglove.Gameplay {
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct PlayerDamageSystem : ISystem {
        private const float WispContactDamage = 10f;
        private const uint ImmunityAfterDamageTicks = 25; // 1/2 of a second

        private EntityQuery _playerQuery;
        private ComponentLookup<Wisp> _wispLookup;

        public void OnCreate(ref SystemState state) {
            _playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerCharacterTag>().WithAllRW<Health>().Build();
            _wispLookup = state.GetComponentLookup<Wisp>();

            state.RequireForUpdate<Tick>();
            state.RequireForUpdate<State<GameState>>();
            state.RequireForUpdate<SimulationSingleton>(); // collision query data source
            state.RequireForUpdate<Wisp>(); // used to look up wisps
            state.RequireForUpdate(_playerQuery); // used to look up the player entity
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            // Only run in playing state
            if (SystemAPI.GetSingleton<State<GameState>>().Current is not GameState.Playing) return;

            _wispLookup.Update(ref state);

            Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerCharacterTag>();
            RefRW<Health> health = SystemAPI.GetComponentRW<Health>(playerEntity);

            // Don't query for collisions if the player is immune to damage
            var tick = SystemAPI.GetSingleton<Tick>();
            if (tick - health.ValueRO.LastDamagedAt < ImmunityAfterDamageTicks) return;

            DynamicBuffer<KinematicCharacterHit> hits = SystemAPI.GetBuffer<KinematicCharacterHit>(playerEntity);
            int wispHits = 0;

            foreach (KinematicCharacterHit hit in hits) {
                if (_wispLookup.HasComponent(hit.Entity))
                    wispHits++;
            }

            if (wispHits is 0) return;

            float totalDamage = wispHits * WispContactDamage;

            Log.Debug(
                "[PlayerDamageSystem] Player collided with {collisions} wisps - applying {totalDamage} damage",
                wispHits,
                totalDamage
            );
            health.ValueRW.ApplyDamage(tick, totalDamage);

            EventBus<PlayerHealthChanged>.Raise(new PlayerHealthChanged(health.ValueRO));
        }
    }
}
