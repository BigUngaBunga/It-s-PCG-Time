using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoise
{
    private float[,] heightMap;
    private int height, width;
    public PerlinNoise()
    {

    }

    public float[,] Generate(int height, int width, int seed)
    {
        this.height = height;
        this.width = width;
        heightMap = new float[height, width];
        Random.InitState(seed);


        return heightMap;
    }
}
