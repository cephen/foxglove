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
    public partial struct TeleporterTriggerSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<State<GameState>>();
            state.RequireForUpdate<SimulationSingleton>(); // the results of the physics simulation from this update tick
            state.RequireForUpdate<PlayerCharacterTag>(); // used to look up the player entity
            state.RequireForUpdate<Teleporter>(); // used to look up the teleporter entity
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            // Only run in playing state
            if (SystemAPI.GetSingleton<State<GameState>>().Current is not GameState.Playing) return;

            Entity teleporter = SystemAPI.GetSingletonEntity<Teleporter>();
            Entity player = SystemAPI.GetSingletonEntity<PlayerCharacterTag>();

            DynamicBuffer<KinematicCharacterHit> hits = SystemAPI.GetBuffer<KinematicCharacterHit>(player);

            foreach (KinematicCharacterHit hit in hits) {
                if (hit.Entity == teleporter) {
                    Log.Debug("Teleporter triggered!");
                    EventBus<TeleporterTriggered>.Raise(new TeleporterTriggered());
                    return;
                }
            }
        }
    }
}
