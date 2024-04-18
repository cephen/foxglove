using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Navigation {
    /// <summary>
    /// Information about a flow field's dimensions
    /// </summary>
    [BurstCompile]
    public struct FlowField : IComponentData {
        public int2 Destination; // field space coordinates
        public int2 FieldSize;
        public int2 SouthWestCorner;
        public int2 NorthEastCorner;

        [BurstCompile]
        public readonly int2 WorldToField(in int2 worldCoordinates) => worldCoordinates - SouthWestCorner;

        [BurstCompile]
        private readonly int2 WorldToField(in float3 worldPosition) => WorldToField((int2)math.floor(worldPosition.xz));

        /// <summary>
        /// Converts a field space coordinate to an array index
        /// </summary>
        [BurstCompile]
        public readonly int IndexFromFieldCoordinates(in int2 coordinate) =>
            coordinate.x + coordinate.y * FieldSize.x;

        /// <summary>
        /// Converts a world space position to an array index
        /// </summary>
        [BurstCompile]
        public readonly int IndexFromWorldPosition(in float3 position) =>
            IndexFromFieldCoordinates(WorldToField(position));
    }

    public struct RecalculateField : IComponentData, IEnableableComponent { }

    public struct FlowFieldSample : IBufferElementData {
        public int2 Direction;

        /// <summary>
        /// Allows implicit conversion from int2 to FlowFieldSample.
        /// For example, allowing code like FlowFieldSample x = new int2(1,2);
        /// </summary>
        public static implicit operator FlowFieldSample(int2 value) => new() { Direction = value };

        /// <summary>
        /// Allows implicit conversion from FlowFieldSample to int2.
        /// For example, allowing code like int2 x = new FlowFieldSample(1,2);
        /// </summary>
        public static implicit operator int2(FlowFieldSample value) => value.Direction;
    }
}
