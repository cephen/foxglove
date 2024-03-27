﻿using System;
using Foxglove.Character;
using Unity.Burst;
using Unity.Entities;
using Unity.Logging;

namespace Foxglove.Agent {
    [BurstCompile]
    [UpdateInGroup(typeof(AgentSimulationGroup))]
    public partial struct WispSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Blackboard>();
            state.RequireForUpdate<FixedTickSystem.State>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            if (!SystemAPI.TryGetSingleton(out Blackboard blackboard)) {
                Log.Error("[WispStateSystem] Blackboard does not exist");
                return;
            }

            uint tick = SystemAPI.GetSingleton<FixedTickSystem.State>().Tick;

            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (WispAspect aspect in SystemAPI
                .Query<WispAspect>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)) {
                ref WispState wispState = ref aspect.State.ValueRW;

                if (aspect.Health.ValueRO.Current <= 0f)
                    wispState.Current = WispState.State.Die;

                switch (wispState.Current) {
                    case WispState.State.Inactive:
                        // Only used on freshly spawned wisps
                        // No associated behaviour
                        break;
                    case WispState.State.Spawn:
                        // Set default stats
                        aspect.Health.ValueRW.Max = 100;
                        aspect.Health.ValueRW.Current = 100;
                        break;
                    case WispState.State.Patrol:
                        // If in range of player transition to attack
                        break;
                    case WispState.State.Attack:
                        // Spawn projectile
                        // Transition back to Patrol
                        break;
                    case WispState.State.Die:
                        // Disable character controller
                        if (SystemAPI.IsComponentEnabled<CharacterController>(aspect.Entity)) {
                            // Disable character controller
                            ecb.SetComponentEnabled<CharacterController>(aspect.Entity, false);
                            // Despawn in one second (50 ticks per second)
                            ecb.SetComponentEnabled<DespawnTimer>(aspect.Entity, true);
                            ecb.SetComponent(aspect.Entity, new DespawnTimer(tick + 50));
                        }

                        // Transition to Despawn after 1 second
                        break;
                    case WispState.State.Despawn:
                        ecb.DestroyEntity(aspect.Entity); // Clean up entity
                        break;
                    default:
                        Log.Error("Wisp {wisp} is in invalid state {state}", aspect.Entity, wispState.Current);
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}