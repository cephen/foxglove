using System;
using System.Collections.Generic;
using BlueRaja;
using Foxglove.Maps.Delaunay;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Foxglove.Maps.Jobs {
    internal struct Node : IEquatable<Node> {
        internal int2 Position;
        internal float Cost;

        public bool Equals(Node other) => Position.Equals(other.Position);
    }

    internal struct PathCost {
        internal bool IsTraversable;
        internal float Cost;
    }

    internal struct PlaceHallwaysJob : IJob {
        internal NativeArray<Edge>.ReadOnly Edges;
        internal NativeArray<Room>.ReadOnly Rooms;
        internal NativeArray<Node> Cells;
        internal int2 Start;


        internal MapConfig Config;
        private NativeHashSet<Node> _closed;
        private SimplePriorityQueue<Node> _queue;
        private Stack<int2> _stack;

        public void Execute() {
            InitBuffers();
        }

        private readonly int GetIndex(int x, int y) => y * Config.Diameter + x;

        private void InitBuffers() {
            Cells = new NativeArray<Node>(Config.Diameter * Config.Diameter, Allocator.Temp);
            _closed = new NativeHashSet<Node>(Config.Diameter * Config.Diameter, Allocator.Temp);
            _queue = new SimplePriorityQueue<Node>();
            _stack = new Stack<int2>();

            for (var y = 0; y < Config.Diameter; y++) {
                for (var x = 0; x < Config.Diameter; x++) {
                    Cells[y * Config.Diameter + x] = new Node {
                        Position = new int2(x, y),
                    };
                }
            }
        }
    }
}
