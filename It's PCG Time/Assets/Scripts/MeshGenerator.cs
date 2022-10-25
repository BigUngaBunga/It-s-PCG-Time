using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static MeshGenerator;

public class MeshGenerator : MonoBehaviour
{
    private Mesh mesh;
    private MeshFilter meshFilter;
    private bool debug;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Write(string text)
    {
        if (debug)
            Debug.Log(text);
    }

    public void CreateMesh(float[,] heightMap, Vector2 targetSize)
    {
        mesh = new Mesh();
        var vertices = GetVertecies(heightMap, targetSize);
        mesh.SetVertices(vertices);
        var triangles = GetTriangles(heightMap, vertices);
        mesh.SetTriangles(ConvertToIndices(triangles), 0);
        meshFilter.mesh = mesh;
        mesh.uv = GetUvs(vertices, heightMap, targetSize);
    }

    public void CreateLayeredMesh(float[,] heightMap, Vector2 targetSize, params float[] layerHeights)
    {//TODO organisera så att indexering sker för varje submesh. Gör submeshklass som kan hantera allt.
        mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        var rawVertices = GetVertecies(heightMap, targetSize);
        Write("Number of vertecies: " + rawVertices.Count);

        var triangles = GetTriangles(heightMap, rawVertices);
        var triangleLayers = ConvertToLayeredTriangles(triangles, layerHeights);
        var vertices = GetLayeredVertecies(ref triangleLayers);
        var subMeshes = GetSubMeshes(triangleLayers, out var indices);
        mesh.SetVertices(vertices);
        mesh.subMeshCount = subMeshes.Length;

        for (int i = 0; i < triangleLayers.Length; i++)
        {
            mesh.SetIndices(indices[i], subMeshes[i].topology, i);
            mesh.SetSubMesh(i, subMeshes[i]);
            mesh.SetTriangles(ConvertToIndices(triangleLayers[i]), i);
        }

        mesh.uv = GetUvs(vertices, heightMap, targetSize);
        meshFilter.mesh = mesh;
    }

    private List<Vector3> GetVertecies(float[,] heightMap, Vector2 targetSize)
    {
        List<Vector3> vertecies = new List<Vector3>();
        Vector2 scale;
        Point size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        scale = new Vector2(targetSize.x / size.X, targetSize.y / size.Y);

        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
            {
                var vertex = new Vector3((x - size.X / 2f) * scale.x, heightMap[x, y], (y - size.Y / 2f) * scale.y);
                vertecies.Add(vertex);
            }
        return vertecies;
    }

    private Vector2[] GetUvs(List<Vector3> vertecies, float[,] heightMap, Vector2 targetSize)
    {
        List<Vector2> uvs = new List<Vector2>();
        //Point size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        //Vector2 scale = new Vector2(targetSize.x / size.X, targetSize.y / size.Y);

        //for (int x = 0; x < size.X; x++)
        //    for (int y = 0; y < size.Y; y++)
        //    {
        //        var vertex = new Vector2((x - size.X / 2f) * scale.x, (y - size.Y / 2f) * scale.y);
        //        uvs.Add(new Vector2(vertex.x, vertex.y));
        //    }

        for (int i = 0; i < vertecies.Count; i++)
        {
            var vertex = vertecies[i];
            uvs.Add(new Vector2(vertex.x, vertex.y));
        }

        return uvs.ToArray();
    }

    private List<Vector3> GetLayeredVertecies(ref List<Triangle>[] triangleLayers)
    {
        List<Vector3> vertecies = new List<Vector3>();
        foreach (var triangles in triangleLayers)
            for (int j = 0; j < triangles.Count; j++)
                triangles[j] = IndexTriangle(triangles[j]);

        return vertecies;
        
        Triangle IndexTriangle(Triangle triangle)
        {
            vertecies.Add(triangle.a.position);
            vertecies.Add(triangle.b.position);
            vertecies.Add(triangle.c.position);
            triangle.a.UpdateIndex(vertecies.Count - 3);
            triangle.b.UpdateIndex(vertecies.Count - 2);
            triangle.c.UpdateIndex(vertecies.Count - 1);
            return triangle;
        }
    }

    private List<Triangle> GetTriangles(float[,] heightMap, List<Vector3> vertecies)
    {
        List<Quad> quads = new List<Quad>();
        Point size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        for (int i = 0; i < vertecies.Count; i++)
        {
            if (i % size.Y == size.Y - 1 || i >= heightMap.Length - size.Y - 1)
                continue;

            quads.Add(new Quad(newVertex(i), newVertex(i + 1), newVertex(i + size.Y), newVertex(i + size.Y + 1)));
        }

        List<Triangle> triangles = new List<Triangle>();
        foreach (var quad in quads)
            triangles.AddRange(quad.GetTriangles());

        Write("Number of triangles: " + triangles.Count);
        return triangles;

        Vertex newVertex(int i) => new Vertex(vertecies[i], i);
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
        List<List<Triangle>> triangleLayers = new List<List<Triangle>>();

        for (int i = 0; i < length; i++)
            triangleLayers.Add(new List<Triangle>());

        for (int i = 0; i < triangles.Count; i++)
            triangleLayers[GetLayerIndex(triangles[i])].Add(triangles[i]);

        return triangleLayers.ToArray();

        int GetLayerIndex(Triangle triangle)
        {
            for (int i = 0; i < length; i++)
            {
                if (i == length - 1 && i > 0 && triangle.Height > layerHeights[i])
                    return i - 1;
                if (triangle.Height >= layerHeights[i])
                    return i;
            }
                
            return length - 1;
        }
    }

    private SubMeshDescriptor[] GetSubMeshes(List<Triangle>[] triangleLayers, out List<List<int>> indices)
    {
        SubMeshDescriptor[] descriptors = new SubMeshDescriptor[triangleLayers.Length];
        indices = new List<List<int>>();
        for (int i = 0; i < triangleLayers.Length; i++)
        {
            int firstIndex, indexCount;
            indices.Add(new List<int>());
            Write("Layer: " + i + " Layer size: " + triangleLayers[i].Count);
            foreach (var triangle in triangleLayers[i])
            {
                indices[i].Add(triangle.indicies[0]);
                indices[i].Add(triangle.indicies[1]);
                indices[i].Add(triangle.indicies[2]);
            }
            
            if (triangleLayers[i].Count > 0)
            {
                firstIndex = triangleLayers[i][0].FirstIndex;
                indexCount = indices[i].Count;
            }
            else
            {
                firstIndex = 0;
                indexCount = 0;
            }


            Write("Index count: " + indexCount + " Start index: " + firstIndex + " Combined length: " + (indexCount + firstIndex));
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
        public Vertex a, b, c;
        public int[] indicies => new int[] { a.index, b.index, c.index};
        public int FirstIndex => Mathf.Min(a.index, b.index, c.index);
        public int LastIndex => Mathf.Max(a.index, b.index, c.index);
        public float Height => Mathf.Max(a.Height, b.Height, c.Height);
        public Triangle(Vertex a, Vertex b, Vertex c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }

    public struct Vertex
    {
        public Vector3 position;
        public float Height => position.y;
        public int index;

        public Vertex(Vector3 position, int index)
        {
            this.position = position;
            this.index = index;
        }

        public void UpdateIndex(int newIndex) => index = newIndex;
    }
}
