using System;
using Foxglove.Maps.Graphs;
using Unity.Mathematics;

namespace Foxglove.Maps.Delaunay {
    internal struct Edge : IEquatable<Edge> {
        public Vertex A;
        public Vertex B;
        public bool IsBad;

        public static bool operator ==(Edge left, Edge right) =>
            (left.A.Equals(right.A) || left.A.Equals(right.B))
            && (left.B.Equals(right.A) || left.B.Equals(right.B));

        public static bool operator !=(Edge left, Edge right) => !(left == right);

        public readonly bool Equals(Edge e) => this == e;

        public readonly override bool Equals(object obj) {
            if (obj is Edge e) return this == e;

            return false;
        }

        public readonly override int GetHashCode() => A.GetHashCode() ^ B.GetHashCode();

        public static bool AlmostEqual(Edge left, Edge right) =>
            (AlmostEqual(left.A, right.A) && AlmostEqual(left.B, right.B))
            || (AlmostEqual(left.A, right.B) && AlmostEqual(left.B, right.A));

        private static bool AlmostEqual(float x, float y) =>
            math.abs(x - y) <= float.Epsilon * math.abs(x + y) * 2
            || math.abs(x - y) < float.MinValue;

        private static bool AlmostEqual(Vertex left, Vertex right) =>
            AlmostEqual(left.Position.x, right.Position.x) && AlmostEqual(left.Position.y, right.Position.y);
    }
}
