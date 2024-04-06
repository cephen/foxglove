using Foxglove.Maps.Delaunay;
using Foxglove.Maps.Graphs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Foxglove.Maps.Jobs {
    [BurstCompile]
    internal struct TriangulateMapJob : IJob {
        internal NativeArray<Room>.ReadOnly Rooms;
        internal NativeList<Edge> Edges;

        public void Execute() {
            NativeList<float2> vertices = ExtractVerticesFromRooms();
            NativeList<Triangle> triangles = InitializeTriangles(vertices);

            ProcessVertices(vertices, triangles);
            RemoveFirstTriangle(triangles);
            ConvertTrianglesToEdges(triangles);

            vertices.Dispose();
            triangles.Dispose();
        }

        /// <summary>
        /// Convert each room to a graph vertex.
        /// Each vertex is placed at the center of it's room
        /// </summary>
        private NativeList<float2> ExtractVerticesFromRooms() {
            NativeList<float2> vertices = new(Rooms.Length, Allocator.Temp);
            foreach (Room room in Rooms) vertices.Add(room.Center);
            return vertices;
        }

        private static NativeList<Triangle> InitializeTriangles(NativeList<float2> vertices) {
            NativeList<Triangle> triangles = new(Allocator.Temp);
            float2 min = vertices[0];
            float2 max = vertices[0];

            foreach (float2 vertex in vertices) {
                min = math.min(min, vertex);
                max = math.max(max, vertex);
            }

            float dx = max.x - min.x;
            float dy = max.y - min.y;
            float deltaMax = math.max(dx, dy) * 2;

            Vertex v1 = new float2(min.x - 1, min.y - 1);
            Vertex v2 = new float2(min.x - 1, max.y + deltaMax);
            Vertex v3 = new float2(max.x + deltaMax, min.y - 1);

            triangles.Add(new Triangle(v1, v2, v3));
            return triangles;
        }

        private static void ProcessVertices(NativeList<float2> vertices, NativeList<Triangle> triangles) {
            foreach (Vertex vertex in vertices) ProcessVertex(vertex, triangles);
        }

        private static void ProcessVertex(Vertex vertex, NativeList<Triangle> triangles) {
            NativeList<Edge> polygon = new(Allocator.Temp);

            for (var i = 0; i < triangles.Length; i++) {
                Triangle triangle = triangles[i];
                if (!triangle.CircumCircleContains(vertex.Position)) continue;
                MarkTriangleAsBadAndAddEdges(triangle, polygon);
                triangles[i] = triangle;
            }

            RemoveBadTriangles(triangles);
            RemoveDuplicateEdges(polygon);
            AddNewTrianglesFromPolygon(polygon, vertex, triangles);

            polygon.Dispose();
        }

        private static void MarkTriangleAsBadAndAddEdges(Triangle triangle, NativeList<Edge> polygon) {
            triangle.IsBad = true;
            polygon.Add(new Edge { A = triangle.A, B = triangle.B });
            polygon.Add(new Edge { A = triangle.B, B = triangle.C });
            polygon.Add(new Edge { A = triangle.C, B = triangle.A });
        }

        private static void RemoveBadTriangles(NativeList<Triangle> triangles) {
            for (int i = triangles.Length - 1; i >= 0; i--) {
                if (triangles[i].IsBad)
                    triangles.RemoveAt(i);
            }
        }

        private static void RemoveDuplicateEdges(NativeList<Edge> polygon) {
            for (var i = 0; i < polygon.Length; i++) {
                Edge eye = polygon[i];
                for (int j = i + 1; j < polygon.Length; j++) {
                    Edge jay = polygon[j];
                    if (!Edge.AlmostEqual(eye, jay)) continue;
                    eye.IsBad = true;
                    jay.IsBad = true;
                    polygon[i] = eye;
                    polygon[j] = jay;
                }
            }

            for (int i = polygon.Length - 1; i >= 0; i--) {
                if (polygon[i].IsBad)
                    polygon.RemoveAt(i);
            }
        }

        private static void AddNewTrianglesFromPolygon(
            NativeList<Edge> polygon,
            Vertex vertex,
            NativeList<Triangle> triangles
        ) {
            foreach (Edge edge in polygon) triangles.Add(new Triangle(edge.A, edge.B, vertex));
        }

        private static void RemoveFirstTriangle(NativeList<Triangle> triangles) {
            Triangle firstTri = triangles[0];

            for (int i = triangles.Length - 1; i >= 0; i--) {
                Triangle t = triangles[i];
                if (t.ContainsVertex(firstTri.A)
                    || t.ContainsVertex(firstTri.B)
                    || t.ContainsVertex(firstTri.C)) triangles.RemoveAt(i);
            }
        }

        private void ConvertTrianglesToEdges(NativeList<Triangle> triangles) {
            NativeHashSet<Edge> edgeSet = new(128, Allocator.Temp);

            foreach (Triangle t in triangles) {
                var ab = new Edge { A = t.A, B = t.B };
                var bc = new Edge { A = t.B, B = t.C };
                var ca = new Edge { A = t.C, B = t.A };

                if (edgeSet.Add(ab)) Edges.Add(ab);
                if (edgeSet.Add(bc)) Edges.Add(bc);
                if (edgeSet.Add(ca)) Edges.Add(ca);
            }

            edgeSet.Dispose();
        }
    }
}
