using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Agent {
    public struct Blackboard : ISharedComponentData {
        public Entity PlayerEntity;
        public float3 PlayerPosition;

        public static Blackboard Default() => new() {
            PlayerEntity = Entity.Null,
            PlayerPosition = float3.zero,
        };
    }

    [UpdateInGroup(typeof(BlackboardUpdateGroup))]
    public partial struct BlackboardUpdateSystem : ISystem {
        private ComponentLookup<LocalToWorld> _localToWorldLookup;


        public void OnCreate(ref SystemState state) {
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>();
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            UpdateComponentLookups(ref state);
        }

        private void UpdateComponentLookups(ref SystemState state) {
            _localToWorldLookup.Update(ref state);
        }
    }
}
