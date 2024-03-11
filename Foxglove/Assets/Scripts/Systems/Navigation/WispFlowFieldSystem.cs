using Foxglove.Agent;
using Foxglove.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            // For each flow field, if a target exists && target voxel coordinate changed:
            // - Update field target coordinates
            // - Recalculate flow field samples

            // There's only one player controller but it's not stored as a singleton, will maybe fix later (sike)
            // So a foreach loop must be written even though there will only ever be one iteration
            foreach (FlowFieldAspect flowField in SystemAPI.Query<FlowFieldAspect>()) {
                if (!SystemAPI.Exists(flowField.Target.ValueRO.TargetEntity)
                    || flowField.Target.ValueRO.TargetEntity == Entity.Null) {
                    Entity playerCharacter = SystemAPI.GetSingleton<PlayerController>().ControlledCharacter;
                    flowField.Target.ValueRW.TargetEntity = playerCharacter;
                }

                Entity targetEntity = flowField.Target.ValueRO.TargetEntity;

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
