using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
        diamondSquare = new DiamondSquareAlgorithm();
        perlinNoise = new PerlinNoise();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateHeightMap();
        }
    }
}
