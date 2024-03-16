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
            foreach (FlowFieldAspect aspect in SystemAPI.Query<FlowFieldAspect>()) {
                FlowField field = aspect.FlowField.ValueRO;
                int2 southWestCoords = field.SouthWestCorner;
                int2 northEastCoords = field.NorthEastCorner;

                for (var x = 0; x < field.FieldSize.x; x++) {
                    for (var y = 0; y < field.FieldSize.y; y++) {
                        float3 cellCenter = new float3(x, 0, y) + 0.5f;
                        cellCenter.xz += southWestCoords;

                        int sampleIndex = aspect.IndexFromFieldCoordinates(new int2(x, y));
                        int2 sampleDirection = aspect.Samples[sampleIndex].Direction;

                        float3 lineEnd = cellCenter;
                        lineEnd.xz += sampleDirection;

                        Debug.DrawLine(cellCenter, cellCenter + math.up(), Color.cyan, SystemAPI.Time.DeltaTime);
                        Debug.DrawLine(cellCenter, lineEnd, Color.magenta, SystemAPI.Time.DeltaTime);
                    }
                }

                // Corners of the field
                float3 southWestCorner = new(southWestCoords.x - 0.5f, 0.5f, southWestCoords.y - 0.5f);
                float3 southEastCorner = new(northEastCoords.x + 0.5f, 0.5f, southWestCoords.y - 0.5f);
                float3 northWestCorner = new(southWestCoords.x - 0.5f, 0.5f, northEastCoords.y + 0.5f);
                float3 northEastCorner = new(northEastCoords.x + 0.5f, 0.5f, northEastCoords.y + 0.5f);

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
