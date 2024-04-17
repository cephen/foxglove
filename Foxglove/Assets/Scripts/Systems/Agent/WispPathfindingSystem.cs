using Foxglove.Character;
using Foxglove.Core;
using Foxglove.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Agent {
    [BurstCompile]
    [UpdateInGroup(typeof(AgentSimulationGroup))]
    internal partial struct WispPathfindingSystem : ISystem {
        private EntityQuery _wispQuery;
        private EntityQuery _flowFieldQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            _wispQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<CharacterController>()
                .WithAll<Wisp, LocalToWorld>()
                .Build();

            _flowFieldQuery = SystemAPI
                .QueryBuilder()
                .WithAll<WispFlowField, FlowField, FlowFieldSample>()
                .Build();

            state.RequireForUpdate(_wispQuery);
            state.RequireForUpdate(_flowFieldQuery);
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            Entity flowFieldEntity = _flowFieldQuery.GetSingletonEntity();

            state.Dependency = new WispPathfindingJob {
                Config = SystemAPI.GetComponent<FlowField>(flowFieldEntity),
                Samples = SystemAPI.GetBuffer<FlowFieldSample>(flowFieldEntity).AsNativeArray().AsReadOnly(),
            }.ScheduleParallel(_wispQuery, state.Dependency);
        }

        /// <summary>
        /// Job that runs for each wisp
        /// Fetches the flow direction at the wisp's position and assigns it to the wisp's character controller
        /// </summary>
        [BurstCompile]
        private partial struct WispPathfindingJob : IJobEntity {
            internal FlowField Config;
            internal NativeArray<FlowFieldSample>.ReadOnly Samples;


            private readonly void Execute(ref CharacterController controller, in LocalToWorld transform) {
                float2 sampledDirection = Samples[IndexFromPosition(transform.Position)].Direction;
                controller.MoveVector.xz = math.normalizesafe(sampledDirection);
            }

            private readonly int IndexFromPosition(in float3 position) {
                var worldSpaceCoord = (int2)math.floor(position.xz);
                int2 fieldSpaceCoord = worldSpaceCoord - Config.SouthWestCorner;
                return fieldSpaceCoord.x + fieldSpaceCoord.y * Config.FieldSize.x;
            }
        }
    }
}
