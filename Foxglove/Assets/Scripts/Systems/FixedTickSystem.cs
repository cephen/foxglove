using Unity.Burst;
using Unity.Entities;

namespace Foxglove {
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
    public sealed partial class FixedTickSystem : SystemBase {
        protected override void OnCreate() {
            EntityManager.CreateOrAddSingleton<Singleton>();
        }

        [BurstCompile]
        protected override void OnUpdate() {
            var tickData = EntityManager.GetSingleton<Singleton>();
            tickData.Tick++;
            EntityManager.CreateOrSetSingleton(tickData);
        }

        protected override void OnDestroy() { }

        public struct Singleton : IComponentData {
            public uint Tick;
        }
    }
}
