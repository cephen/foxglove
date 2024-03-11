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

                float3 targetPosition = SystemAPI.GetComponent<LocalToWorld>(targetEntity).Position;

                float minX = float.MaxValue, minZ = float.MaxValue;
                float maxX = float.MinValue, maxZ = float.MinValue;


                foreach (RefRO<LocalToWorld> transform in SystemAPI.Query<RefRO<LocalToWorld>>().WithAny<WispTag>()) {
                    float3 position = transform.ValueRO.Position;
                    minX = math.min(math.floor(position.x), minX);
                    minZ = math.min(math.floor(position.z), minZ);
                    maxX = math.max(math.floor(position.x), maxX);
                    maxZ = math.max(math.floor(position.z), maxZ);
                }

                var fieldOrigin = new int2((int)minX, (int)minZ);
                var fieldSize = new int2(
                    (int)math.distance(maxX, minX),
                    (int)math.distance(maxZ, minZ)
                );
                var samples = new NativeArray<FlowFieldSample>(fieldSize.x * fieldSize.y, Allocator.Temp);
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
