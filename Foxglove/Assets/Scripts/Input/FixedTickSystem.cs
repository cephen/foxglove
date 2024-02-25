using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Input {
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
    public partial struct FixedTickSystem : ISystem {
        public struct Singleton : IComponentData {
            public uint Tick;
        }

        public void OnCreate(ref SystemState state) {
            if (SystemAPI.HasSingleton<Singleton>()) return;
            Entity singletonEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(singletonEntity, new Singleton());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            ref Singleton singleton = ref SystemAPI.GetSingletonRW<Singleton>().ValueRW;
            singleton.Tick++;
        }

        void ISystem.OnDestroy(ref SystemState state) { }
    }
}
