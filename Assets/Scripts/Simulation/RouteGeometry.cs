using System.Collections.Generic;
using UnityEngine;

namespace Bunker.Simulation
{
    // Shape of the routes soldiers walk, shared by the route builder and the route overlay
    public static class RouteGeometry
    {
        // Soldiers never move diagonally: two points out of line need an "L" corner between them
        public static bool TryGetCorner(Vector3 from, Vector3 to, bool verticalFirst, out Vector3 corner)
        {
            corner = verticalFirst ? new Vector3(from.x, to.y, from.z) : new Vector3(to.x, from.y, from.z);
            return Mathf.Abs(from.x - to.x) > 0.001f && Mathf.Abs(from.y - to.y) > 0.001f;
        }

        // Appends a flat strip along the polyline. uv.x is the distance walked and uv.y the distance from the centre line, both in world units
        public static void AppendStrip(IReadOnlyList<Vector3> polyline, float halfWidth, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            float walked = 0f;
            for (int i = 1; i < polyline.Count; i++)
            {
                Vector2 a = polyline[i - 1], b = polyline[i];
                float length = Vector2.Distance(a, b);
                if (length < 0.001f) continue;

                Vector2 direction = (b - a) / length;
                Vector2 side = new Vector2(-direction.y, direction.x) * halfWidth;

                int first = vertices.Count;
                vertices.Add(a + side);
                vertices.Add(a - side);
                vertices.Add(b + side);
                vertices.Add(b - side);
                uvs.Add(new Vector2(walked, halfWidth));
                uvs.Add(new Vector2(walked, -halfWidth));
                uvs.Add(new Vector2(walked + length, halfWidth));
                uvs.Add(new Vector2(walked + length, -halfWidth));
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
                triangles.Add(first + 2);
                triangles.Add(first + 1);
                triangles.Add(first + 3);

                walked += length;
            }
        }
    }
}
