using System;
using Unity.Burst;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace Foxglove {
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct RandomNumberSystem : ISystem {
        public struct Singleton : IComponentData {
            public Random Random;
        }

        public void OnCreate(ref SystemState state) {
            state.EntityManager.CreateOrSetSingleton(
                new Singleton {
                    Random = new Random((uint)DateTimeOffset.UtcNow.GetHashCode()),
                }
            );
        }

        public void OnDestroy(ref SystemState state) { }
        public void OnUpdate(ref SystemState state) { }
    }
}
