using Foxglove.Character;
using Foxglove.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Agent {
    [BurstCompile]
    [UpdateInGroup(typeof(AgentUpdateGroup))]
    public partial struct WispPathfindingSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            // Agent query
            state.RequireForUpdate(
                SystemAPI
                    .QueryBuilder()
                    .WithAll<WispTag, CharacterController, LocalToWorld>()
                    .Build()
            );

            // Flow Field query
            state.RequireForUpdate(
                SystemAPI.QueryBuilder().WithAll<WispFlowField, FlowField, FlowFieldSample>().Build()
            );

            state.RequireForUpdate<Blackboard>();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var blackboard = SystemAPI.GetSingleton<Blackboard>();

            foreach ((RefRW<CharacterController> controller, RefRO<LocalToWorld> transform) in SystemAPI
                .Query<RefRW<CharacterController>, RefRO<LocalToWorld>>()
                .WithAll<WispTag>()) {
                float3 move = math.normalizesafe(blackboard.PlayerPosition - transform.ValueRO.Position);

                move.y = 0;

                controller.ValueRW.MoveVector = move;
            }
        }
    }
}
