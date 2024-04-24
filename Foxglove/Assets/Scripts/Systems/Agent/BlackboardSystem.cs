using Foxglove.Core;
using Foxglove.Core.State;
using Foxglove.Gameplay;
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
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<State<GameState>>();
            state.EntityManager.AddComponent(state.SystemHandle, ComponentType.ReadWrite<Blackboard>());
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            // Only run in playing state
            if (SystemAPI.GetSingleton<State<GameState>>().Current is not GameState.Playing) return;

            ref Blackboard blackboard = ref SystemAPI.GetComponentRW<Blackboard>(state.SystemHandle).ValueRW;

            // If there is exactly one player in the world
            if (SystemAPI.TryGetSingletonEntity<PlayerCharacterTag>(out Entity player)
                // And the currently tracked player does not exist, or is not that player
                && (!SystemAPI.Exists(blackboard.PlayerEntity) || blackboard.PlayerEntity != player))
                // Track that player
                blackboard.PlayerEntity = player;

            // If the player has a LocalToWorld (transform) component
            if (SystemAPI.HasComponent<LocalToWorld>(blackboard.PlayerEntity))
                // Cache the player's position in the blackboard
                blackboard.PlayerPosition = SystemAPI.GetComponent<LocalToWorld>(blackboard.PlayerEntity).Position;
        }
    }
}
