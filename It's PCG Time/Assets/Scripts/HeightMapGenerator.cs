using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class HeightMapGenerator : MonoBehaviour
{
    enum GenerationMethod { DiamondSquare, PerlinNoise, Debug}
    enum InterpolationMethod { Bilinear, Bicubic}
    [Header("Interpolation")]
    [SerializeField] private InterpolationMethod interpolation;
    [Range(1, 10)]
    [SerializeField] private int interpolationDetail = 1;
    [SerializeField] private bool interpolateFourCorners;
    [SerializeField] private bool keepSizeConstant;
    [SerializeField] private float mapWidth, mapHeight;

    [Header("Generation")]
    [SerializeField] private GenerationMethod generation;
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private string seed;
    [SerializeField] private float amplitude;

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

    private int HeightMapWidth => heightMap.GetLength(0);
    private int HeightMapHeight => heightMap.GetLength(1);

    public float[,] GenerateHeightMap()
    {
        if (randomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue).ToString();

        if(!int.TryParse(seed, out int seedValue))
            seedValue = seed.GetHashCode();

        return generation switch
        {
            GenerationMethod.PerlinNoise => perlinNoise.Generate(height, width, seedValue),
            GenerationMethod.DiamondSquare => diamondSquare.Generate(size, seedValue, amplitude, roughness),
            _ => new float[3, 3],
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

    //TODO tillämpa s-kurvor istället för raka gångar.
    private void GenerateMap()
    {
        heightMap = GenerateHeightMap();
        CreateMesh(InterpolateHeightMap());
    }


    private float[,] InterpolateHeightMap()
    {
        float[,] interpolation = new float[heightMap.GetLength(0) * interpolationDetail, heightMap.GetLength(1) * interpolationDetail];

        int width, height;
        float dX, dY;

        for (int x = 0; x < interpolation.GetLength(0); x++)
        {
            width = x / interpolationDetail;
            dX = (float)x / interpolationDetail - width;
            for (int y = 0; y < interpolation.GetLength(1); y++)
            {
                height = y / interpolationDetail;
                dY = (float)y / interpolationDetail - height;
                if (this.interpolation == InterpolationMethod.Bicubic)
                {
                    dX = -2f * Mathf.Pow(dX, 3) + 3 * Mathf.Pow(dX, 2);
                    dY = -2f * Mathf.Pow(dY, 3) + 3 * Mathf.Pow(dY, 2);
                }
                interpolation[y, x] = GetInterpolation(width, height, dX, dY);
            }
        }

        return interpolation;

        
    }

    float GetInterpolation(int width, int height, float dX, float dY)
    {
        int nextWidth = (width + 1) % heightMap.GetLength(0);
        int nextHeight = (height + 1) % heightMap.GetLength(1);

        float value = (heightMap[width, height] * (1f - dX) + heightMap[nextWidth, height] * dX);
        value += (heightMap[width, height] * (1f - dY) + heightMap[width, nextHeight] * dY);

        if (interpolateFourCorners)
        {
            value = (heightMap[width, height] * (1f - dX) + heightMap[nextWidth, height] * dX) * (1f - dY);
            value += (heightMap[width, nextHeight] * (1f - dX) + heightMap[nextWidth, nextHeight] * dX) * dY;
            value += (heightMap[width, height] * (1f - dY) + heightMap[width, nextHeight] * dY) * (1f - dX);
            value += (heightMap[nextWidth, height] * (1f - dY) + heightMap[nextWidth, nextHeight] * dY) * dX;
        }
        
        return value;
    }

    private void CreateMesh(float[,] heightMap)
    {
        Debug.Log("Creating mesh");
        mesh = new Mesh();
        mesh.SetVertices(GetVertecies(heightMap, out Vector2[] uv));
        Debug.Log("Generating triangles");
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
        if (keepSizeConstant)
             scale = new Vector2(HeightMapWidth / (float)size.X, HeightMapHeight / (float)size.Y);
        else
            scale = new Vector2(mapWidth / size.X, mapHeight / size.Y);

        Debug.Log("Size of the height map: " + size + " scale of the map: " + scale);
        Debug.Log("Original widht: " + HeightMapWidth + " Original height: " + HeightMapHeight);
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
