using Foxglove.Character;
using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Agent {
    [BurstCompile]
    [UpdateInGroup(typeof(AgentUpdateGroup))]
    public partial struct WispAgentSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate(
                SystemAPI
                    .QueryBuilder()
                    .WithAll<WispTag, CharacterController, Blackboard>()
                    .Build()
            );
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) { }
    }
}
