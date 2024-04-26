using Foxglove.Combat;
using Foxglove.Player;
using SideFX.Events;
using Unity.Entities;
using Unity.Logging;

namespace Foxglove.Character {
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(HealthRegenSystem))]
    public partial struct PlayerDeathNotifierSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            foreach ((RefRO<Health> health, Entity e) in SystemAPI
                .Query<RefRO<Health>>()
                .WithAll<PlayerCharacterTag>()
                .WithEntityAccess()) {
                if (health.ValueRO.Current > 0) continue;

                Log.Debug("Player died: {entity}", e.ToFixedString());
                EventBus<PlayerDied>.Raise(new PlayerDied(e));
            }
        }
    }
}
