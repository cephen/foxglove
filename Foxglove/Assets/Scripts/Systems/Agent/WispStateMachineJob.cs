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
    // Some components on WispAspect can be disabled.
    // This attribute allows queries to to capture entities with disabled components
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
                // but hasn't been sentenced to death yet
                && aspect.State.ValueRO.Current is not WispState.State.Dying)
                // drop the hammer and issue a death certificate
                aspect.State.ValueRW.TransitionTo(WispState.State.Dying);

            switch (aspect.State.ValueRO.Current) {
                case WispState.State.Spawn:
                    Log.Debug("Spawning Wisp {entity}", wispDebugName);

                    aspect.Health.ValueRW.Reset();

                    Commands.SetComponentEnabled<CharacterController>(chunkIndex, aspect.Entity, true);

                    aspect.State.ValueRW.TransitionTo(WispState.State.Patrol);

                    return;
                case WispState.State.Patrol:
                    // If in range of player transition to attack
                    float distanceToPlayer = math.distance(aspect.LocalToWorld.ValueRO.Position, PlayerPosition);
                    bool isInRange = distanceToPlayer < 10;

                    // and attack is cooled down
                    bool attackCooledDown = aspect.Wisp.ValueRO.CanAttackAt <= Tick;

                    if (isInRange && attackCooledDown) { // TODO: Add line of sight check
                        Log.Debug("Wisp {entity} transitioning to Attack State", wispDebugName);
                        aspect.State.ValueRW.TransitionTo(WispState.State.Attack);
                    }

                    return;
                case WispState.State.Attack:
                    Log.Debug("Wisp {wisp} attacking player", wispDebugName);
                    // TODO: spawn projectile

                    uint cooldownDuration = Rng.NextUInt(
                        aspect.Wisp.ValueRO.MinAttackCooldown,
                        aspect.Wisp.ValueRO.MaxAttackCooldown
                    );

                    aspect.Wisp.ValueRW.CanAttackAt = Tick + cooldownDuration;

                    aspect.State.ValueRW.TransitionTo(WispState.State.Patrol);

                    return;
                case WispState.State.Dying:
                    if (aspect.IsCharacterControllerEnabled.ValueRO) {
                        Log.Debug(
                            "Wisp {entity} died, disabling character controller and adding despawn timer",
                            wispDebugName
                        );

                        Commands.SetComponentEnabled<CharacterController>(chunkIndex, aspect.Entity, false);

                        // Schedule despawn for 50 ticks / 1 second from now
                        Commands.AddComponent(chunkIndex, aspect.Entity, new DespawnTimer(Tick + 50));
                    }
                    else if (aspect.DespawnTimer.ValueRO.TickToDestroy <= Tick) {
                        Log.Debug("Despawning Wisp {entity}", wispDebugName);

                        Commands.DestroyEntity(chunkIndex, aspect.Entity);
                    }

                    return;
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
