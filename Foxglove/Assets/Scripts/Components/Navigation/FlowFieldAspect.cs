using Unity.Burst;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;

namespace Foxglove.Navigation {
    /// <summary>
    /// Aspects are used to query the ECS world for entities with a given set of components.
    /// They can additionally define methods that need to be shared across systems.
    /// ---
    /// An individual entity may have many aspects, and aspects are not mutually exclusive.
    /// Conceptually, each aspect represents a view into a subset any given entities components.
    /// </summary>
    [BurstCompile]
    public readonly partial struct FlowFieldAspect : IAspect {
        // flow field settings
        public readonly RefRW<FlowField> FlowField;
        public readonly EnabledRefRW<RecalculateField> RecalculateField;

        // buffer containing flow direction data
        public readonly DynamicBuffer<FlowFieldSample> Samples;

        /// <summary>
        /// Look up the horizontal flow direction at a given world position
        /// </summary>
        [BurstCompile]
        public float2 FlowDirectionAtWorldPosition(in float3 position) {
            // Convert WorldSpace position to FieldSpace coordinates
            int2 coords = WorldToField(position);

            if (IsInBounds(coords)) {
                int i = IndexFromFieldCoordinates(coords);
                // calculated direction is not normalized if diagonal
                // normalizesafe is used because the flow direction at the destination grid cell
                // will have a magnitude of zero, meaning it cannot be normalized.
                // normalizesafe returns a default of float2.zero in those cases
                return math.normalizesafe(Samples[i].Direction);
            }

            Log.Error(
                "[FlowFieldAspect] Position {position} is outside field with size {size}",
                coords,
                FlowField.ValueRO.FieldSize
            );
            return float2.zero;
        }

        [BurstCompile]
        public void SetDestination(in float3 worldPosition) {
            int2 newDestination = WorldToField(worldPosition);

            if (FlowField.ValueRO.Destination.Equals(newDestination)) return;
            // // If the destination has changed, recalculate the field
            FlowField.ValueRW.Destination = WorldToField(worldPosition);
            RecalculateField.ValueRW = true;
        }

        [BurstCompile]
        public void SetFieldBounds(in int2 southWestCorner, in int2 northEastCorner) {
            int2 fieldSize = northEastCorner - southWestCorner;
            FlowField fieldRO = FlowField.ValueRO;

            // If all properties are unchanged, do nothing
            if (fieldRO.NorthEastCorner.Equals(northEastCorner)
                && fieldRO.SouthWestCorner.Equals(southWestCorner)
                && fieldRO.FieldSize.Equals(fieldSize)) return;

            // Otherwise trigger a recalculation
            FlowField.ValueRW.SouthWestCorner = southWestCorner;
            FlowField.ValueRW.NorthEastCorner = northEastCorner;
            FlowField.ValueRW.FieldSize = fieldSize;
            RecalculateField.ValueRW = true;
        }

        [BurstCompile]
        public int2 WorldToField(in int2 worldCoordinates) => worldCoordinates - FlowField.ValueRO.SouthWestCorner;

        [BurstCompile]
        private int2 WorldToField(in float3 worldPosition) => WorldToField((int2)math.floor(worldPosition.xz));

        /// <summary>
        /// Helper function to check if a position is within the bounds of the flow field
        /// </summary>
        [BurstCompile]
        public bool IsInBounds(in int2 fieldCoordinates) =>
            fieldCoordinates is { x: >= 0, y: >= 0 } // funny pattern matching syntax to check if both x and y are >= 0
            && fieldCoordinates.x < FlowField.ValueRO.FieldSize.x
            && fieldCoordinates.y < FlowField.ValueRO.FieldSize.y;


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
