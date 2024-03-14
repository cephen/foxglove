using Foxglove.Agent;
using Foxglove.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Navigation {
    /// <summary>
    /// flow fields provide 3D pathfinding towards a given destination
    /// This system manages a flow field that Wisps use to navigate towards the player
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CheckpointUpdateGroup))]
    [DisableAutoCreation] // Disable this system for now
    public partial struct WispFlowFieldSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            // Query for transforms relevant to field calculation
            state.RequireForUpdate(
                SystemAPI.QueryBuilder().WithAll<LocalToWorld>().WithAny<PlayerCharacterTag, WispTag>().Build()
            );
            state.RequireForUpdate<Blackboard>();

            if (SystemAPI.HasSingleton<WispFlowField>()) return;

            Entity fieldEntity = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(fieldEntity, "Wisp Flow Field");
            state.EntityManager.AddComponent<FlowField>(fieldEntity);
            state.EntityManager.AddComponent<WispFlowField>(fieldEntity);
            state.EntityManager.AddBuffer<FlowFieldSample>(fieldEntity);
        }

        // Unused function but required by ISystem interface
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            // Calculate field bounds in grid coordinates
            var lowerBound = new int2(int.MaxValue);
            var upperBound = new int2(int.MinValue);

            foreach (RefRO<LocalToWorld> ltw in SystemAPI
                .Query<RefRO<LocalToWorld>>()
                .WithAny<PlayerCharacterTag, WispTag>()) {
                lowerBound = math.min(lowerBound, (int2)math.floor(ltw.ValueRO.Position.xz));
                upperBound = math.max(upperBound, (int2)math.ceil(ltw.ValueRO.Position.xz));
            }

            // Expand the border of the field by one unit in every direction,
            // ensuring the field is never zero sized
            lowerBound--;
            upperBound++;

            int2 fieldSize = upperBound - lowerBound;

            // In theory the blackboard should always be available, but just in case
            if (!SystemAPI.HasSingleton<Blackboard>()) {
                Log.Error("[WispFlowFieldSystem] Blackboard not found");
                return;
            }

            var blackboard = SystemAPI.GetSingleton<Blackboard>();

            Log.Debug("FlowField: Lower: {0}, Upper: {1}, Size: {2}", lowerBound, upperBound, fieldSize);

            foreach (RefRW<FlowField> field in SystemAPI
                .Query<RefRW<FlowField>>()
                .WithAll<WispFlowField>()) {
                // Convert destination from worldspace to fieldspace
                field.ValueRW.Destination = ToGridCoordinates(blackboard.PlayerPosition) - lowerBound;
                field.ValueRW.RegionSize = fieldSize;
                field.ValueRW.LowerBound = lowerBound;
                field.ValueRW.UpperBound = upperBound;
            }

            // Perform flow field calculations on a background thread
            new FlowFieldCalculationJob().Schedule();
        }

        [BurstCompile]
        private readonly int2 ToGridCoordinates(in float3 position) =>
            new((int)position.x, (int)position.z);

        /// <summary>
        /// This struct performs flow direction calculation for every node in the field.
        /// Implementing this as an IJob allows it to be run on a background thread.
        /// </summary>
        [BurstCompile]
        private partial struct FlowFieldCalculationJob : IJobEntity {
            /// <summary>
            /// Here's where all the work really happens.
            /// For each flow field in the world, perform a breadth first search starting from the destination.
            /// For now, each node has a uniform travel cost, so the first time a node is found it's flow
            /// can be assumed to travel to the neighbour it was discovered from
            /// </summary>
            [BurstCompile]
            public void Execute(ref FlowField field, ref DynamicBuffer<FlowFieldSample> samples) {
                // Set sample buffer to the correct size
                int cellCount = field.RegionSize.x * field.RegionSize.y;
                samples.Resize(cellCount, NativeArrayOptions.ClearMemory);

                // Temporary collections used to store cells to check
                NativeQueue<int2> frontier = new(Allocator.Temp);
                // And cells that have already been visited
                NativeHashSet<int2> visited = new(cellCount, Allocator.Temp);

                // The destination cell should be the first one checked
                frontier.Enqueue(field.Destination);
                visited.Add(field.Destination);

                // While there are cells left to check
                while (!frontier.IsEmpty()) {
                    int2 current = frontier.Dequeue();

                    // For each potential neighbour of the current cell
                    foreach (int2 next in NeighboursOf(current)) {
                        // Skip cells outside the bounds of the field
                        if (!IsInBounds(next, field.RegionSize)) continue;

                        // Skip cells already visited
                        if (visited.Contains(next)) continue;

                        // Queue new cells
                        frontier.Enqueue(next);
                        visited.Add(next);

                        // Store flow direction from neighbour to current
                        samples[IndexFromPosition(next, field.LowerBound, field.RegionSize)] = next - current;
                    }

                // Deallocate the collection now that we're done with it
                // This is necessary for all NativeCollection types provided by Unity.Collections
                frontier.Dispose();
                visited.Dispose();
            }

            /// <summary>
            /// Converts a position to an array index
            /// </summary>
            /// <param name="position">The WorldSpace grid coordinate of the position to be checked</param>
            /// <param name="lowerBound">The minimum WorldSpace grid coordinate of the field</param>
            /// <param name="fieldSize">The size of the field in cells</param>
            [BurstCompile]
            private readonly int IndexFromPosition(in int2 position, in int2 lowerBound, in int2 fieldSize) {
                int2 fieldSpacePosition = position - lowerBound;
                return fieldSpacePosition.y + fieldSpacePosition.x * fieldSize.x;
            }

            /// <summary>
            /// Returns the coordinates of potential neighbours of a given position
            /// </summary>
            [BurstCompile]
            private readonly NativeArray<int2> NeighboursOf(in int2 position) {
                var array = new NativeArray<int2>(8, Allocator.Temp);
                array[0] = position + new int2(-1, -1); // Southwest
                array[1] = position + new int2(-1, +0); // West
                array[2] = position + new int2(-1, +1); // Northwest
                array[3] = position + new int2(+0, +1); // North
                array[4] = position + new int2(+1, +1); // Northeast
                array[5] = position + new int2(+1, +0); // East
                array[6] = position + new int2(+1, -1); // Southeast
                array[7] = position + new int2(+0, -1); // South
                return array;
            }

            /// <summary>
            /// Helper function to check if a position is within the bounds of the flow field
            /// </summary>
            [BurstCompile]
            private readonly bool IsInBounds(in int2 position, in int2 regionSize) =>
                position is { x: >= 0, y: >= 0 } // funny pattern matching syntax to check if both x and y are >= 0
                && position.x < regionSize.x
                && position.y < regionSize.y;
        }
    }
}
