﻿using System;
using Unity.Burst;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace Foxglove {
    public struct RandomNumberGenerators : IComponentData {
        public readonly uint Seed;
        public Random Base;

        public RandomNumberGenerators(uint seed) {
            Seed = seed;
            Base = new Random(Seed);
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct RandomNumberSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            var seed = (uint)DateTimeOffset.UtcNow.GetHashCode();
            state.EntityManager.AddComponent<RandomNumberGenerators>(state.SystemHandle);
            state.EntityManager.SetComponentData(state.SystemHandle, new RandomNumberGenerators(seed));
        }

        public void OnDestroy(ref SystemState state) { }
        public void OnUpdate(ref SystemState state) { }
    }
}
