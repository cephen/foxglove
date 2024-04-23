using Foxglove.Core.State;
using Foxglove.Player;
using SideFX.Events;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Logging;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Foxglove.Gameplay {
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct TeleporterTriggerSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<State<GameState>>();
            state.RequireForUpdate<SimulationSingleton>(); // the results of the physics simulation from this update tick
            state.RequireForUpdate<PlayerCharacterTag>(); // used to look up the player entity
            state.RequireForUpdate<Teleporter>(); // used to look up the teleporter entity
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            GameState gameState = SystemAPI.GetSingleton<State<GameState>>().Current;
            if (gameState is not GameState.Playing) return;

            var query = new TeleporterTriggerQuery {
                Player = SystemAPI.GetSingletonEntity<PlayerCharacterTag>(),
                Teleporter = SystemAPI.GetSingletonEntity<Teleporter>(),
                Triggered = new NativeReference<bool>(false, Allocator.Persistent),
            };

            JobHandle jobHandle = query.Schedule(
                SystemAPI.GetSingleton<SimulationSingleton>(), // Physics state
                state.Dependency // This system's data dependencies
            );

            jobHandle.Complete();

            if (query.Triggered.Value) {
                Log.Debug("Teleporter triggered!");
                EventBus<TeleporterTriggered>.Raise(new TeleporterTriggered());
            }

            query.Triggered.Dispose();
        }
    }

    /// <summary>
    /// ITriggerEventsJob is the finest example of a shenanigan I encountered in this project.
    /// ---
    /// In order to identify if the teleporter trigger collider has touched the player,
    /// we must filter *all* trigger events from this frame for events involving both entities.
    /// On the plus side, because query is entirely read only, it is highly parallelizable,
    /// and unity can process the events using all CPU cores.
    /// ---
    /// Thanks, Unity
    /// ---
    /// Docs: https://docs.unity3d.com/Packages/com.unity.physics@1.1/manual/simulation-results.html#trigger-events
    /// </summary>
    [BurstCompile]
    internal struct TeleporterTriggerQuery : ITriggerEventsJob {
        internal Entity Player;
        internal Entity Teleporter;
        internal NativeReference<bool> Triggered; // :D

        public void Execute(TriggerEvent triggerEvent) {
            if (Triggered.Value) return; // only need one trigger to win

            bool aIsPlayer = triggerEvent.EntityA == Player;
            bool bIsPlayer = triggerEvent.EntityB == Player;

            bool aIsTeleporter = triggerEvent.EntityA == Teleporter;
            bool bIsTeleporter = triggerEvent.EntityB == Teleporter;

            if (aIsPlayer && bIsTeleporter) Triggered.Value = true;
            else if (bIsPlayer && aIsTeleporter) Triggered.Value = true;
        }
    }
}
