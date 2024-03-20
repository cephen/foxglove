using Foxglove.Player;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Checkpoints {
    [BurstCompile]
    [UpdateInGroup(typeof(CheckpointUpdateGroup))]
    public partial struct PlayerGroundCheckpoint : ISystem, ISystemStartStop {
        public struct State : IComponentData {
            public float3 Position;
        }

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerController>();
            state.RequireForUpdate<KinematicCharacterBody>();
        }

        public void OnStartRunning(ref SystemState state) {
            state.EntityManager.CreateOrAddSingleton<State>();
        }

        public void OnStopRunning(ref SystemState state) {
            state.EntityManager.RemoveSingletonComponentIfExists<State>();
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            ref State checkpoint = ref SystemAPI.GetSingletonRW<State>().ValueRW;

            foreach (RefRO<PlayerController> player in SystemAPI.Query<RefRO<PlayerController>>().WithAll<Simulate>()) {
                Entity characterEntity = player.ValueRO.ControlledCharacter;
                ref LocalTransform transform = ref SystemAPI.GetComponentRW<LocalTransform>(characterEntity).ValueRW;
                var character = SystemAPI.GetComponent<KinematicCharacterBody>(characterEntity);

                if (character.IsGrounded) checkpoint.Position = transform.Position;
                else if (transform.Position.y < -3f) transform.Position = checkpoint.Position;
            }
        }
    }
}
