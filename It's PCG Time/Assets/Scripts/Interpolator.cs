using UnityEngine;
using Method = HeightMapGenerator.InterpolationMethod;
using Detail = HeightMapGenerator.DetailType;

public class Interpolator : MonoBehaviour
{
    private Method method;
    private Detail detail;
    private float[,] heightMap;
    private int interpolationDetail;
    private float width, height;

    public Interpolator()
    {

    }

    public void PrepareInterpolation(int interpolationDetail, float width, float height)
    {
        this.interpolationDetail = interpolationDetail;
        this.width = width;
        this.height = height;
    }

    public float[,] Interpolate(Method method, Detail detail, float[,] heightMap)
    {
        this.method = method;
        this.heightMap = heightMap;
        this.detail = detail;
        return InterpolateHeightMap();
    }

    private float[,] InterpolateHeightMap()
    {
        float[,] interpolation;
        if (detail == Detail.Factor)
            interpolation = new float[heightMap.GetLength(0) * interpolationDetail, heightMap.GetLength(1) * interpolationDetail];
        else if (detail == Detail.Map)
            interpolation = new float[Mathf.Max(heightMap.GetLength(0), (int)width), Mathf.Max(heightMap.GetLength(1), (int)height)];
        else
            interpolation = new float[heightMap.GetLength(0), heightMap.GetLength(1)];


        int interpolatedX, interpolatedY;
        float dWidth, dHeight;
        float dX, dY;

        if (method == Method.None)
            return heightMap;

        dWidth = interpolation.GetLength(0) / (float)(heightMap.GetLength(0) - 1);
        dHeight = interpolation.GetLength(1) / (float)(heightMap.GetLength(1) - 1);

        for (int x = 0; x < interpolation.GetLength(0); x++)
        {
            interpolatedX = GetWidth(x);
            dX = x / dWidth - interpolatedX;
            for (int y = 0; y < interpolation.GetLength(1); y++)
            {
                interpolatedY = GetHeight(y);
                dY = y / dHeight - interpolatedY;
                interpolation[x, y] = GetInterpolation(interpolatedX, interpolatedY, dX, dY);

            }
        }
        return interpolation;

        int GetWidth(int x) => (int)(x / dWidth);
        int GetHeight(int y) => (int)(y / dHeight);
    }

    float GetInterpolation(int x, int y, float dX, float dY)
    {
        float value = InterpolateX(x, y, dX, dY);
        value += InterpolateY(x, y, dX, dY);
        value /= 2f;
        return value;
    }

    float InterpolateX(int x, int y, float dX, float dY)
    {
        float value = 0;
        if (x <= 0 || x + 2 >= heightMap.GetLength(0))
        {
            value += (heightMap[x, y] * (1f - dX) + heightMap[x + 1, y] * dX) * (1f - dY);
            value += (heightMap[x, y + 1] * (1f - dX) + heightMap[x + 1, y + 1] * dX) * dY;
            return value;
        }

        var firstHeights = ValuesInX(x, y);
        var secondHeights = ValuesInX(x, y + 1);

        if (method == Method.Bicubic)
        {
            value += InterpolateCubic(firstHeights[0], firstHeights[1], firstHeights[2], firstHeights[3], dX) * (1f - dY);
            value += InterpolateCubic(secondHeights[0], secondHeights[1], secondHeights[2], secondHeights[3], dX) * dY;
        }
        else if (method == Method.Cosine)
        {
            value += InterpolateCosine(firstHeights[1], firstHeights[2], dX) * (1f - dY);
            value += InterpolateCosine(secondHeights[1], secondHeights[2], dX) * dY;
        }
        else
        {
            value += InterpolateLinear(firstHeights[1], firstHeights[2], dX) * (1f - dY);
            value += InterpolateLinear(secondHeights[1], secondHeights[2], dX) * dY;
        }

        return value;

        float[] ValuesInX(int x, int y)
        {
            float[] Values = new float[4];
            Values[0] = heightMap[x - 1, y];
            Values[1] = heightMap[x, y];
            Values[2] = heightMap[x + 1, y];
            Values[3] = heightMap[x + 2, y];
            return Values;
        }
    }
    float InterpolateY(int x, int y, float dX, float dY)
    {
        float value = 0;
        if (y <= 0 || y + 2 >= heightMap.GetLength(1))
        {
            value += (heightMap[x, y] * (1f - dY) + heightMap[x, y + 1] * dY) * (1f - dX);
            value += (heightMap[x + 1, y] * (1f - dY) + heightMap[x + 1, y + 1] * dY) * dX;
            return value;
        }

        var firstHeights = ValuesInY(x, y);
        var secondHeights = ValuesInY(x + 1, y);

        if (method == Method.Bicubic)
        {
            value += InterpolateCubic(firstHeights[0], firstHeights[1], firstHeights[2], firstHeights[3], dY) * (1f - dX);
            value += InterpolateCubic(secondHeights[0], secondHeights[1], secondHeights[2], secondHeights[3], dY) * dX;
        }
        else if (method == Method.Cosine)
        {
            value += InterpolateCosine(firstHeights[1], firstHeights[2], dY) * (1f - dX);
            value += InterpolateCosine(secondHeights[1], secondHeights[2], dY) * dX;
        }
        else
        {
            value += InterpolateLinear(firstHeights[1], firstHeights[2], dY) * (1f - dX);
            value += InterpolateLinear(secondHeights[1], secondHeights[2], dY) * dX;
        }
        return value;

        float[] ValuesInY(int x, int y)
        {
            float[] Values = new float[4];
            Values[0] = heightMap[x, y - 1];
            Values[1] = heightMap[x, y];
            Values[2] = heightMap[x, y + 1];
            Values[3] = heightMap[x, y + 2];
            return Values;
        }
    }

    private float InterpolateCubic(float a0, float a1, float a2, float a3, float dA)
    {
        float p = (a3 - a2) - (a0 - a1);
        float q = (a0 - a1) - p;
        float r = a2 - a0;
        return p * dA * dA * dA + q * dA * dA + r * dA + a1;
    }

    private float InterpolateCosine(float a1, float a2, float dA)
    {
        float f = (1 - Mathf.Cos(dA * Mathf.PI)) * 0.5f;
        return a1 * (1 - f) + a2 * f;
    }

    private float InterpolateLinear(float a1, float a2, float dA)
    {
        return a1 * (1 - dA) + a2 * dA;
    }

}
