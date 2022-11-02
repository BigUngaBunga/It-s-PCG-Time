using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using Color = UnityEngine.Color;

public class LandArea
{
    public enum WaterRemovalArea { Neumann, Moore}

    public static WaterRemovalArea removalArea;

    public int Size => landPoints.Count;
    public Point Start => start;

    private List<Point> landPoints;
    private List<Point> coastPoints;
    private Queue<Point> searchQueue;
    private bool[,] searchedPoints;
    private float[,] heightMap;

    private float waterHeight;
    private Point start;

    public List<Point> CoastPoints => coastPoints;

    public LandArea(Point start, float[,] heightMap, float waterHeight)
    {
        landPoints = new List<Point>();
        coastPoints = new List<Point>();
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
    public Point GetRandomCoast() => coastPoints[Random.Range(0, coastPoints.Count - 1)];
    public List<Point> GetAdjacentCoast(Point start)
    {
        var adjacent = GetAdjacentPoints(start);
        for (int i = adjacent.Count - 1; i >= 0; i--)
        {
            bool hasWaterNeighbor = false;
            var neighbours = GetNeumannAdjacentPoints(adjacent[i], 1, false);
            for (int j = 0; j < neighbours.Count; j++)
                if (!IsLand(neighbours[j]))
                {
                    hasWaterNeighbor = true;
                    break;
                }

            if (!hasWaterNeighbor)
                adjacent.RemoveAt(i);
        }
        return adjacent;
    }
    public Point GetClosestWaterTile(Point start)
    {
        searchQueue.Clear();
        searchedPoints = new bool[heightMap.GetLength(0), heightMap.GetLength(1)];

        AddPoint(start, false);
        Point currentPoint;
        while (searchQueue.Count > 0)
        {
            currentPoint = searchQueue.Dequeue();
            var adjacent = GetNeumannAdjacentPoints(currentPoint, excludeWater: false);

            foreach (var point in adjacent)
            {
                if (!IsLand(point))
                    return point;
                else if (!WasSearched(point))
                    AddPoint(point, false);
            }
        }
        return currentPoint;
    }

    public List<Point> GetAdjacentPoints(Point start, int distance = 1, bool excludeWater = true)
    {
        List<Point> adjacent = new List<Point>();

        for (int x = -distance; x < (1 + distance); x++)
        {
            for (int y = -distance; y < (1 + distance); y++)
            {
                Point point = new Point(start.X + x, start.Y + y);
                if (IsWithinBounds(point) && point != start)
                {
                    if (excludeWater && !IsLand(point))
                        continue;
                    adjacent.Add(point);
                }
                    
            }
        }

        return adjacent;
    }

    public List<Point> GetNeumannAdjacentPoints(Point start, int distance = 1, bool excludeWater = true)
    {
        List<Point> adjacent = GetAdjacentPoints(start, distance, excludeWater);

        for (int i = adjacent.Count - 1; i >= 0; i--)
        {
            if (adjacent[i].X == start.X || adjacent[i].Y == start.Y)
                continue;
            adjacent.RemoveAt(i);
        }
        return adjacent;
    }

    public void CalculateLandArea(float[,] heightMap)
    {
        this.heightMap = heightMap;
        landPoints.Clear();
        searchQueue.Clear();
        searchedPoints = new bool[heightMap.GetLength(0), heightMap.GetLength(1)];
        int searchedCounter = 0;

        AddPoint(start);
        Point currentPoint;

        int distance = 2;
        while (searchQueue.Count > 0)
        {
            ++searchedCounter;
            currentPoint = searchQueue.Dequeue();
            var adjacent = GetAdjacentPoints(currentPoint, distance);
                
            foreach (var point in adjacent)
                if (!WasSearched(point))
                    AddPoint(point);
        }
        CalculateCoast(heightMap);
    }

    private void AddPoint(Point point, bool searchingLand = true)
    {
        if (searchingLand)
            landPoints.Add(point);
        searchQueue.Enqueue(point);
        searchedPoints[point.X, point.Y] = true;
    }

    private void CalculateCoast(float[,] heightMap)
    {
        coastPoints.Clear();
        foreach (var point in landPoints)
        {
            var neighbours = GetNeumannAdjacentPoints(point, 1, false);
            foreach (var neighbour in neighbours)
            {
                if (!IsLand(neighbour) && !IsLonleyWaterPoint(neighbour))
                {
                    coastPoints.Add(point);
                    break;
                }
            }
        }
    }
    private bool IsLonleyWaterPoint(Point point)
    {
        List<Point> neighbours;
        int tooManyNeighbors;
        bool wasRemoved = false;
        if (removalArea == WaterRemovalArea.Neumann)
        {
            neighbours = GetNeumannAdjacentPoints(point, 1, true);
            tooManyNeighbors = 4;
        }
        else
        {
            neighbours = GetAdjacentPoints(point, 1, true);
            tooManyNeighbors = 6;
        }
        

        if (neighbours.Count >= tooManyNeighbors)
        {
            float averageHeight = 0;
            foreach (var neighbour in neighbours)
                averageHeight += heightMap[neighbour.X, neighbour.Y];
            heightMap[point.X, point.Y] = averageHeight / neighbours.Count;
            wasRemoved = true;
        }

        if (wasRemoved && removalArea == WaterRemovalArea.Moore)
        {
            neighbours = GetAdjacentPoints(point, 1, false);
            foreach (var neighbour in neighbours)
                if (!IsLand(neighbour))
                    IsLonleyWaterPoint(neighbour);
        }

        return wasRemoved;
    }

    private bool IsLand(Point point) => heightMap[point.X, point.Y] > waterHeight;
    private bool WasSearched(Point point) => WasSearched(point.X, point.Y);
    private bool WasSearched(int x, int y) => searchedPoints[x, y];
    private bool IsWithinBounds(Point point) => IsWithinBounds(point.X, point.Y);
    private bool IsWithinBounds(int x, int y) => !(x < 0 || y < 0 || x >= searchedPoints.GetLength(0) || y >= searchedPoints.GetLength(1));
}


