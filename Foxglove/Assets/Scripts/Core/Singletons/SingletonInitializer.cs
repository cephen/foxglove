using Unity.Burst;
using Unity.Entities;

namespace Foxglove {
    /// <summary>
    /// This system runs once on game start and initializes an entity that will hold all of the game's singletons
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct SingletonInitializer : ISystem {
        public void OnCreate(ref SystemState state) {
            SingletonUtilities.Setup(state.EntityManager);
            state.EntityManager.SetName(state.EntityManager.GetDefaultSingletonEntity(), "Singletons");
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) { }
    }
}
