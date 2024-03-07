using Unity.Entities;

namespace Foxglove {
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
