using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Maps.Jobs {
    [BurstCompile]
    public struct SpawnMapCellsJob : IJobParallelFor {
        internal NativeArray<MapCell>.ReadOnly Cells;
        internal EntityCommandBuffer.ParallelWriter Commands;
        internal Entity MapRoot;
        internal MapConfig Config;

        [NativeSetThreadIndex] private int _threadIndex;

        public void Execute(int index) {
            Entity e = Commands.CreateEntity(_threadIndex);
            float3 position = CoordsFromIndex(index);
            Commands.AddComponent(_threadIndex, e, new Parent { Value = MapRoot });
            Commands.AddComponent(_threadIndex, e, LocalTransform.FromPosition(position));
        }

        private readonly float3 CoordsFromIndex(int index) =>
            new int3(index % Config.Diameter, 0, index / Config.Diameter) - Config.Radius;
    }
}
