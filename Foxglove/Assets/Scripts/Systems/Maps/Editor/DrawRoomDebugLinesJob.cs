using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Foxglove.Maps.Editor {
    [BurstCompile]
    internal struct DrawRoomDebugLinesJob : IJobFor {
        public float DeltaTime;
        public Color Colour;
        [ReadOnly] public NativeArray<Room> Rooms;

        [BurstCompile]
        public readonly void Execute(int i) {
            Room room = Rooms[i];

            var southWestCorner = new float3(room.Position.x, 0, room.Position.y);
            var southEastCorner = new float3(room.Position.x + room.Size.x, 0, room.Position.y);
            var northWestCorner = new float3(room.Position.x, 0, room.Position.y + room.Size.y);
            var northEastCorner = new float3(room.Position.x + room.Size.x, 0, room.Position.y + room.Size.y);

            Debug.DrawLine(southWestCorner, southEastCorner, Colour, DeltaTime);
            Debug.DrawLine(southEastCorner, northEastCorner, Colour, DeltaTime);
            Debug.DrawLine(northEastCorner, northWestCorner, Colour, DeltaTime);
            Debug.DrawLine(northWestCorner, southWestCorner, Colour, DeltaTime);
        }
    }
}
