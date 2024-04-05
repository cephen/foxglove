using Foxglove.Maps.Delaunay;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Foxglove.Maps.Jobs {
    [BurstCompile]
    internal struct AddHallwaysJob : IJob {
        [ReadOnly] internal MapConfig Config;
        [ReadOnly] internal NativeArray<Edge> AllEdges;
        internal NativeList<Edge> SelectedEdges;

        private Random _random;

        [BurstCompile]
        public void Execute() {
            _random = new Random(Config.Seed);

            var remainingEdges = new NativeHashSet<Edge>(AllEdges.Length, Allocator.Temp);
            foreach (Edge edge in AllEdges) remainingEdges.Add(edge);

            remainingEdges.ExceptWith(SelectedEdges);


            foreach (Edge edge in remainingEdges) {
                // Restore 1/8 of the unselected edges
                if (_random.NextDouble() < 0.125)
                    SelectedEdges.Add(edge);
            }
        }
    }
}
