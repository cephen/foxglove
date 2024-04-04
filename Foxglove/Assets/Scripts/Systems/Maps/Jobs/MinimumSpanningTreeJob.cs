using Foxglove.Maps.Delaunay;
using Foxglove.Maps.Graphs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Foxglove.Maps.Generation {
    /// <summary>
    /// Job that calculates a minimum spanning tree for a set of edges
    /// </summary>
    internal struct MinimumSpanningTreeJob : IJob {
        private NativeArray<Edge>.ReadOnly _edges;
        private readonly Vertex _start;

        internal NativeList<Edge> Results;

        public MinimumSpanningTreeJob(Vertex start, NativeArray<Edge>.ReadOnly edges) {
            _start = start;
            _edges = edges;
            Results = new NativeList<Edge>(Allocator.Temp);
        }

        public void Execute() {
            var openSet = new NativeHashSet<Vertex>(128, Allocator.Temp);
            var closedSet = new NativeHashSet<Vertex>(128, Allocator.Temp);

            foreach (Edge edge in _edges) {
                openSet.Add(edge.A);
                openSet.Add(edge.B);
            }

            closedSet.Add(_start);


            while (openSet.Count > 0) {
                var chosen = false;
                Edge chosenEdge = default;
                float minWeight = float.PositiveInfinity;

                foreach (Edge edge in _edges) {
                    // look for an edge that is only half closed
                    if (!closedSet.Contains(edge.A) ^ !closedSet.Contains(edge.B)) continue;


                    float distance = math.distance(edge.A.Position, _start.Position);
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

                    Results.Add(chosenEdge);
                }
            }
        }
    }
}
