using System;
using Unity.Mathematics;

namespace Foxglove.Maps.Delaunay {
    internal struct Triangle : IEquatable<Triangle> {
        internal Vertex A, B, C;
        internal bool IsBad;

        public Triangle(Vertex a, Vertex b, Vertex c) : this() {
            A = a;
            B = b;
            C = c;
        }

        public readonly bool ContainsVertex(float2 v) =>
            math.distance(v, A.Position) < 0.01f
            || math.distance(v, B.Position) < 0.01f
            || math.distance(v, C.Position) < 0.01f;


        public readonly bool CircumCircleContains(float2 v) {
            float2 a = A.Position;
            float2 b = B.Position;
            float2 c = C.Position;

            float ab = math.lengthsq(a);
            float cd = math.lengthsq(b);
            float ef = math.lengthsq(c);

            float circumX = (ab * (c.y - b.y) + cd * (a.y - c.y) + ef * (b.y - a.y))
                            / (a.x * (c.y - b.y) + b.x * (a.y - c.y) + c.x * (b.y - a.y));
            float circumY = (ab * (c.x - b.x) + cd * (a.x - c.x) + ef * (b.x - a.x))
                            / (a.y * (c.x - b.x) + b.y * (a.x - c.x) + c.y * (b.x - a.x));

            var circum = new float2(circumX / 2, circumY / 2);
            float circumRadius = math.lengthsq(a - circum);
            float dist = math.lengthsq(v - circum);
            return dist <= circumRadius;
        }

        /// <summary>
        /// Returns true if all vertices in left triangle are in right
        /// </summary>
        public static bool operator ==(Triangle left, Triangle right) =>
            (left.A.Equals(right.A) || left.A.Equals(right.B) || left.A.Equals(right.C))
            && (left.B.Equals(right.A) || left.B.Equals(right.B) || left.B.Equals(right.C))
            && (left.C.Equals(right.A) || left.C.Equals(right.B) || left.C.Equals(right.C));

        public static bool operator !=(Triangle left, Triangle right) => !(left == right);

        public readonly override bool Equals(object obj) {
            if (obj is Triangle t) return this == t;

            return false;
        }

        public readonly bool Equals(Triangle t) => this == t;

        public readonly override int GetHashCode() => A.GetHashCode() ^ B.GetHashCode() ^ C.GetHashCode();
    }
}
