using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using UnityEngine;

public class HeightMapGenerator : MonoBehaviour
{
    enum GenerationMethod { DiamondSquare, PerlinNoise}
    [Header("General parameters")]
    [SerializeField] private GenerationMethod method;
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private string seed;
    [SerializeField] private float amplitude;

    [Header("Diamond Square parameters")]
    [Range(0, 0.25f)]
    [SerializeField] private float roughness;
    [Range(0,10)]
    [SerializeField] private int size;

    [Header("Perlin noise parameters")]
    [SerializeField] private int width, height;

    private MeshFilter meshFilter;
    private Mesh mesh;
    private float[,] heightMap;
    private DiamondSquareAlgorithm diamondSquare;
    private PerlinNoise perlinNoise;

    public float[,] GenerateHeightMap()
    {
        if (randomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue).ToString();

        if(!int.TryParse(seed, out int seedValue))
            seedValue = seed.GetHashCode();

        return method switch
        {
            GenerationMethod.PerlinNoise => perlinNoise.Generate(height, width, seedValue),
            GenerationMethod.DiamondSquare => diamondSquare.Generate(size, seedValue, amplitude, roughness),
            _ => new float[1, 1],
        };
    }

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        diamondSquare = new DiamondSquareAlgorithm();
        perlinNoise = new PerlinNoise();
        GenerateMap();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GenerateMap();
    }

    //TODO skapa en UV-karta
    //TODO tillämpa s-kurvor istället för raka gångar.
    private void GenerateMap()
    {
        heightMap = GenerateHeightMap();
        CreateMesh(heightMap);
    }

    private void CreateMesh(float[,] heightMap)
    {
        mesh = new Mesh();
        mesh.SetVertices(GetVertecies(heightMap));
        var triangles = GetTriangles(heightMap);
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;
    }

    private Vector3[] GetVertecies(float[,] heightMap)
    {
        List<Vector3> vertecies = new List<Vector3>();
        Point size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
                vertecies.Add(new Vector3(x - size.X / 2f, heightMap[x, y], y - size.Y / 2f));

        return vertecies.ToArray();
    }

    private int[] GetTriangles(float[,] heightMap)
    {
        List<Quad> quads = new List<Quad>();
        Point size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        for (int i = 0; i < heightMap.Length; i++)
        {
            if (i % size.X == size.X - 1 || i >= heightMap.Length - heightMap.GetLength(1))
                continue;

            quads.Add(new Quad(i, i + 1, i + heightMap.GetLength(1), i + heightMap.GetLength(1) + 1));
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
}
