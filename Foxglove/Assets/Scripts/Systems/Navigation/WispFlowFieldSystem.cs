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
                lowerBound.x = math.min(lowerBound.x, (int)ltw.ValueRO.Position.x);
                lowerBound.y = math.min(lowerBound.y, (int)ltw.ValueRO.Position.z);
                upperBound.x = math.max(upperBound.x, (int)ltw.ValueRO.Position.x);
                upperBound.y = math.max(upperBound.y, (int)ltw.ValueRO.Position.z);
            }

            // If all entities are in the same row or column,
            // the calculated width/height will be zero,
            // which can cause a zero sized buffer to be allocated later on
            // of course, that is seriously no bueno :'c
            int2 fieldSize = upperBound - lowerBound + 1;

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
                field.ValueRW.Destination = ToGridCoordinates(blackboard.PlayerPosition);
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

            }
        }
    }

    [BurstCompile]
    public partial struct FlowFieldUpdateJob : IJobEntity {
        // Length of each side of the field in voxels
        private const uint FieldSideLength = 100;

        // This method will be run for each flow field
        [BurstCompile]
        public void Execute(ref FlowFieldTarget flowFieldTarget, ref DynamicBuffer<FlowFieldSample> samples) {
            // Create a list of nodes to scan, starting with the destination
            // For each node in open list:
            // - Add node neighbours to open list if not already searched
            // - Determine cost of node
            // - Determine direction of node
            // - Add node to closed list
        }

        private int2 ToGridCoordinates(float3 position, float3 fieldOrigin) {
            float3 positionInField = position - fieldOrigin;
            var positionCoordinates = new int2(
                (int)math.floor(positionInField.x),
                (int)math.floor(positionInField.z)
            );
            return positionCoordinates;
        }
    }
}
