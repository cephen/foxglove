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
    [UpdateInGroup(typeof(BlackboardUpdateGroup))]
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
            var southWestCorner = new int2(int.MaxValue);
            var northEastCorner = new int2(int.MinValue);

            foreach (RefRO<LocalToWorld> ltw in SystemAPI
                .Query<RefRO<LocalToWorld>>()
                .WithAny<PlayerCharacterTag, WispTag>()) {
                float2 position = ltw.ValueRO.Position.xz;
                southWestCorner = math.min(southWestCorner, (int2)math.floor(position));
                northEastCorner = math.max(northEastCorner, (int2)math.ceil(position));
            }

            // Expand the border of the field by one unit in every direction,
            // ensuring the field is never zero sized
            southWestCorner--;
            northEastCorner++;

            int2 fieldSize = northEastCorner - southWestCorner;

            // In theory the blackboard should always be available, but just in case
            if (!SystemAPI.HasSingleton<Blackboard>()) {
                Log.Error("[WispFlowFieldSystem] Blackboard not found");
                return;
            }

            var blackboard = SystemAPI.GetSingleton<Blackboard>();

            Log.Debug("FlowField: Lower: {0}, Upper: {1}, Size: {2}", southWestCorner, northEastCorner, fieldSize);

            foreach ((FlowFieldAspect field, DynamicBuffer<FlowFieldSample> sampleBuffer) in SystemAPI
                .Query<FlowFieldAspect, DynamicBuffer<FlowFieldSample>>()
                .WithAll<WispFlowField>()) {
                field.SetDestination(blackboard.PlayerPosition);
                field.FlowField.ValueRW.FieldSize = fieldSize;
                field.FlowField.ValueRW.SouthWestCorner = southWestCorner;
                field.FlowField.ValueRW.NorthEastCorner = northEastCorner;
                sampleBuffer.Resize(fieldSize.x * fieldSize.y, NativeArrayOptions.ClearMemory);
            }

            // Perform flow field calculations on a background thread
            new FlowFieldCalculationJob().Schedule();
        }

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
            public readonly void Execute(FlowFieldAspect aspect) {
                DynamicBuffer<FlowFieldSample> flowBuffer = aspect.Samples;
                ref FlowField field = ref aspect.FlowField.ValueRW;
                int cellCount = field.FieldSize.x * field.FieldSize.y;

                // Temporary collections used to store cells to check...
                NativeQueue<int2> uncheckedCells = new(Allocator.Temp);
                // ...and cells that have already been checked
                NativeHashSet<int2> checkedCells = new(cellCount, Allocator.Temp);

                // The destination cell should be the first one checked
                uncheckedCells.Enqueue(field.Destination);
                checkedCells.Add(field.Destination);

                int CellCost(int2 cell, int2 destination) {
                    int2 offset = cell - destination;
                    return math.abs(offset.x) + math.abs(offset.y);
                }

                // While there are cells left to check
                while (!uncheckedCells.IsEmpty()) {
                    int2 current = uncheckedCells.Dequeue();
                    float currentDistance = math.distance(current, field.Destination);

                    float bestNeighbourCost = currentDistance;

                    // For each potential neighbour of the current cell
                    foreach (int2 neighbour in NeighboursOf(current)) {
                        // Skip neighbour if it's outside the bounds of the field
                        if (!aspect.IsInBounds(neighbour)) continue;

                        // If neighbour hasn't been checked before, add it to the queue
                        if (!checkedCells.Contains(neighbour)) {
                            uncheckedCells.Enqueue(neighbour);
                            checkedCells.Add(neighbour);
                        }

                        // Update flow direction if neighbour costs less than previous cheapest neighbour
                        float neighbourCost = math.distance(neighbour, field.Destination);
                        if (neighbourCost > bestNeighbourCost) continue;

                        // Store best flow direction
                        flowBuffer[aspect.IndexFromFieldCoordinates(neighbour)] = neighbour - current;
                        bestNeighbourCost = neighbourCost;
                    }
                }

                // Deallocate the collections now that we're done with them
                // This is necessary for all NativeCollection types provided by Unity.Collections
                uncheckedCells.Dispose();
                checkedCells.Dispose();
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
        }
    }
}
