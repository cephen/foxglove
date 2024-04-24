using Foxglove.Agent;
using Foxglove.Character;
using Foxglove.Core.State;
using Foxglove.Gameplay;
using SideFX.Events;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Foxglove.Player {
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct PlayerDamageSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<State<GameState>>();
            state.RequireForUpdate<SimulationSingleton>(); // collision query data source
            state.RequireForUpdate<PlayerCharacterTag>(); // used to look up the player entity
            state.RequireForUpdate<Wisp>(); // used to look up wisps
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            // Only run in playing state
            if (SystemAPI.GetSingleton<State<GameState>>().Current is not GameState.Playing) return;

            var collisions = new NativeReference<int>(0, Allocator.TempJob);

            // Configure and run the query immediately
            JobHandle query = new PlayerWispCollisionQuery {
                    Player = SystemAPI.GetSingletonEntity<PlayerCharacterTag>(),
                    WispLookup = SystemAPI.GetComponentLookup<Wisp>(true),
                    Collisions = collisions,
                }
                .Schedule(
                    SystemAPI.GetSingleton<SimulationSingleton>(), // Physics state
                    state.Dependency // This system's data dependencies
                );

            query.Complete();

            for (int i = 0; i < collisions.Value; i++) EventBus<PlayerDamaged>.Raise(new PlayerDamaged(10.0f));

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
