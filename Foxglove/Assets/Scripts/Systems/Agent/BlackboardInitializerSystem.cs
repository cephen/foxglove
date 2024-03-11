using Unity.Entities;

namespace Foxglove.Agent {
    /// <summary>
    /// Responsible for creating and destroying the blackboard
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public sealed partial class BlackboardInitializerSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<DefaultSingleton>();
            EntityManager.CreateOrSetSingleton(Blackboard.Default());
        }

        protected override void OnDestroy() {
            EntityManager.RemoveSingletonComponentIfExists<Blackboard>();
        }

        protected override void OnUpdate() { }
    }
}
