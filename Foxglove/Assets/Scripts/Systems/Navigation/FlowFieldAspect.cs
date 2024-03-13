using Unity.Burst;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;

namespace Foxglove.Navigation {
    [BurstCompile]
    public readonly partial struct FlowFieldAspect : IAspect {
        public readonly Entity Entity;
        public readonly RefRO<FlowField> FlowField;
        public readonly DynamicBuffer<FlowFieldSample> Samples;

        public float2 DirectionAt(float3 position) {
            int2 gridCoordinates = ToGridCoordinates(position);

            if (!IsInBounds(gridCoordinates)) {
                Log.Error(
                    "[FlowField] Position {position} is outside field with size {size} and Lower bound {lowerBound}",
                    gridCoordinates,
                    FlowField.ValueRO.RegionSize,
                    FlowField.ValueRO.LowerBound
                );
                return float2.zero;
            }

            int index = IndexFromPosition(gridCoordinates);

            return math.normalizesafe(Samples[index].Direction);
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
        /// Converts a position to an array index
        /// </summary>
        [BurstCompile]
        private int IndexFromPosition(in int2 position) {
            int2 offsetPosition = position - FlowField.ValueRO.LowerBound;
            return offsetPosition.y + offsetPosition.x * FlowField.ValueRO.RegionSize.x;
        }
    }
}
