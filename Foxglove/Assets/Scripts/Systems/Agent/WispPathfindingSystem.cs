using Foxglove.Character;
using Foxglove.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Agent {
    [BurstCompile]
    [UpdateInGroup(typeof(AgentSimulationGroup))]
    internal partial struct WispPathfindingSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            // Agent query
            state.RequireForUpdate(
                SystemAPI
                    .QueryBuilder()
                    .WithAll<Wisp, CharacterController, LocalToWorld>()
                    .Build()
            );

            // Flow Field query
            state.RequireForUpdate(
                SystemAPI.QueryBuilder().WithAll<WispFlowField, FlowField, FlowFieldSample>().Build()
            );
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            Entity flowFieldEntity = SystemAPI.GetSingletonEntity<WispFlowField>();
            var field = SystemAPI.GetAspect<FlowFieldAspect>(flowFieldEntity);

            foreach ((RefRW<CharacterController> controller, RefRO<LocalToWorld> transform) in SystemAPI
                .Query<RefRW<CharacterController>, RefRO<LocalToWorld>>()
                .WithAll<Wisp>()) {
                float2 sampledDirection = field.FlowDirectionAtWorldPosition(transform.ValueRO.Position);
                controller.ValueRW.MoveVector.xz = math.normalizesafe(sampledDirection);
            }
        }
    }
}
