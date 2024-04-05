using Foxglove.Maps.Delaunay;
using Foxglove.Maps.Graphs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Foxglove.Maps.Jobs {
    /// <summary>
    /// Job that calculates a minimum spanning tree for a set of edges
    /// </summary>
    internal struct MinimumSpanningTreeJob : IJob {
        [ReadOnly] internal Vertex Start;
        [ReadOnly] internal DynamicBuffer<Edge> Edges;

        internal NativeList<Edge> Results;

        public void Execute() {
            var openSet = new NativeHashSet<Vertex>(128, Allocator.Temp);
            var closedSet = new NativeHashSet<Vertex>(128, Allocator.Temp);
            var chosenEdges = new NativeHashSet<Edge>(128, Allocator.Temp);

            foreach (Edge edge in Edges) {
                openSet.Add(edge.A);
                openSet.Add(edge.B);
            }

            closedSet.Add(Start);


            while (openSet.Count > 0) {
                var chosen = false;
                Edge chosenEdge = default;
                float minWeight = float.PositiveInfinity;

                foreach (Edge edge in Edges) {
                    // look for an edge that is only half closed
                    if (!closedSet.Contains(edge.A) ^ !closedSet.Contains(edge.B)) continue;


                    float distance = math.distance(edge.A.Position, Start.Position);
                    if (distance < minWeight) {
                        chosen = true;
                        chosenEdge = edge;
                        minWeight = distance;
                    }

                    if (!chosen) break;

                    openSet.Remove(chosenEdge.A);
                    openSet.Remove(chosenEdge.B);
                    closedSet.Add(chosenEdge.A);
                    closedSet.Add(chosenEdge.B);

                    chosenEdges.Add(chosenEdge);
                }
            }

            foreach (Edge edge in chosenEdges) Results.Add(edge);
        }
    }
}
