using Unity.Burst;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;

namespace Foxglove.Navigation {
    [BurstCompile]
    public readonly partial struct FlowFieldAspect : IAspect {
        public readonly Entity Entity;
        public readonly RefRW<FlowField> FlowField;
        public readonly DynamicBuffer<FlowFieldSample> Samples;

        [BurstCompile]
        public float2 FlowDirectionAtWorldPosition(in float3 position) {
            int2 coords = WorldToField(position);

            int i = IndexFromFieldCoordinates(coords);
            if (IsInBounds(coords)) return math.normalizesafe(Samples[i].Direction);

            Log.Error(
                "[FlowField] Position {position} is outside field with size {size} and Lower bound {lowerBound}",
                coords,
                FlowField.ValueRO.FieldSize,
                FlowField.ValueRO.SouthWestCorner
            );
            return float2.zero;
        }

        public float3 ToWorldspace(in int2 gridCoordinates) => new(
            gridCoordinates.x + FlowField.ValueRO.LowerBound.x,
            0f,
            gridCoordinates.y + FlowField.ValueRO.LowerBound.y
        );

        private static int2 ToGridCoordinates(in float3 position) =>
            new((int)position.x, (int)position.z);

        /// <summary>
        /// Helper function to check if a position is within the bounds of the flow field
        /// </summary>
        private bool IsInBounds(int2 position) =>
            position is { x: >= 0, y: >= 0 } // funny pattern matching syntax to check if both x and y are >= 0
            && position.x < FlowField.ValueRO.RegionSize.x
            && position.y < FlowField.ValueRO.RegionSize.y;

        /// <summary>
        /// Converts a field space coordinate to an array index
        /// </summary>
        [BurstCompile]
        public int IndexFromFieldCoordinates(in int2 coordinate) =>
            coordinate.x + coordinate.y * FlowField.ValueRO.FieldSize.x;

        /// <summary>
        /// Converts a world space position to an array index
        /// </summary>
        [BurstCompile]
        public int IndexFromWorldPosition(in float3 position) =>
            IndexFromFieldCoordinates(WorldToField(position));
    }
}
