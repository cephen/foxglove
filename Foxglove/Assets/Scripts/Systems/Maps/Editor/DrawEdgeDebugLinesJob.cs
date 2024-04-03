using Foxglove.Maps.Delaunay;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Foxglove.Maps.Editor {
    [BurstCompile]
    internal struct DrawEdgeDebugLinesJob : IJobFor {
        public float DeltaTime;
        public Color Colour;
        public NativeArray<Edge>.ReadOnly Edges;

        [BurstCompile]
        public readonly void Execute(int i) {
            Edge edge = Edges[i];
            float3 start = new(edge.A.Position.x, 0, edge.A.Position.y);
            float3 end = new(edge.B.Position.x, 0, edge.B.Position.y);
            Debug.DrawLine(start, end, Colour, DeltaTime);
        }
    }
}
