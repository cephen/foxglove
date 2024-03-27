using Foxglove.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Agent {
    [BurstCompile]
    [UpdateInGroup(typeof(BlackboardUpdateGroup))]
    public partial struct BlackboardPlayerTrackingSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Blackboard>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<LocalToWorld, PlayerCharacterTag>().Build());
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            ref Blackboard blackboard = ref SystemAPI.GetSingletonRW<Blackboard>().ValueRW;

            foreach ((RefRO<LocalToWorld> ltw, Entity entity) in SystemAPI
                .Query<RefRO<LocalToWorld>>()
                .WithAll<PlayerCharacterTag>()
                .WithEntityAccess()) {
                blackboard.PlayerEntity = entity;
                blackboard.PlayerPosition = ltw.ValueRO.Position;
            }

            state.EntityManager.CreateOrSetSingleton(blackboard);
        }
    }
}
