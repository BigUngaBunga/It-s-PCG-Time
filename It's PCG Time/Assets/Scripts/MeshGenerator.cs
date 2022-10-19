using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;
using static MeshGenerator;

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
        var vertices = GetVertecies(heightMap, targetSize, out Vector2[] uv);
        mesh.SetVertices(vertices);
        var triangles = GetTriangles(heightMap, vertices);
        mesh.SetTriangles(ConvertToIndices(triangles), 0);
        meshFilter.mesh = mesh;
        mesh.uv = uv;
    }

    public void CreateLayeredMesh(float[,] heightMap, Vector2 targetSize, params float[] layerHeights)
    {//TODO organisera så att indexering sker för varje submesh. Gör submeshklass som kan hantera allt.
        mesh = new Mesh();
        var vertices = GetVertecies(heightMap, targetSize, out Vector2[] uv);
        Debug.Log("Number of verecies: " + vertices.Length);

        mesh.SetVertices(vertices);
        var triangles = GetTriangles(heightMap, vertices);
        var triangleLayers = ConvertToLayeredTriangles(triangles, layerHeights);

        mesh.subMeshCount = triangleLayers.Length;

        for (int i = 0; i < triangleLayers.Length; i++)
            mesh.SetTriangles(ConvertToIndices(triangleLayers[i]), i);
        mesh.SetSubMeshes(GetSubMeshes(triangleLayers));
        
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

    private List<Triangle> GetTriangles(float[,] heightMap, Vector3[] vertecies)
    {
        List<Quad> quads = new List<Quad>();
        Point size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        for (int i = 0; i < vertecies.Length; i++)
        {
            if (i % size.Y == size.Y - 1 || i >= heightMap.Length - size.Y - 1)
                continue;

            quads.Add(new Quad(newVertex(i), newVertex(i + 1), newVertex(i + size.Y), newVertex(i + size.Y + 1)));
        }

        List<Triangle> triangles = new List<Triangle>();
        foreach (var quad in quads)
            triangles.AddRange(quad.GetTriangles());

        Debug.Log("Number of triangles: " + triangles.Count);
        return triangles;

        Vertex newVertex(int i) => new Vertex(vertecies[i].y, i);
    }

    private List<int> ConvertToIndices(List<Triangle> triangles)
    {
        List<int> indices = new List<int>();
        foreach (var triangle in triangles)
            indices.AddRange(triangle.indicies);
        return indices;
    }

    private List<Triangle>[] ConvertToLayeredTriangles(List<Triangle> triangles, float[] layerHeights)
    {
        int length = layerHeights.Length;
        List<Triangle>[] triangleLayers = new List<Triangle>[length];

        for (int i = 0; i < length; i++)
        {
            triangleLayers[i] = new List<Triangle>();
        }

        for (int i = 0; i < triangles.Count; i++)
            triangleLayers[GetLayerIndex(triangles[i])].Add(triangles[i]);
        return triangleLayers;

        int GetLayerIndex(Triangle triangle)
        {
            for (int i = 0; i < length; i++)
                if (triangle.Height >= layerHeights[i])
                    return i;
            return length - 1;
        }
    }

    private SubMeshDescriptor[] GetSubMeshes(List<Triangle>[] triangleLayers)
    {
        SubMeshDescriptor[] descriptors = new SubMeshDescriptor[triangleLayers.Length];
        //HashSet<int> indices = new HashSet<int>();
        List<int> indices = new List<int>();
        for (int i = 0; i < triangleLayers.Length; i++)
        {
            int firstIndex, indexCount;
            Debug.Log("Layer: " + i + " Layer size: " + triangleLayers[i].Count);
            indices.Clear();
            foreach (var triangle in triangleLayers[i])
            {
                indices.Add(triangle.indicies[0]);
                indices.Add(triangle.indicies[1]);
                indices.Add(triangle.indicies[2]);
            }
            
            if (triangleLayers[i].Count > 0)
            {
                firstIndex = triangleLayers[i][0].FirstIndex;
                indexCount = indices.Count;
            }
            else
            {
                firstIndex=0;
                indexCount = 0;
            }
             

            Debug.Log("Index count: " + indexCount + " Start index: " + firstIndex + " Combined length: " + (indexCount + firstIndex));
            descriptors[i] = new SubMeshDescriptor(firstIndex, indexCount);
        }
        return descriptors;
    }

    public struct Quad
    {
        private readonly Triangle t1, t2;

        public Quad(Vertex upperLeft, Vertex upperRight, Vertex lowerLeft, Vertex lowerRight)
        {
            t1 = new Triangle(lowerLeft, upperLeft, upperRight);
            t2 = new Triangle(lowerLeft, upperRight, lowerRight);
        }

        public Triangle[] GetTriangles() => new Triangle[] { t1, t2 };
    }

    public struct Triangle
    {
        private Vertex a, b, c;
        public int[] indicies;
        public int FirstIndex => Mathf.Min(a.index, b.index, c.index);
        public int LastIndex => Mathf.Max(a.index, b.index, c.index);
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
