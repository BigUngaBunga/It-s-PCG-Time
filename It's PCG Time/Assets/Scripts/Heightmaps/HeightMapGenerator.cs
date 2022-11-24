using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using UnityEngine;

public class HeightMapGenerator : MonoBehaviour
{
    enum GenerationMethod { DiamondSquare, PerlinNoise, Debug}

    [Header("Generation")]
    [SerializeField] private GenerationMethod method;
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private string seed;
    [Range(0.1f, 5f)]
    [SerializeField] private float amplitude;

    [Header("Mesh")]
    [SerializeField] private Vector2 meshSize;

    [Header("Diamond Square")]
    [Range(0, 0.25f)]
    [SerializeField] private float roughness;
    [Range(1,6)]
    [SerializeField] private int size;

    [Header("Perlin noise")]
    [Min(1)]
    [SerializeField] private int width;
    [Min(1)]
    [SerializeField] private int height;

    private float[,] heightMap;
    private DiamondSquareAlgorithm diamondSquare;
    private PerlinNoise perlinNoise;
    private Interpolator interpolator;
    private MeshGenerator meshGenerator;
    private Vector2 adjustedMeshSize;

    private void Awake()
    {
        meshGenerator = gameObject.AddComponent<MeshGenerator>();
    }

    private void Start()
    {
        interpolator = GetComponent<Interpolator>();
        diamondSquare = new DiamondSquareAlgorithm();
        perlinNoise = new PerlinNoise();
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
                heightMap = diamondSquare.Generate(size, seedValue, amplitude, roughness);
                adjustedMeshSize = meshSize * heightMap.GetLength(0);
                return interpolator.Interpolate(heightMap);
            case GenerationMethod.PerlinNoise:
                adjustedMeshSize = new Vector2(meshSize.x * width, meshSize.y * height);
                heightMap = perlinNoise.Generate(width, height, seedValue, amplitude);
                return interpolator.Interpolate(heightMap);
            case GenerationMethod.Debug:
            default:
                return new float[3, 3];
        }

    }

    private void GenerateMap()
    {
        heightMap = GenerateHeightMap();
        meshGenerator.CreateMesh(heightMap, adjustedMeshSize);
    }
}
