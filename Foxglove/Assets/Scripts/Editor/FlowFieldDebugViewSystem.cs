using Foxglove.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Foxglove.Editor {
    [BurstCompile]
    internal partial struct FlowFieldDebugViewSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FlowField, FlowFieldSample>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            state.Dependency = new PaintDebugLinesJob().ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        private partial struct PaintDebugLinesJob : IJobEntity {
            public float DeltaTime;

            [BurstCompile]
            public readonly void Execute(FlowFieldAspect aspect) {
                FlowField field = aspect.FlowField.ValueRO;
                int2 southWestCoords = field.SouthWestCorner;
                int2 northEastCoords = field.NorthEastCorner;

                for (var x = 0; x < field.FieldSize.x; x++) {
                    for (var y = 0; y < field.FieldSize.y; y++) {
                        float3 cellCenter = new float3(x, 0, y) + 0.5f;
                        cellCenter.xz += southWestCoords;

                        int sampleIndex = aspect.IndexFromFieldCoordinates(new int2(x, y));
                        int2 flowDirection = aspect.Samples[sampleIndex].Direction;

                        float3 lineEnd = cellCenter;
                        lineEnd.xz += flowDirection;

                        Debug.DrawLine(cellCenter, cellCenter + math.up(), Color.cyan, DeltaTime);
                        Debug.DrawLine(cellCenter, lineEnd, Color.magenta, DeltaTime);
                    }
                }

                // Corners of the field
                float3 southWestCorner = new(southWestCoords.x - 0.5f, 0.5f, southWestCoords.y - 0.5f);
                float3 southEastCorner = new(northEastCoords.x + 0.5f, 0.5f, southWestCoords.y - 0.5f);
                float3 northWestCorner = new(southWestCoords.x - 0.5f, 0.5f, northEastCoords.y + 0.5f);
                float3 northEastCorner = new(northEastCoords.x + 0.5f, 0.5f, northEastCoords.y + 0.5f);

                // Draw field bounds
                Debug.DrawLine(southEastCorner, southWestCorner, Color.green, DeltaTime);
                Debug.DrawLine(southWestCorner, northWestCorner, Color.green, DeltaTime);
                Debug.DrawLine(northWestCorner, northEastCorner, Color.green, DeltaTime);
                Debug.DrawLine(northEastCorner, southEastCorner, Color.green, DeltaTime);
            }
        }
    }
}
