#if UNITY_EDITOR
using Foxglove.Core;
using Foxglove.Core.State;
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

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state) {
            _mapQuery = SystemAPI.QueryBuilder().WithAll<Map, Room, Edge>().Build();
            state.RequireForUpdate(_mapQuery);
        }

        /// <summary>
        /// Request debug lines to be drawn from a background thread.
        /// If no entities are matched by the query, the jobs won't run.
        /// </summary>
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState ecs) {
            JobHandle drawRooms = new DrawRoomDebugLinesJob {
                DrawTime = CheckpointUpdateGroup.UPDATE_RATE,
                Color = Color.yellow,
            }.Schedule(_mapQuery, ecs.Dependency);

            JobHandle drawEdges = new DrawEdgeDebugLinesJob {
                DeltaTime = CheckpointUpdateGroup.UPDATE_RATE,
                Color = Color.red,
            }.Schedule(_mapQuery, ecs.Dependency);

            ecs.Dependency = JobHandle.CombineDependencies(drawRooms, drawEdges);
            return;

        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state) { }
    }
}
#endif
