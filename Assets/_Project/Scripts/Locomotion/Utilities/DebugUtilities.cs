using UnityEngine;

namespace Locomotion
{
    public static class DebugUtilities
    {
        /// <summary>
        /// Draws a wireframe sphere using Debug.DrawLine with duration
        /// </summary>
        public static void DrawWireSphere(Vector3 position, float radius, Color color, float duration = 0, int segments = 16)
        {
            float step = Mathf.PI * 2.0f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = step * i;
                float angle2 = step * (i + 1);

                // XY Plane
                Debug.DrawLine(
                    position + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * radius,
                    position + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * radius,
                    color, duration
                );

                // XZ Plane
                Debug.DrawLine(
                    position + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius,
                    position + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius,
                    color, duration
                );

                // YZ Plane
                Debug.DrawLine(
                    position + new Vector3(0, Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius,
                    position + new Vector3(0, Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius,
                    color, duration
                );
            }
        }

        /// <summary>
        /// Draws a wireframe cube using Debug.DrawLine with duration
        /// </summary>
        public static void DrawWireCube(Vector3 position, Vector3 size, Matrix4x4 rotationMatrix, Color color,
            float duration = 0)
        {
            Vector3 halfSize = size * 0.5f;

            // Define cube vertices
            Vector3[] vertices = new Vector3[8]
            {
                position + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                position + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
                position + new Vector3(halfSize.x, halfSize.y, -halfSize.z),
                position + new Vector3(-halfSize.x, halfSize.y, -halfSize.z),
                position + new Vector3(-halfSize.x, -halfSize.y, halfSize.z),
                position + new Vector3(halfSize.x, -halfSize.y, halfSize.z),
                position + new Vector3(halfSize.x, halfSize.y, halfSize.z),
                position + new Vector3(-halfSize.x, halfSize.y, halfSize.z)
            };

            // Define cube edges
            int[,] edges = new int[12, 2]
            {
                { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
                { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
                { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
            };

            // Draw cube edges
            for (int i = 0; i < edges.GetLength(0); i++)
            {
                Debug.DrawLine(vertices[edges[i, 0]], vertices[edges[i, 1]], color, duration);
            }
        }

        /// <summary>
        /// Draws a wireframe cylinder using Debug.DrawLine with duration
        /// </summary>
        public static void DrawWireCylinder(Vector3 position, float radius, float height, Color color, float duration = 0, int segments = 16)
        {
            float step = Mathf.PI * 2.0f / segments;
            float halfHeight = height * 0.5f;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = step * i;
                float angle2 = step * (i + 1);

                Vector3 offset1 = new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
                Vector3 offset2 = new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

                // Draw top and bottom circles
                Debug.DrawLine(position + offset1 + Vector3.up * halfHeight, position + offset2 + Vector3.up * halfHeight, color, duration);
                Debug.DrawLine(position + offset1 - Vector3.up * halfHeight, position + offset2 - Vector3.up * halfHeight, color, duration);

                // Connect top and bottom circles
                Debug.DrawLine(position + offset1 + Vector3.up * halfHeight, position + offset1 - Vector3.up * halfHeight, color, duration);
            }
        }
    }
}
