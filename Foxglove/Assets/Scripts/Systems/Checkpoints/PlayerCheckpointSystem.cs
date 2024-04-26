using Foxglove.Core;
using Foxglove.Core.State;
using Foxglove.Gameplay;
using Foxglove.Player;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Checkpoints {
    /// <summary>
    /// Tracks the player and returns them to a safe place if they fall out of the map
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CheckpointUpdateGroup))]
    internal partial struct PlayerCheckpointSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<State<GameState>>();
            state.RequireForUpdate<PlayerController>();
            state.RequireForUpdate<KinematicCharacterBody>();

            state.EntityManager.AddComponent<PlayerCheckpoints>(state.SystemHandle);
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            // Only run in playing state
            if (SystemAPI.GetSingleton<State<GameState>>().Current is not GameState.Playing) return;

            RefRW<PlayerCheckpoints> checkpoint = SystemAPI.GetComponentRW<PlayerCheckpoints>(state.SystemHandle);

            var controller = SystemAPI.GetSingleton<PlayerController>();

            // Early exit if player character doesn't exist
            if (!SystemAPI.Exists(controller.ControlledCharacter)) return;

            Entity characterEntity = controller.ControlledCharacter;
            RefRW<LocalTransform> transform = SystemAPI.GetComponentRW<LocalTransform>(characterEntity);
            var character = SystemAPI.GetComponent<KinematicCharacterBody>(characterEntity);

            if (character.IsGrounded) // Track the players position when grounded
                checkpoint.ValueRW.LastGroundPosition = transform.ValueRO.Position;
            else if (transform.ValueRO.Position.y < -3f) // Or teleport them if they're ungrounded and out of bounds
                transform.ValueRW.Position = checkpoint.ValueRO.LastGroundPosition;
        }
    }
}
