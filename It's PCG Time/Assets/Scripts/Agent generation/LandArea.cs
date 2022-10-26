using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using UnityEngine;

public class LandArea
{

    public int Size => landPoints.Count;
    public Point Start => start;

    private List<Point> landPoints;
    private Queue<Point> searchQueue;
    private bool[,] searchedPoints;
    private float[,] heightMap;

    private float waterHeight;
    private Point start;

    public LandArea(Point start, float[,] heightMap, float waterHeight)
    {
        landPoints = new List<Point>();
        searchQueue = new Queue<Point>();
        this.waterHeight = waterHeight;
        this.start = start;
        CalculateLandArea(heightMap);
    }

    public bool ContainsPoint(Point point) => landPoints.Contains(point);
    public List<Point> GetRandomPoints(int numberOfPoints)
    {
        List<Point> points = landPoints;
        List<Point> result = new List<Point>();

        for (int i = 0; i < numberOfPoints; i++)
        {
            int index = Random.Range(0, points.Count - 1);
            result.Add(points[index]);
            points.RemoveAt(index);
        }
        return result;
    }
    public Point GetRandomPoint() => landPoints[Random.Range(0, landPoints.Count - 1)];
    public List<Point> GetAdjacentPoints(Point start, int distance = 1)
    {
        List<Point> adjacent = new List<Point>();

        for (int x = -distance; x < (1 + distance); x++)
        {
            for (int y = -distance; y < (1 + distance); y++)
            {
                Point point = new Point(start.X + x, start.Y + y);
                if (IsWithinBounds(point) && IsLand(point))
                    adjacent.Add(point);
            }
        }

        return adjacent;
    }

    public void CalculateLandArea(float[,] heightMap)
    {
        this.heightMap = heightMap;
        landPoints.Clear();
        searchQueue.Clear();
        searchedPoints = new bool[heightMap.GetLength(0), heightMap.GetLength(1)];

        if (IsLand(start))
            AddPoint(start);

        Point currentPoint;

        int distance = 2;
        while (searchQueue.Count > 0)
        {
            currentPoint = searchQueue.Dequeue();
            var adjacent = GetAdjacentPoints(currentPoint, distance);
            foreach (var point in adjacent)
                if (!WasSearched(point))
                    AddPoint(point);
        }
    }

    private void AddPoint(Point point)
    {
        searchQueue.Enqueue(point);
        landPoints.Add(point);
        searchedPoints[point.X, point.Y] = true;
    }

    private bool IsLand(Point point) => heightMap[point.X, point.Y] > waterHeight; 
    private bool WasSearched(Point point) => WasSearched(point.X, point.Y);
    private bool WasSearched(int x, int y) => searchedPoints[x, y];
    private bool IsWithinBounds(Point point) => IsWithinBounds(point.X, point.Y);
    private bool IsWithinBounds(int x, int y) => !(x < 0 || y < 0 || x >= searchedPoints.GetLength(0) || y >= searchedPoints.GetLength(1));
}
