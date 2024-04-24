using Foxglove.Agent;
using Foxglove.Combat;
using Foxglove.Core;
using Foxglove.Core.State;
using Foxglove.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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

        public void OnCreate(ref SystemState state) {
            _playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerCharacterTag>().WithAllRW<Health>().Build();

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

            var tick = SystemAPI.GetSingleton<Tick>();

            Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerCharacterTag>();
            RefRW<Health> health = SystemAPI.GetComponentRW<Health>(playerEntity);

            // Don't query for collisions if the player is immune to damage
            if (health.ValueRO.LastDamagedAt + ImmunityAfterDamageTicks > tick) return;

            var collisions = new NativeReference<int>(0, Allocator.TempJob);

            // Configure and run the query immediately
            JobHandle query = new PlayerWispCollisionQuery {
                    Player = playerEntity,
                    WispLookup = SystemAPI.GetComponentLookup<Wisp>(true),
                    Collisions = collisions,
                }
                .Schedule(
                    SystemAPI.GetSingleton<SimulationSingleton>(), // Physics state
                    state.Dependency // This system's data dependencies
                );

            query.Complete();

            if (collisions.Value > 0) {
                float totalDamage = collisions.Value * WispContactDamage;
                health.ValueRW.ApplyDamage(tick, totalDamage);
            }

            collisions.Dispose();
        }

        [BurstCompile]
        private struct PlayerWispCollisionQuery : ICollisionEventsJob {
            internal Entity Player;
            internal ComponentLookup<Wisp> WispLookup;
            internal NativeReference<int> Collisions;

            public void Execute(CollisionEvent collisionEvent) {
                bool aIsPlayer = collisionEvent.EntityA == Player;
                bool bIsPlayer = collisionEvent.EntityB == Player;

                bool aIsWisp = WispLookup.HasComponent(collisionEvent.EntityA);
                bool bIsWisp = WispLookup.HasComponent(collisionEvent.EntityB);

                if (aIsPlayer && bIsWisp) Collisions.Value++;
                if (bIsPlayer && aIsWisp) Collisions.Value++;
            }
        }
    }
}
