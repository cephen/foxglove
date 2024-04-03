using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Maps {
    internal struct MapArchetypes : IComponentData {
        public EntityArchetype Room;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    internal partial struct MapArchetypeInitializer : ISystem {
        public void OnCreate(ref SystemState state) {
            MapArchetypes archetypes = new() {
                Room = state.EntityManager.CreateArchetype(
                    ComponentType.ReadOnly<Room>(),
                    ComponentType.ReadWrite<LocalTransform>(),
                    ComponentType.ReadOnly<Parent>()
                ),
            };
            state.EntityManager.AddComponentData(state.SystemHandle, archetypes);
        }
    }
}
