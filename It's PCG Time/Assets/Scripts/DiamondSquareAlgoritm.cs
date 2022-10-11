using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class DiamondSquareAlgorithm
{
    private float[,] heightMap;
    private int size;
    private float roughness;
    private float amplitude;
    private float RandomAmplitude => (Random.value -0.5f) * 2f * amplitude;
    private float Width => heightMap.GetLength(0);
    private float Height => heightMap.GetLength(1);

    private bool WithinBound(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
    private float GetHeightAt(int x, int y) => heightMap[x, y];
    private bool TryGetHeight(int x, int y, ref float value)
    {
        value = 0;
        if (WithinBound(x, y))
        {
            value = GetHeightAt(x, y);
            return true;
        }
        return false;
    }

    public float[,] Generate(int sizeValue, int seed, float amplitude, float roughness)
    {
        size = (int)Mathf.Pow(2, sizeValue) + 1;
        Debug.Log("Size: " + size + " sizeValue: " + sizeValue);
        Debug.Log("Initial amplitude: " + amplitude);
        this.roughness = roughness;
        this.amplitude = amplitude;
        heightMap = new float[size, size];
        Random.InitState(seed);

        FillCorners();
        FillSquare();
        return heightMap;
    }

    private void FillCorners()
    {
        heightMap[0, 0] = RandomAmplitude;
        heightMap[size - 1, 0] = RandomAmplitude;
        heightMap[0, size - 1] = RandomAmplitude;
        heightMap[size - 1, size - 1] = RandomAmplitude;
    }

    private void FillSquare()
    {
        int lenghtToCorners = size /= 2;
        bool lastWasSquare = false;
        Queue<CalculationPoint> calculationPoints = new Queue<CalculationPoint>();
        calculationPoints.Enqueue(new CalculationPoint(lenghtToCorners, true));


        while (calculationPoints.Count > 0)
        {
            var point = calculationPoints.Dequeue();

            if (lastWasSquare == point.calculateDiamond)
            {
                amplitude *= Mathf.Pow(2f, -roughness);
                Debug.Log("New amplitude is: " + amplitude);
            }
                

            if (lastWasSquare && point.calculateDiamond)
                lenghtToCorners /= 2;
            lastWasSquare = !point.calculateDiamond;

            if (point.calculateDiamond)
                CalculateDiamond(point.point, lenghtToCorners);
            else
                CalculateSquare(point.point, lenghtToCorners);

            if (!point.calculateDiamond && lenghtToCorners <= 1)
                continue;
            EnqueueNext(point);
        }

        void EnqueueNext(CalculationPoint calculationPoint)
        {
            Point point = calculationPoint.point;
            if (calculationPoint.calculateDiamond)
            {
                EnqueueValid(new CalculationPoint(point.X, point.Y + lenghtToCorners, false));
                EnqueueValid(new CalculationPoint(point.X + lenghtToCorners, point.Y, false));
                EnqueueValid(new CalculationPoint(point.X, point.Y - lenghtToCorners, false));
                EnqueueValid(new CalculationPoint(point.X - lenghtToCorners, point.Y, false));
            }
            else
            {
                int halfLenght = lenghtToCorners / 2;
                EnqueueValid(new CalculationPoint(point.X + halfLenght, point.Y + halfLenght, true));
                EnqueueValid(new CalculationPoint(point.X + halfLenght, point.Y - halfLenght, true));
                EnqueueValid(new CalculationPoint(point.X - halfLenght, point.Y + halfLenght, true));
                EnqueueValid(new CalculationPoint(point.X - halfLenght, point.Y - halfLenght, true));
            }
        }

        void EnqueueValid(CalculationPoint point)
        {
            if (WithinBound(point.point.X, point.point.Y))
                calculationPoints.Enqueue(point);
        }
    }

    private void CalculateDiamond(Point point, int lenghtToCorners)
    {
        float height = GetHeightAt(point.X + lenghtToCorners, point.Y + lenghtToCorners);
        height += GetHeightAt(point.X + lenghtToCorners, point.Y - lenghtToCorners);
        height += GetHeightAt(point.X - lenghtToCorners, point.Y + lenghtToCorners);
        height += GetHeightAt(point.X - lenghtToCorners, point.Y - lenghtToCorners);
        heightMap[point.X, point.Y] = height / 4f + RandomAmplitude;
    }

    private void CalculateSquare(Point point, int lenghtToCorners)
    {
        List<Point> points = new List<Point> { new Point(point.X, point.Y + lenghtToCorners), new Point(point.X + lenghtToCorners, point.Y),
                                                new Point(point.X, point.Y - lenghtToCorners), new Point(point.X - lenghtToCorners, point.Y)};
        float height = 0;
        float value = 0;
        int validHeights = 0;
        foreach (var heightMapPoint in points)
        {
            if (TryGetHeight(heightMapPoint.X, heightMapPoint.Y, ref value))
            {
                height += value;
                ++validHeights;
            } 
        }
        heightMap[point.X, point.Y] = height / validHeights + RandomAmplitude;
    }

    public struct CalculationPoint
    {

        public Point point;
        public bool calculateDiamond;

        public CalculationPoint(int position, bool calculateDiamond)
        {
            this.point = new Point(position, position);
            this.calculateDiamond = calculateDiamond;
        }

        public CalculationPoint(int x, int y, bool calculateDiamond)
        {
            this.point = new Point(x, y);
            this.calculateDiamond = calculateDiamond;
        }

        public CalculationPoint(Point point, bool calculateDiamond)
        {
            this.point = point;
            this.calculateDiamond = calculateDiamond;
        }
    }
}