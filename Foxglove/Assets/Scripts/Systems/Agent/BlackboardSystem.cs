using Foxglove.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Agent {
    /// <summary>
    /// Responsible for creating and updating the blackboard
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(BlackboardUpdateGroup))]
    public partial struct BlackboardSystem : ISystem {
        public void OnCreate(ref SystemState state) =>
            state.EntityManager.AddComponent(state.SystemHandle, ComponentType.ReadWrite<Blackboard>());

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            ref Blackboard blackboard = ref SystemAPI.GetComponentRW<Blackboard>(state.SystemHandle).ValueRW;

            if (!SystemAPI.TryGetSingletonEntity<PlayerCharacterTag>(out Entity player)) return;
            blackboard.PlayerEntity = player;

            if (SystemAPI.HasComponent<LocalToWorld>(player))
                blackboard.PlayerPosition = SystemAPI.GetComponent<LocalToWorld>(player).Position;
        }
    }
}
