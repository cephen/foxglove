#if UNITY_EDITOR
using Foxglove.Maps;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Foxglove.Editor.Maps {
    [BurstCompile]
    internal partial struct DrawRoomDebugLinesJob : IJobEntity {
        internal float DrawTime;
        internal Color Color;

        [BurstCompile]
        private readonly void Execute(in DynamicBuffer<Room> rooms) {
            foreach (Room room in rooms) {
                var southWestCorner = new float3(room.Position.x, 0, room.Position.y);
                var southEastCorner = new float3(room.Position.x + room.Size.x, 0, room.Position.y);
                var northWestCorner = new float3(room.Position.x, 0, room.Position.y + room.Size.y);
                var northEastCorner = new float3(room.Position.x + room.Size.x, 0, room.Position.y + room.Size.y);

                Debug.DrawLine(southWestCorner, southEastCorner, Color, DrawTime);
                Debug.DrawLine(southEastCorner, northEastCorner, Color, DrawTime);
                Debug.DrawLine(northEastCorner, northWestCorner, Color, DrawTime);
                Debug.DrawLine(northWestCorner, southWestCorner, Color, DrawTime);
            }
        }
    }
}
#endif
