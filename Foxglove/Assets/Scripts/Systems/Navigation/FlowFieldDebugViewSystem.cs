#if UNITY_EDITOR // Debug view is only needed in the editor, don't compile this system for builds
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Foxglove.Navigation {
    public sealed partial class FlowFieldDebugViewSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FlowField, FlowFieldSample>().Build());
        }

        protected override void OnUpdate() {
            foreach ((RefRO<FlowField> flowField, DynamicBuffer<FlowFieldSample> flowFieldSamples) in SystemAPI
                .Query<RefRO<FlowField>, DynamicBuffer<FlowFieldSample>>()) {
                int2 lowerBound = flowField.ValueRO.SouthWestCorner;
                int2 upperBound = flowField.ValueRO.NorthEastCorner;

                for (var x = 0; x < flowField.ValueRO.FieldSize.x; x++) {
                    for (var y = 0; y < flowField.ValueRO.FieldSize.y; y++) {
                        float3 cellCenter = new float3(x, 0, y) + 0.5f;
                        cellCenter.xz += lowerBound;

                        int sampleIndex = x + y * flowField.ValueRO.FieldSize.x;
                        int2 sampleDirection = flowFieldSamples[sampleIndex].Direction;

                        float3 lineEnd = cellCenter;
                        lineEnd.xz += sampleDirection;

                        Debug.DrawLine(cellCenter, lineEnd, Color.magenta, SystemAPI.Time.DeltaTime);
                    }
                }

                // Corners of the field
                float3 southWestCorner = new(lowerBound.x - 0.5f, 0.5f, lowerBound.y - 0.5f);
                float3 southEastCorner = new(upperBound.x + 0.5f, 0.5f, lowerBound.y - 0.5f);
                float3 northWestCorner = new(lowerBound.x - 0.5f, 0.5f, upperBound.y + 0.5f);
                float3 northEastCorner = new(upperBound.x + 0.5f, 0.5f, upperBound.y + 0.5f);

                // Draw field bounds
                Debug.DrawLine(southEastCorner, southWestCorner, Color.green, SystemAPI.Time.DeltaTime);
                Debug.DrawLine(southWestCorner, northWestCorner, Color.green, SystemAPI.Time.DeltaTime);
                Debug.DrawLine(northWestCorner, northEastCorner, Color.green, SystemAPI.Time.DeltaTime);
                Debug.DrawLine(northEastCorner, southEastCorner, Color.green, SystemAPI.Time.DeltaTime);
            }
        }
    }
}
#endif
