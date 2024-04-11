#if UNITY_EDITOR
using Foxglove.Maps;
using Foxglove.Maps.Delaunay;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Foxglove.Editor.Maps {
    [BurstCompile]
    internal partial struct MapDebugSystem : ISystem {
        private EntityQuery _mapQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state) {
            _mapQuery = SystemAPI.QueryBuilder().WithAll<Room, Edge>().Build();
            state.RequireForUpdate(_mapQuery);
        }

        /// <summary>
        /// Request debug lines to be drawn from a background thread.
        /// If no entities are matched by the query, the jobs won't run.
        /// </summary>
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            float deltaTime = SystemAPI.Time.DeltaTime;

            JobHandle drawRooms = new DrawRoomDebugLinesJob {
                DrawTime = deltaTime,
                Color = Color.yellow,
            }.Schedule(_mapQuery, state.Dependency);

            JobHandle drawEdges = new DrawEdgeDebugLinesJob {
                DeltaTime = deltaTime,
                Color = Color.red,
            }.Schedule(_mapQuery, state.Dependency);

            state.Dependency = JobHandle.CombineDependencies(drawRooms, drawEdges);
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state) { }
    }
}
#endif
