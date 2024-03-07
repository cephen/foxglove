using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Navigation {
    public struct FlowFieldTarget : IComponentData {
        public Entity TargetEntity;
        public uint3 TargetCoordinate;
    }

    public struct FlowFieldSample : IBufferElementData {
        public float Cost;
        public float3 Direction;
    }

    public readonly partial struct FlowFieldAspect : IAspect {
        public readonly Entity Entity;
        public readonly RefRW<FlowFieldTarget> Target;
        public readonly RefRW<LocalToWorld> Origin;
        public readonly DynamicBuffer<FlowFieldSample> Samples;
    }
}
