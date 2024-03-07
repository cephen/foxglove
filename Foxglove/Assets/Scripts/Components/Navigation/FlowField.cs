using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Navigation {
    public struct FlowFieldTarget : IComponentData {
        public Entity Value;
    }

    public struct FlowFieldResolution : IComponentData {
        public int3 Value;
    }

    public struct FlowFieldChunk : IComponentData {
        public Entity Parent;

        /// <summary>
        /// Position of the chunk in grid coordinates relative to the chunk origin.
        /// Each component is in the range [-1, 1]
        /// </summary>
        public int3 Position;

        public float3 Size;
    }

    public readonly partial struct FlowFieldAspect : IAspect {
        public readonly Entity Entity;
        public readonly RefRW<FlowFieldTarget> Target;
    }

    public readonly partial struct FlowFieldChunkAspect : IAspect {
        public readonly Entity Entity;
        public readonly RefRW<FlowFieldChunk> Chunk;
        public readonly RefRW<FlowFieldResolution> Resolution;
        public readonly RefRW<LocalTransform> Transform;
        public readonly DynamicBuffer<FlowFieldSample> Samples;

        public bool Contains(float3 position) => true;
    }

    public struct FlowFieldSample : IBufferElementData {
        public Entity Chunk;
        public int3 Position;
        public float Cost;
        public float3 Direction;
    }
}
