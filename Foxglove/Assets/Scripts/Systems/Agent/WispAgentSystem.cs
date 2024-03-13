using Foxglove.Character;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Agent {
    [BurstCompile]
    [UpdateInGroup(typeof(AgentUpdateGroup))]
    public partial struct WispAgentSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            // Agent components
            state.RequireForUpdate(
                SystemAPI
                    .QueryBuilder()
                    .WithAll<WispTag, CharacterController, LocalToWorld>()
                    .Build()
            );

            state.RequireForUpdate<Blackboard>();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) { }
    }
}
