using System;
using Foxglove.Character;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Foxglove.Agent {
    [BurstCompile]
    [UpdateInGroup(typeof(AgentSimulationGroup))]
    public partial struct WispSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Blackboard>();
            state.RequireForUpdate<FixedTickSystem.State>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<RandomNumberSystem.Singleton>();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (!SystemAPI.TryGetSingleton(out Blackboard blackboard)) {
                Log.Error("[WispStateSystem] Blackboard does not exist");
                return;
            }

            uint tick = SystemAPI.GetSingleton<FixedTickSystem.State>().Tick;

            EntityCommandBuffer commands = SystemAPI
                .GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new WispStateMachineJob {
                Tick = tick,
                Blackboard = blackboard,
                Commands = commands.AsParallelWriter(),
                Rng = SystemAPI.GetSingleton<RandomNumberSystem.Singleton>().Random,
            }.ScheduleParallel( /*wispQuery,*/ state.Dependency);
        }

        [BurstCompile]
        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        private partial struct WispStateMachineJob : IJobEntity {
            public uint Tick;
            public Random Rng;
            public Blackboard Blackboard;
            public EntityCommandBuffer.ParallelWriter Commands;

            public void Execute(Entity entity, WispAspect aspect, [ChunkIndexInQuery] int chunkIndex) {
                // regular ToString is disallowed in burst because it allocates on the heap
                FixedString64Bytes entityDebugName = entity.ToFixedString();

                if (aspect.Health.ValueRO.Current <= 0
                    && aspect.State.ValueRO.Current is not WispState.State.Die)
                    aspect.State.ValueRW.TransitionTo(WispState.State.Die);

                switch (aspect.State.ValueRO.Current) {
                    case WispState.State.Inactive:
                        // Freshly spawned wisps have state {current = Spawn, previous = Inactive}
                        // Inactive is not used anywhere else, so this branch should never be entered
                        Log.Error("Wisp {entity} should not be inactive but is", entityDebugName);
                        break;
                    case WispState.State.Spawn:
                        Log.Debug("Spawning Wisp {entity}", entityDebugName);
                        // Set default stats
                        aspect.Health.ValueRW.Max = 100;
                        aspect.Health.ValueRW.Current = 100;
                        // Enable Character Controller
                        Commands.SetComponentEnabled<CharacterController>(chunkIndex, aspect.Entity, true);
                        // Transition to Patrol state
                        aspect.State.ValueRW.TransitionTo(WispState.State.Patrol);
                        break;
                    case WispState.State.Patrol:
                        // If in range of player transition to attack
                        float distanceToPlayer = math.distance(
                            aspect.LocalToWorld.ValueRO.Position,
                            Blackboard.PlayerPosition
                        );

                        // TODO: Add line of sight check
                        bool attackCooledDown = aspect.Wisp.ValueRO.CanAttackAt <= Tick;
                        if (distanceToPlayer <= 10 && attackCooledDown) {
                            Log.Debug("Wisp {entity} transitioning to Attack State", entityDebugName);
                            aspect.State.ValueRW.TransitionTo(WispState.State.Attack);
                        }

                        break;
                    case WispState.State.Attack:
                        Log.Debug("Wisp {wisp} attacking player", entityDebugName);
                        uint cooldownDuration = Rng.NextUInt(
                            aspect.Wisp.ValueRO.MinAttackCooldown,
                            aspect.Wisp.ValueRO.MaxAttackCooldown
                        );
                        aspect.Wisp.ValueRW.CanAttackAt = Tick + cooldownDuration;

                        // TODO: spawn projectile
                        aspect.State.ValueRW.TransitionTo(WispState.State.Patrol);
                        break;
                    case WispState.State.Die:
                        if (aspect.IsCharacterControllerEnabled.ValueRO) {
                            Log.Debug(
                                "Wisp {entity} died, disabling character controller and adding despawn timer",
                                entityDebugName
                            );
                            // Disable character controller
                            Commands.SetComponentEnabled<CharacterController>(chunkIndex, aspect.Entity, false);
                            // Despawn in one second (50 ticks per second)
                            Commands.SetComponentEnabled<DespawnTimer>(chunkIndex, aspect.Entity, true);
                            Commands.SetComponent(chunkIndex, aspect.Entity, new DespawnTimer(Tick + 50));
                        }
                        else if (aspect.DespawnTimer.ValueRO.TickToDestroy <= Tick) {
                            // Transition to Despawn after 1 second
                            Log.Debug("Despawning Wisp {entity}", entityDebugName);
                            aspect.State.ValueRW.TransitionTo(WispState.State.Despawn);
                        }

                        break;
                    case WispState.State.Despawn:
                        Commands.DestroyEntity(chunkIndex, aspect.Entity); // Clean up entity
                        break;
                    default:
                        Log.Error(
                            "Wisp {wisp} is in invalid state {state}",
                            entityDebugName,
                            nameof(aspect.State.ValueRO.Current)
                        );
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
