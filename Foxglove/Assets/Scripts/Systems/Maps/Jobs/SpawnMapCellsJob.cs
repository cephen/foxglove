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
            LocalTransform transform = LocalTransform
                .FromPosition(PositionFromIndex(index))
                .WithRotation(quaternion.AxisAngle(math.right(), 90));
            Commands.AddComponent(_threadIndex, e, new Parent { Value = MapRoot });
            Commands.AddComponent(_threadIndex, e, transform);
        }

        private readonly float3 PositionFromIndex(int index) {
            int2 coords = new int2(index % Config.Diameter, index / Config.Diameter) - Config.Radius;
            var to3d = new float3(coords.x, 0f, coords.y);
            return to3d;
        }
    }
}
