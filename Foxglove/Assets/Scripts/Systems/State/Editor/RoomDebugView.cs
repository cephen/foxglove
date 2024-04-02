using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Foxglove.State.Editor {
    [BurstCompile]
    public partial struct RoomDebugPainterSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Room>();
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            state.Dependency = new PaintRoomsJob {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct PaintRoomsJob : IJobEntity {
            public float DeltaTime;

            [BurstCompile]
            private readonly void Execute(in Room room) {
                // Room Corners
                var southWestCorner = new float3(room.Position.x, 0, room.Position.y);
                var southEastCorner = new float3(room.Position.x + room.Size.x, 0, room.Position.y);
                var northWestCorner = new float3(room.Position.x, 0, room.Position.y + room.Size.y);
                var northEastCorner = new float3(room.Position.x + room.Size.x, 0, room.Position.y + room.Size.y);

                Debug.DrawLine(southWestCorner, southEastCorner, Color.green, DeltaTime);
                Debug.DrawLine(southEastCorner, northEastCorner, Color.green, DeltaTime);
                Debug.DrawLine(northEastCorner, northWestCorner, Color.green, DeltaTime);
                Debug.DrawLine(northWestCorner, southWestCorner, Color.green, DeltaTime);
            }
        }
    }
}
