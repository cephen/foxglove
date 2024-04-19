using System;
using Foxglove.Character;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Foxglove.Agent {
    /// <summary>
    /// This job implements wisp behaviour using a state machine.
    /// </summary>
    [BurstCompile]
    // Some components on WispAspect can be disabled, without this attribute those wisps will be ignored by this job
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    internal partial struct WispStateMachineJob : IJobEntity {
        public uint Tick;
        public Random Rng;
        public float3 PlayerPosition;
        public EntityCommandBuffer.ParallelWriter Commands;

        /// <summary>
        /// Run state machine logic for a single wisp.
        /// </summary>
        /// <param name="entity">The wisp being updated</param>
        /// <param name="aspect">WispAspect of the wisp being updated</param>
        /// <param name="chunkIndex">
        /// The chunk the wisp is in.
        /// Used as a key for <see cref="Commands" /> because this job can run in parallel
        /// </param>
        private void Execute(Entity entity, WispAspect aspect, [ChunkIndexInQuery] int chunkIndex) {
            // regular ToString is disallowed in burst because it allocates on the heap
            FixedString64Bytes wispDebugName = entity.ToFixedString();

            // If the wisp has less than 0 health
            if (aspect.Health.ValueRO.Current <= 0
                // and is not yet marked as dying
                && aspect.State.ValueRO.Current is not WispState.State.Dying)
                // mark it as dying
                aspect.State.ValueRW.TransitionTo(WispState.State.Dying);

            switch (aspect.State.ValueRO.Current) {
                case WispState.State.Inactive:
                    // Freshly spawned wisps have state {current = Spawn, previous = Inactive}
                    // Inactive is not used anywhere else, so this branch should never be entered
                    Log.Error("Wisp {entity} should not be inactive but is", wispDebugName);
                    break;
                case WispState.State.Spawn:
                    Log.Debug("Spawning Wisp {entity}", wispDebugName);
                    aspect.Health.ValueRW.Reset();
                    Commands.SetComponentEnabled<CharacterController>(chunkIndex, aspect.Entity, true);
                    // Transition to Patrol state
                    aspect.State.ValueRW.TransitionTo(WispState.State.Patrol);
                    break;
                case WispState.State.Patrol:
                    // If in range of player transition to attack
                    float distanceToPlayer = math.distance(aspect.LocalToWorld.ValueRO.Position, PlayerPosition);
                    bool isInRange = distanceToPlayer < 10;
                    bool attackCooledDown = aspect.Wisp.ValueRO.CanAttackAt <= Tick;

                    if (isInRange && attackCooledDown) { // TODO: Add line of sight check
                        Log.Debug("Wisp {entity} transitioning to Attack State", wispDebugName);
                        aspect.State.ValueRW.TransitionTo(WispState.State.Attack);
                    }

                    break;
                case WispState.State.Attack:
                    Log.Debug("Wisp {wisp} attacking player", wispDebugName);
                    uint cooldownDuration = Rng.NextUInt(
                        aspect.Wisp.ValueRO.MinAttackCooldown,
                        aspect.Wisp.ValueRO.MaxAttackCooldown
                    );
                    aspect.Wisp.ValueRW.CanAttackAt = Tick + cooldownDuration;

                    // TODO: spawn projectile
                    aspect.State.ValueRW.TransitionTo(WispState.State.Patrol);
                    break;
                case WispState.State.Dying:
                    if (aspect.IsCharacterControllerEnabled.ValueRO) {
                        Log.Debug(
                            "Wisp {entity} died, disabling character controller and adding despawn timer",
                            wispDebugName
                        );
                        // Disable character controller
                        Commands.SetComponentEnabled<CharacterController>(chunkIndex, aspect.Entity, false);
                        // Despawn in one second (50 ticks per second)
                        Commands.AddComponent(chunkIndex, aspect.Entity, new DespawnTimer(Tick + 50));
                    }
                    else if (aspect.DespawnTimer.ValueRO.TickToDestroy <= Tick) {
                        // Transition to Despawn after 1 second
                        Log.Debug("Despawning Wisp {entity}", wispDebugName);
                        aspect.State.ValueRW.TransitionTo(WispState.State.Despawn);
                    }

                    break;
                case WispState.State.Despawn:
                    Commands.DestroyEntity(chunkIndex, aspect.Entity); // Clean up entity
                    break;
                default:
                    Log.Error(
                        "Wisp {wisp} is in invalid state {state}",
                        wispDebugName,
                        nameof(aspect.State.ValueRO.Current)
                    );
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
