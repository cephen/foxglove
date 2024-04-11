﻿using Foxglove.Maps.Delaunay;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Foxglove.Maps.Jobs {
    /// <summary>
    /// Incomplete job that will eventually be used to set hallway cells on the map.
    /// </summary>
    [BurstCompile]
    internal struct SetMapCells : IJobParallelFor {
        [ReadOnly] internal MapConfig Config;
        internal NativeArray<Edge>.ReadOnly Hallways;
        internal NativeArray<Room>.ReadOnly Rooms;

        internal NativeArray<MapCell> Results;

        public void Execute() {
            for (var i = 0; i < Results.Length; i++) Execute(i);
        }

        public void Execute(int i) {
            int2 cellCoord = IndexToCoord(i);
            foreach (Edge edge in Hallways) {
                if (!EdgeIntersectsCell(edge, cellCoord)) continue;
                Results[i] = CellType.Hallway;
            }
        }

        private readonly int2 IndexToCoord(in int i) => new(i % Config.Diameter, i / Config.Diameter);

        private readonly bool EdgeIntersectsCell(in Edge edge, in int2 cellCoordinate) {
            var hallway = new LineSegment(edge.A, edge.B);

            NativeArray<LineSegment> borders = CellBorders(cellCoordinate);
            var intersects = false;

            foreach (LineSegment border in borders) {
                if (!hallway.Intersects(border)) continue;

                intersects = true;
                break;
            }

            borders.Dispose();
            return intersects;
        }

        private readonly NativeArray<LineSegment> CellBorders(in int2 cellCoordinate) {
            var borders = new NativeArray<LineSegment>(4, Allocator.Temp);

            float2 southWest = cellCoordinate;
            float2 northWest = cellCoordinate + new int2(0, 1);
            float2 northEast = cellCoordinate + new int2(1, 1);
            float2 southEast = cellCoordinate + new int2(1, 0);

            borders[0] = new LineSegment(southWest, northWest);
            borders[1] = new LineSegment(northWest, northEast);
            borders[2] = new LineSegment(northEast, southEast);
            borders[3] = new LineSegment(southEast, southWest);

            return borders;
        }
    }

    internal readonly struct LineSegment {
        private readonly float2 _start;
        private readonly float2 _end;

        public LineSegment(float2 start, float2 end) {
            _start = start;
            _end = end;
        }

        /// <summary>
        /// Source: https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection#Given_two_points_on_each_line
        /// </summary>
        internal bool Intersects(LineSegment other) {
            float x1 = _start.x;
            float y1 = _start.y;
            float x2 = _end.x;
            float y2 = _end.y;

            float x3 = other._start.x;
            float y3 = other._start.y;
            float x4 = other._end.x;
            float y4 = other._end.y;

            float tTop = (x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4);
            float tBot = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            float t = tTop / tBot;

            float uTop = (x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3);
            float uBot = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            float u = uTop / uBot;

            return t is >= 0 and <= 1
                   && u is >= 0 and <= 1;
        }
    }
}
