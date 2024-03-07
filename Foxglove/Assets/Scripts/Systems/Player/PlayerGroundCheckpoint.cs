using Foxglove.Groups;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Player {
    [BurstCompile]
    [UpdateInGroup(typeof(CheckpointUpdateGroup))]
    public partial struct PlayerGroundCheckpoint : ISystem {
        public struct Singleton : IComponentData {
            public float3 Position;
        }

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerController>();
            state.RequireForUpdate<KinematicCharacterBody>();
            state.EntityManager.CreateOrAddSingleton<Singleton>();
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            foreach (RefRO<PlayerController> player in
                SystemAPI.Query<RefRO<PlayerController>>().WithAll<Simulate>()) {
                var checkpoint = state.EntityManager.GetSingleton<Singleton>();

                Entity characterEntity = player.ValueRO.ControlledCharacter;
                var character = SystemAPI.GetComponent<KinematicCharacterBody>(characterEntity);
                ref LocalTransform transform = ref SystemAPI.GetComponentRW<LocalTransform>(characterEntity).ValueRW;

                if (character.IsGrounded)
                    checkpoint.Position = transform.Position;
                else if (transform.Position.y < -3f)
                    transform.Position = checkpoint.Position;

                state.EntityManager.CreateOrSetSingleton(checkpoint);
            }
        }
    }
}
