using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    private Mesh mesh;
    private MeshFilter meshFilter;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    public void CreateMesh(float[,] heightMap, Vector2 targetSize)
    {
        mesh = new Mesh();
        mesh.SetVertices(GetVertecies(heightMap, targetSize, out Vector2[] uv));
        var triangles = GetTriangles(heightMap);
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;
        mesh.uv = uv;
    }

    public void CreateLayeredMesh(float[,] heightMap, Vector2 targetSize, params float[] layerHeights)
    {
        mesh = new Mesh();
        mesh.SetVertices(GetVertecies(heightMap, targetSize, out Vector2[] uv));
        var triangles = GetTriangles(heightMap);
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;
        mesh.uv = uv;
    }

    private Vector3[] GetVertecies(float[,] heightMap, Vector2 targetSize, out Vector2[] uv)
    {
        List<Vector3> vertecies = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        Vector2 scale;
        Point size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        scale = new Vector2(targetSize.x / size.X, targetSize.y / size.Y);

        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
            {
                var vertex = new Vector3((x - size.X / 2f) * scale.x, heightMap[x, y], (y - size.Y / 2f) * scale.y);
                vertecies.Add(vertex);
                uvs.Add(new Vector2(vertex.x, vertex.z));
            }

        uv = uvs.ToArray();

        return vertecies.ToArray();
    }

    private int[] GetTriangles(float[,] heightMap)
    {
        List<Quad> quads = new List<Quad>();
        Point size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        for (int i = 0; i < heightMap.Length; i++)
        {
            if (i % size.Y == size.Y - 1 || i >= heightMap.Length - size.Y - 1)
                continue;

            quads.Add(new Quad(i, i + 1, i + size.Y, i + size.Y + 1));
        }

        List<int> triangles = new List<int>();
        foreach (var quad in quads)
            triangles.AddRange(quad.GetTriangles());
        return triangles.ToArray();
    }

    public struct Quad
    {
        private readonly int upperLeft, upperRight, lowerLeft, lowerRight;

        public Quad(int upperLeft, int upperRight, int lowerLeft, int lowerRight)
        {
            this.upperLeft = upperLeft;
            this.upperRight = upperRight;
            this.lowerLeft = lowerLeft;
            this.lowerRight = lowerRight;
        }

        public int[] GetTriangles()
        {
            int[] triangles = new int[6];
            //
            triangles[0] = lowerLeft;
            triangles[1] = upperLeft;
            triangles[2] = upperRight;
            //
            triangles[3] = lowerLeft;
            triangles[4] = upperRight;
            triangles[5] = lowerRight;
            return triangles;
        }
    }

    public struct Triangle
    {
        private Vertex a, b, c;
        public int[] indicies;
        public float Height => Mathf.Max(a.height, b.height, c.height);
        public Triangle(Vertex a, Vertex b, Vertex c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            indicies = new int[] { a.index, b.index, c.index};
        }
    }

    public struct Vertex
    {
        public float height;
        public int index;

        public Vertex(float height, int index)
        {
            this.height = height;
            this.index = index;
        }
    }
}
