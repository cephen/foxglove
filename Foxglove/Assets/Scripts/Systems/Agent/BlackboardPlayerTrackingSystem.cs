using Foxglove.Player;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Agent {
    [UpdateInGroup(typeof(BlackboardUpdateGroup))]
    public partial struct BlackboardPlayerTrackingSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Blackboard>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<LocalToWorld, PlayerCharacterTag>().Build());
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            var blackboard = state.EntityManager.GetSingleton<Blackboard>();

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
