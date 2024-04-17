#if UNITY_EDITOR
using Foxglove.Core;
using Foxglove.Maps;
using Foxglove.Maps.Delaunay;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Foxglove.Editor.Maps {
    [BurstCompile]
    [UpdateInGroup(typeof(CheckpointUpdateGroup))]
    internal partial struct MapDebugSystem : ISystem {
        private EntityQuery _mapQuery;

        public void OnCreate(ref SystemState state) {
            _mapQuery = SystemAPI.QueryBuilder().WithAll<Map, Room, Edge>().Build();
            state.RequireForUpdate(_mapQuery);
        }

        /// <summary>
        /// Request debug lines to be drawn from a background thread.
        /// If no entities are matched by the query, the jobs won't run.
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState ecs) {
            JobHandle drawRooms = new DrawRoomDebugLinesJob {
                DrawTime = CheckpointUpdateGroup.UpdateRate,
                Color = Color.yellow,
            }.Schedule(_mapQuery, ecs.Dependency);

            JobHandle drawEdges = new DrawEdgeDebugLinesJob {
                DeltaTime = CheckpointUpdateGroup.UpdateRate,
                Color = Color.red,
            }.Schedule(_mapQuery, ecs.Dependency);

            ecs.Dependency = JobHandle.CombineDependencies(drawRooms, drawEdges);
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
#endif
