using Foxglove.Core;
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
            state.RequireForUpdate<PlayerController>();
            state.RequireForUpdate<KinematicCharacterBody>();
            state.EntityManager.AddComponent<PlayerCheckpoints>(state.SystemHandle);
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            RefRW<PlayerCheckpoints> checkpoint = SystemAPI.GetComponentRW<PlayerCheckpoints>(state.SystemHandle);

            RefRW<PlayerController> controller = SystemAPI.GetSingletonRW<PlayerController>();
            if (!SystemAPI.Exists(controller.ValueRO.ControlledCharacter)) return;
            Entity characterEntity = controller.ValueRO.ControlledCharacter;

            RefRW<LocalTransform> transform = SystemAPI.GetComponentRW<LocalTransform>(characterEntity);
            var character = SystemAPI.GetComponent<KinematicCharacterBody>(characterEntity);

            if (character.IsGrounded)
                checkpoint.ValueRW.LastGroundPosition = transform.ValueRO.Position;
            else if (transform.ValueRO.Position.y < -3f)
                transform.ValueRW.Position = checkpoint.ValueRO.LastGroundPosition;
        }
    }
}
