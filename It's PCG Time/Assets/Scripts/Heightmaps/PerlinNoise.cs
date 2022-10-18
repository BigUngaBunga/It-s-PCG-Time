using System.Text;
using UnityEngine;

public class PerlinNoise
{
    private Vector2[,] gradientVectors;
    private float[,] heightMap;
    private float amplitude;
    private int height, width;
    private float RandomAngle => Random.Range(-1f, 1f);
    private Vector2 RandomVector => new Vector2(RandomAngle, RandomAngle).normalized;
    public PerlinNoise()
    {

    }

    public float[,] Generate(int width, int height, int seed, float amplitude)
    {
        this.width = width;
        this.height = height;
        this.amplitude = amplitude;
        heightMap = new float[width, height];
        Random.InitState(seed);
        GetRandomGradientVectors();
        ConvertGradientToHeightMap();
        return heightMap;
    }

    private void GetRandomGradientVectors()
    {
        gradientVectors = new Vector2[width, height];
        for (int x = 0; x < gradientVectors.GetLength(0); x++)
            for (int y = 0; y < gradientVectors.GetLength(1); y++)
                gradientVectors[x, y] = RandomVector;
    }

    private void ConvertGradientToHeightMap()
    {
        int heightMapWidth = heightMap.GetLength(0);
        int heightMapHeight = heightMap.GetLength(1);
        float positionY, positionX;
        for (int x = 0; x < heightMap.GetLength(0); x++)
        {
            positionX = GetPosition(width, heightMapWidth, x);
            for (int y = 0; y < heightMap.GetLength(1); y++)
            {
                positionY = GetPosition(height, heightMapHeight, y);
                heightMap[x,y] = CalculateHeight(GetCornersAt((int)positionX, (int)positionY), positionX, positionY);
            }
        }

        static float GetPosition(int sizeOfTarget, int sizeOfSource, int sourceIndex)
        {
            float index = sourceIndex;
            index = index / sizeOfSource * (sizeOfTarget - 1);
            return index;
        }
    }

    private Vector2[,] GetCornersAt(int x, int y)
    {
        Vector2[,] corners = new Vector2[2,2];
        if (x >= 0 && x <= gradientVectors.GetLength(0) && y >= 0 && y <= gradientVectors.GetLength(1))
        {
            corners[0, 0] = gradientVectors[x, y];
            corners[1, 0] = gradientVectors[x + 1, y];
            corners[0, 1] = gradientVectors[x, y + 1];
            corners[1, 1] = gradientVectors[x + 1, y + 1];
        }
        else
            corners = null;

        return corners;
    }

    private float CalculateHeight(Vector2[,] corners, float xPosition, float yPosition)
    {
        if (corners.GetLength(0) < 2 && corners.GetLength(1) < 2)
            return float.NaN;

        float height = 0;

        Vector2 position = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));

        //Vector2 position = new Vector2(xPosition - (int)xPosition, yPosition - (int)yPosition);
        for (int x = 0; x < corners.GetLength(0); x++)
            for (int y = 0; y < corners.GetLength(1); y++)
                height += Vector2.Dot(position - corners[x, y], corners[x, y]) * amplitude;
        return height;
    }
}
