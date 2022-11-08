using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Random = UnityEngine.Random;

public class Maze : IComparable
{
    public enum MazeComponent { Wall, Path, Entrance, Treasure}

    public MazeComponent[,] MazeGrid { get; private set; }
    public static float mutationFactor;
    public float Fitness { get; private set; }

    public int Width => MazeGrid.GetLength(0);
    public int Height => MazeGrid.GetLength(1);

    private Dictionary<Point, int> treasureDistance;
    private Queue<Point> searchQueue;
    private bool[,] hasBeenSearched;

    private Point entrance;
    private int reachablePaths;
    private int reachableTreasures;
    private int narrowness;

    private int numberOfPaths;
    private int numberOfEntrances;
    private int numberOfTreasures;
    private int numberOfWalls;

    public Maze(Maze parentA, Maze parentB, int crossoverStart, int crossoverEnd)
    {
        CombineAndMutate(parentA.MazeGrid, parentB.MazeGrid, crossoverStart, crossoverEnd);
        Evaluate();
    }

    public Maze(Point mazeSize)
    {
        GenerateRandomMaze(mazeSize);
        Evaluate();
    }

    public Maze(int mazeSize)
    {
        GenerateRandomMaze(new Point(mazeSize, mazeSize));
        Evaluate();
    }

    public MazeComponent GetValueAt(int x, int y) => MazeGrid[x, y];

    private void GenerateRandomMaze(Point mazeSize)
    {
        
        bool hasEntrance = false;
        MazeGrid = new MazeComponent[mazeSize.X, mazeSize.Y];

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var typeToPlace = GetRandomComponent(hasEntrance);
                MazeGrid[x, y] = typeToPlace;
                if (typeToPlace == MazeComponent.Entrance)
                    hasEntrance = true;
            }
    }

    private MazeComponent GetRandomComponent(bool preventEntrance)
    {
        float[] values = new float[] {0.01f, 0.01f, 0.45f};
        for (int i = 1; i < values.Length; i++)
            values[i] += values[i - 1];

        float randomValue = Random.value;
        if (!preventEntrance && values[0] > randomValue)
            return MazeComponent.Entrance;
        else if (values[1] > randomValue)
            return MazeComponent.Treasure;
        else if (values[2] > randomValue)
            return MazeComponent.Path;
        return MazeComponent.Wall;
    }

    private void CombineAndMutate(MazeComponent[,] parentA, MazeComponent[,] parentB, int crossoverStart, int crossoverEnd)
    {
        MazeGrid = parentA;

        Point start = new Point(crossoverStart / Width, crossoverStart % Width);
        Point stop = new Point(crossoverEnd / Width, crossoverEnd % Width);
        int y = start.Y;
        bool lastX = false;
        for (int x = start.X; x < stop.X; x++)
        {
            lastX = x + 1 >= stop.X;
            for (y = y;  y < Height; y++)
            {
                MazeGrid[x, y] = Random.value < mutationFactor ? GetRandomComponent(false) : parentB[x, y];
                if (lastX && y + 1 >= stop.Y)
                    break;
            }
            y = 0;
        }
    }

    private void Evaluate()
    {
        CountElements();
        TraverseMaze();
    }

    private void CountElements()
    {
        numberOfWalls = 0;
        numberOfPaths = 0;
        numberOfEntrances = 0;
        numberOfTreasures = 0;

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                switch (MazeGrid[x, y])
                {
                    case MazeComponent.Wall:
                        numberOfWalls++;
                        break;
                    case MazeComponent.Path:
                        numberOfPaths++;
                        break;
                    case MazeComponent.Entrance:
                        numberOfEntrances++;
                        entrance = new Point(x, y);
                        break;
                    case MazeComponent.Treasure:
                        numberOfTreasures++;
                        break;
                }

        
    }

    //TODO kanske mät mängden avstickande vägar och deras längd
    private void TraverseMaze()
    {
        hasBeenSearched = new bool[Width, Height];
        searchQueue = new Queue<Point>();
        treasureDistance = new Dictionary<Point, int>();
        int distanceFromEntrance = 0;
        int enqueuedPoints = 0;

        AddPointToSearch(entrance, 0);
        while (searchQueue.Count > 0)
        {
            ++distanceFromEntrance;
            enqueuedPoints = searchQueue.Count;
            for (int i = 0; i < enqueuedPoints; i++)
            {
                Point currentPoint = searchQueue.Dequeue();
                var adjacent = GetNeumannAdjacent(currentPoint);
                if (GetType(currentPoint) == MazeComponent.Path || GetType(currentPoint) == MazeComponent.Treasure)
                    MeasureNarrowness(currentPoint, adjacent);
                
                foreach (var point in adjacent)
                    AddPointToSearch(point, distanceFromEntrance);
            }
        }
    }

    private void AddPointToSearch(Point point, int distanceFromEntrance)
    {
        if (!hasBeenSearched[point.X, point.Y])
        {
            var type = GetType(point);

            if (type != MazeComponent.Wall)
                searchQueue.Enqueue(point);
            if (type == MazeComponent.Path)
                ++reachablePaths;
            else if (type == MazeComponent.Treasure)
            {
                ++reachableTreasures;
                treasureDistance.Add(point, distanceFromEntrance);
            }
            hasBeenSearched[point.X, point.Y] = true;
        }
    }

    private List<Point> GetNeumannAdjacent(Point currentPoint)
    {
        List<Point> result = new List<Point>();
        int distance = 1;
        for (int i = -distance; i < (1 + distance); i++)
        {
            if (i == 0)
                continue;
            AddIfWithinBounds(new Point(currentPoint.X + i, currentPoint.Y));
            AddIfWithinBounds(new Point(currentPoint.X, currentPoint.Y + i));
        }
        return result;

        void AddIfWithinBounds(Point point)
        {
            if (point.X >= 0 && point.X < Width && point.Y >= 0 && point.Y < Height)
                result.Add(point);
        }
    }

    private MazeComponent GetType(Point point) => MazeGrid[point.X, point.Y];

    private void MeasureNarrowness(Point startPoint, List<Point> adjacent)
    {
        int passableNeighbours = 0;
        foreach (var neighbour in adjacent)
        {
            var type = GetType(neighbour);
            if (type == MazeComponent.Path || type == MazeComponent.Treasure)
                passableNeighbours++;
        }

        narrowness += 3 - passableNeighbours;
    }

    public float CalculateFitness(float pathWeight, float treasureWeight, float treasureDistanceWeight, float narrownessWeight, float reachableWeight)
    {
        Fitness = 0;
        if (reachableTreasures <= 0 || numberOfEntrances != 1)
            Fitness = -100;
        else
        {
            int unreachablePaths = numberOfPaths - reachablePaths;
            int unreachableTreasures = numberOfTreasures - reachableTreasures;
            //Fitness += numberOfWalls;

            Fitness += reachablePaths * reachableWeight;
            Fitness -= unreachablePaths * reachableWeight;

            Fitness += reachableTreasures * treasureWeight * reachableWeight;
            Fitness -= unreachableTreasures * treasureWeight * reachableWeight;

            Fitness += narrowness * narrownessWeight;

            foreach (int distance in treasureDistance.Values)
                Fitness += (distance - (Width + Height) / 4) * treasureDistanceWeight;
            Fitness -= Mathf.Pow(1.5f, treasureDistance.Count);
        }

        return Fitness;
    }

    public int CompareTo(object obj)
    {
        if (obj is Maze maze)
        {
            if (Fitness > maze.Fitness)
                return -1;
            if (Fitness == maze.Fitness)
                return 0;
        }
        return 1;
    }
}
