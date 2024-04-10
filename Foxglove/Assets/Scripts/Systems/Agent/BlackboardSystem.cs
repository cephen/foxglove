using Foxglove.Core;
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
    internal partial struct BlackboardSystem : ISystem {
        public void OnCreate(ref SystemState state) =>
            state.EntityManager.AddComponent(state.SystemHandle, ComponentType.ReadWrite<Blackboard>());

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            ref Blackboard blackboard = ref SystemAPI.GetComponentRW<Blackboard>(state.SystemHandle).ValueRW;

            // If there is exactly one player in the world
            if (SystemAPI.TryGetSingletonEntity<PlayerCharacterTag>(out Entity player)
                // And the currently tracked player is not that player
                && !SystemAPI.Exists(blackboard.PlayerEntity))
                // Track that player
                blackboard.PlayerEntity = player;

            if (SystemAPI.HasComponent<LocalToWorld>(blackboard.PlayerEntity))
                blackboard.PlayerPosition = SystemAPI.GetComponent<LocalToWorld>(blackboard.PlayerEntity).Position;
        }
    }
}
