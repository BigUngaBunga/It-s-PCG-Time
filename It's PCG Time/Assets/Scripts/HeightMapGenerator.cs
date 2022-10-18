using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using UnityEngine;

public class HeightMapGenerator : MonoBehaviour
{
    enum GenerationMethod { DiamondSquare, PerlinNoise, ReversePerlin, Debug}
    public enum InterpolationMethod { Bilinear, Bicubic, Cosine, None}

    [Header("Generation")]
    [SerializeField] private GenerationMethod method;
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private string seed;
    [SerializeField] private float amplitude;

    [Header("Interpolation")]
    [SerializeField] private InterpolationMethod interpolation;
    [Range(1, 10)]
    [SerializeField] private int detailFactor = 1;

    [Header("Mesh")]
    [SerializeField] private Vector2 meshSize;


    [Header("Diamond Square")]
    [Range(0, 0.25f)]
    [SerializeField] private float roughness;
    [Range(1,10)]
    [SerializeField] private int size;

    [Header("Perlin noise")]
    [Min(1)]
    [SerializeField] private int width;
    [Min(1)]
    [SerializeField] private int height;

    private MeshFilter meshFilter;
    private Mesh mesh;
    private float[,] heightMap;
    private DiamondSquareAlgorithm diamondSquare;
    private PerlinNoise perlinNoise;
    private Interpolator interpolator;
    private Vector2 adjustedMeshSize;

    private int HeightMapWidth => heightMap.GetLength(0);
    private int HeightMapHeight => heightMap.GetLength(1);

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        diamondSquare = new DiamondSquareAlgorithm();
        perlinNoise = new PerlinNoise();
        interpolator = new Interpolator();
        GenerateMap();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GenerateMap();
    }


    public float[,] GenerateHeightMap()
    {
        if (randomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue).ToString();

        if (!int.TryParse(seed, out int seedValue))
            seedValue = seed.GetHashCode();


        switch (method)
        {
            case GenerationMethod.DiamondSquare:
                adjustedMeshSize = meshSize * size;
                heightMap = diamondSquare.Generate(size, seedValue, amplitude, roughness);
                return interpolator.Interpolate(interpolation, detailFactor, heightMap);
            case GenerationMethod.PerlinNoise:
                adjustedMeshSize = new Vector2(meshSize.x * width, meshSize.y * height);
                heightMap = perlinNoise.Generate(width, height, seedValue, amplitude);
                return interpolator.Interpolate(interpolation, detailFactor, heightMap);
            case GenerationMethod.Debug:
            default:
                return new float[3, 3];
        }

    }

    private void GenerateMap()
    {
        heightMap = GenerateHeightMap();
        CreateMesh(heightMap);
    }

    private void CreateMesh(float[,] heightMap)
    {
        mesh = new Mesh();
        mesh.SetVertices(GetVertecies(heightMap, out Vector2[] uv));
        var triangles = GetTriangles(heightMap);
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;
        mesh.uv = uv;
    }

    private Vector3[] GetVertecies(float[,] heightMap, out Vector2[] uv)
    {
        List<Vector3> vertecies = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        Vector2 scale;
        Point size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        scale = new Vector2(adjustedMeshSize.x / size.X, adjustedMeshSize.y / size.Y);

        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
            {
                var vertex = new Vector3((x - size.X / 2f) * scale.x , heightMap[x, y], (y - size.Y ) * scale.y);
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
}
