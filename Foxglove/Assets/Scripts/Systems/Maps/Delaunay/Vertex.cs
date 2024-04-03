using System;
using Unity.Mathematics;

namespace Foxglove.Maps.Graphs {
    public struct Vertex : IEquatable<Vertex> {
        public float2 Position;

        public static implicit operator float2(Vertex v) => v.Position;
        public static implicit operator Vertex(float2 v) => new() { Position = v };

        public bool Equals(Vertex other) => Position.Equals(other.Position);
        public override bool Equals(object obj) => obj is Vertex other && Equals(other);

        public readonly override int GetHashCode() => Position.GetHashCode();
    }
}
