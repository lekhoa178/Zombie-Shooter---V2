using System.Collections.Generic;
using UnityEngine;

public class MeshSlicer : MonoBehaviour
{
    public MeshFilter meshFilter; // Reference to the mesh filter of the large mesh
    public float sliceWidth = 1.0f; // Width of each slice (n)
    public float sliceHeight = 1.0f; // Height of each slice (m)

    void Start()
    {
        if (meshFilter == null) return;
        Mesh originalMesh = meshFilter.mesh;
        SliceMesh(originalMesh);
    }

    void SliceMesh(Mesh originalMesh)
    {
        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        Bounds bounds = originalMesh.bounds;

        float xMin = bounds.min.x;
        float yMin = bounds.min.y;
        float xMax = bounds.max.x;
        float yMax = bounds.max.y;

        // Define grid slicing dimensions
        int xSlices = Mathf.CeilToInt((xMax - xMin) / sliceWidth);
        int ySlices = Mathf.CeilToInt((yMax - yMin) / sliceHeight);

        for (int i = 0; i < xSlices; i++)
        {
            for (int j = 0; j < ySlices; j++)
            {
                float x0 = xMin + i * sliceWidth;
                float x1 = x0 + sliceWidth;
                float y0 = yMin + j * sliceHeight;
                float y1 = y0 + sliceHeight;

                // Slice triangles intersecting this grid
                Mesh slicedMesh = ExtractGridMesh(vertices, triangles, x0, x1, y0, y1);
                CreateSubMesh(slicedMesh, i, j);
            }
        }
    }

    Mesh ExtractGridMesh(Vector3[] vertices, int[] triangles, float x0, float x1, float y0, float y1)
    {
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            // Check if triangle intersects the grid cell
            if (IsTriangleInsideGrid(v0, v1, v2, x0, x1, y0, y1))
            {
                AddClippedTriangle(v0, v1, v2, x0, x1, y0, y1, newVertices, newTriangles);
            }
        }

        Mesh gridMesh = new Mesh();
        gridMesh.vertices = newVertices.ToArray();
        gridMesh.triangles = newTriangles.ToArray();
        gridMesh.RecalculateNormals();
        return gridMesh;
    }

    bool IsTriangleInsideGrid(Vector3 v0, Vector3 v1, Vector3 v2, float x0, float x1, float y0, float y1)
    {
        return (IsPointInsideGrid(v0, x0, x1, y0, y1) ||
                IsPointInsideGrid(v1, x0, x1, y0, y1) ||
                IsPointInsideGrid(v2, x0, x1, y0, y1));
    }

    bool IsPointInsideGrid(Vector3 point, float x0, float x1, float y0, float y1)
    {
        return point.x >= x0 && point.x <= x1 && point.y >= y0 && point.y <= y1;
    }

    void AddClippedTriangle(Vector3 v0, Vector3 v1, Vector3 v2, float x0, float x1, float y0, float y1,
        List<Vector3> newVertices, List<int> newTriangles)
    {
        // Basic example: add triangles that are inside the grid directly
        int baseIndex = newVertices.Count;
        newVertices.Add(v0);
        newVertices.Add(v1);
        newVertices.Add(v2);

        newTriangles.Add(baseIndex);
        newTriangles.Add(baseIndex + 1);
        newTriangles.Add(baseIndex + 2);
    }

    void CreateSubMesh(Mesh mesh, int i, int j)
    {
        GameObject subMeshObj = new GameObject("SubMesh_" + i + "_" + j);
        subMeshObj.transform.position = transform.position;
        subMeshObj.transform.parent = this.transform;

        MeshFilter mf = subMeshObj.AddComponent<MeshFilter>();
        MeshRenderer mr = subMeshObj.AddComponent<MeshRenderer>();
        mf.mesh = mesh;
        mr.material = new Material(Shader.Find("Standard"));
    }
}
