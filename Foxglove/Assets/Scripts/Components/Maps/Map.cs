using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Maps {
    public struct Map : IComponentData { }

    [BurstCompile]
    public readonly partial struct MapAspect : IAspect {
        public readonly Entity Entity;
        public readonly RefRW<Map> Map;
        public readonly DynamicBuffer<MapCell> Cells;
    }
}
