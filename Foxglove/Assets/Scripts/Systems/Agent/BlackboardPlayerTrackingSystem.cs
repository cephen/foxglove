using Foxglove.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Agent {
    [BurstCompile]
    [UpdateInGroup(typeof(BlackboardUpdateGroup))]
    public partial struct BlackboardPlayerTrackingSystem : ISystem {
        private EntityQuery _playerQuery;

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Blackboard>();
            _playerQuery = new EntityQueryBuilder(Allocator.Persistent)
                .WithAll<LocalToWorld, PlayerCharacterTag>()
                .Build(ref state);
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            ref Blackboard blackboard = ref SystemAPI.GetSingletonRW<Blackboard>().ValueRW;

            if (!_playerQuery.TryGetSingletonEntity<LocalToWorld>(out Entity playerEntity)) return;
            blackboard.PlayerEntity = playerEntity;

            if (!_playerQuery.TryGetSingleton(out LocalToWorld playerTransform)) return;
            blackboard.PlayerPosition = playerTransform.Position;
        }
    }
}
