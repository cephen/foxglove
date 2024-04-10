using Foxglove.Maps.Delaunay;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Foxglove.Maps.Editor {
    [BurstCompile]
    internal partial struct DrawEdgeDebugLinesJob : IJobEntity {
        public float DeltaTime;
        public Color Color;

        [BurstCompile]
        private readonly void Execute(in DynamicBuffer<Edge> edges) {
            foreach (Edge edge in edges) {
                float3 start = new(edge.A.Position.x, 0, edge.A.Position.y);
                float3 end = new(edge.B.Position.x, 0, edge.B.Position.y);
                Debug.DrawLine(start, end, Color, DeltaTime);
            }
        }
    }
}
