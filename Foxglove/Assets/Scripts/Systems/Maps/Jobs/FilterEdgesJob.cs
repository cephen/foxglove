using Foxglove.Maps.Delaunay;
using Foxglove.Maps.Graphs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Foxglove.Maps.Jobs {
    /// <summary>
    /// Job that calculates a minimum spanning tree for a set of edges
    /// </summary>
    [BurstCompile]
    internal struct FilterEdgesJob : IJob {
        [ReadOnly] internal NativeArray<Edge>.ReadOnly Edges;
        [ReadOnly] internal Vertex Start;
        internal Random Random;

        internal NativeList<Edge> Results;

        public void Execute() {
            FilterEdges();
            RestoreSomeEdges();
        }

        private void FilterEdges() {
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
                    bool hasA = closedSet.Contains(edge.A);
                    bool hasB = closedSet.Contains(edge.B);
                    if (!(hasA ^ hasB)) continue;

                    float distance = math.distance(edge.A.Position, edge.B.Position);
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

            openSet.Dispose();
            closedSet.Dispose();
            chosenEdges.Dispose();
        }

        private void RestoreSomeEdges() {
            NativeHashSet<Edge> remainingEdges = new(Results.Length, Allocator.Temp);
            foreach (Edge edge in Edges) remainingEdges.Add(edge);

            remainingEdges.ExceptWith(Results.AsArray());

            foreach (Edge edge in remainingEdges) {
                if (Random.NextDouble() < 0.125)
                    Results.Add(edge);
            }

            remainingEdges.Dispose();
        }
    }
}
