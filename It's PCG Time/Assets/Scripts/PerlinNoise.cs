using UnityEngine;

public class PerlinNoise
{
    private Vector2[,] gradientVectors;
    private float[,] heightMap;
    private int height, width;
    private int targetHeight, targetWidth;
    int detailFactor;
    bool useDetailFactor;
    private float RandomAngle => Random.Range(-Mathf.PI, Mathf.PI);
    private Vector2 RandomVector => new Vector2(RandomAngle, RandomAngle).normalized;
    public PerlinNoise()
    {

    }

    public void SetHeightMapTarget(float targetWidth, float targetHeight, int detailFactor, bool useDetailFactor)
    {
        this.targetHeight = (int)targetHeight;
        this.targetWidth = (int)targetWidth;
        this.detailFactor = detailFactor;
        this.useDetailFactor = useDetailFactor;
    }

    public float[,] Generate(int height, int width, int seed)
    {
        this.height = height;
        this.width = width;
        if (useDetailFactor)
            heightMap = new float[width * detailFactor, height * detailFactor];
        else
            heightMap = new float[Mathf.Max(width, targetWidth), Mathf.Max(height, targetHeight)];
        Random.InitState(seed);
        RandomizeGradientVectors();
        ConvertGradientToHeightMap();
        return heightMap;
    }

    private void RandomizeGradientVectors()
    {
        gradientVectors = new Vector2[height, width];
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
            index /= (sizeOfSource - 1) * sizeOfTarget;
            return index;
        }
    }

    private Vector2[,] GetCornersAt(int x, int y)
    {
        Vector2[,] corners = new Vector2[2,2];
        if (x >= 0 && x < gradientVectors.GetLength(0) && y >= 0 && y < gradientVectors.GetLength(1))
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
        Vector2 position = new Vector2(xPosition, yPosition);
        for (int x = 0; x < corners.GetLength(0); x++)
            for (int y = 0; y < corners.GetLength(1); y++)
            {
                height += Vector2.Dot(position - corners[x, y], corners[x, y]);
            }
        return height;
    }
}
