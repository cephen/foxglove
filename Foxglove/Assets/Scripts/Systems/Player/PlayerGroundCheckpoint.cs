using Foxglove.Groups;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Player {
    [BurstCompile]
    [UpdateInGroup(typeof(CheckpointUpdateGroup))]
    public partial struct PlayerGroundCheckpoint : ISystem, ISystemStartStop {
        public struct Singleton : IComponentData {
            public float3 Position;
        }

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerController>();
            state.RequireForUpdate<KinematicCharacterBody>();
        }

        public void OnStartRunning(ref SystemState state) {
            state.EntityManager.CreateOrAddSingleton<Singleton>();
        }

        public void OnStopRunning(ref SystemState state) {
            state.EntityManager.RemoveSingletonComponentIfExists<Singleton>();
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            foreach (RefRO<PlayerController> player in SystemAPI.Query<RefRO<PlayerController>>().WithAll<Simulate>()) {
                Entity characterEntity = player.ValueRO.ControlledCharacter;
                var character = SystemAPI.GetComponent<KinematicCharacterBody>(characterEntity);
                ref LocalTransform transform = ref SystemAPI.GetComponentRW<LocalTransform>(characterEntity).ValueRW;

                Entity singletonEntity = state.EntityManager.GetDefaultSingletonEntity();
                ref Singleton checkpoint = ref SystemAPI.GetComponentRW<Singleton>(singletonEntity).ValueRW;

                if (character.IsGrounded) checkpoint.Position = transform.Position;
                else if (transform.Position.y < -3f) transform.Position = checkpoint.Position;
            }
        }
    }
}
