using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

public class Maze : IComparable
{
    public enum MazeComponent { Wall, Path, Entrance, Treasure}

    public static float mutationFactor;

    public MazeComponent[,] MazeGrid { get; private set; }
    public float Fitness { get; private set; }
    public int Width => MazeGrid.GetLength(0);
    public int Height => MazeGrid.GetLength(1);

    private Queue<Point> searchQueue;
    private bool[,] hasBeenSearched;

    public Point Entrance { get; private set; }
    public Point Treasure { get; private set; }

    private int numberOfPaths;
    private int reachablePaths;
    private int numberOfWalls;
    private int reachableWalls;
    private int treasureDistance = -10;

    private float fitnessWall;
    private float fitnessPath;
    private float fitnessTreasure;

    public Maze(Maze parentA, Maze parentB, int crossoverStart)
    {
        CombineAndMutate(parentA, parentB, crossoverStart);
    }
    public Maze(Point mazeSize)
    {
        GenerateRandomMaze(mazeSize);
    }
    public Maze(int mazeSize)
    {
        GenerateRandomMaze(new Point(mazeSize, mazeSize));
    }
    public Maze(Maze maze)
    {
        MazeGrid = maze.MazeGrid;
        Entrance = maze.Entrance;
        Treasure = maze.Treasure;
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
    public float CalculateFitness(float reachableWeight, float treasureWeight)
    {
        Evaluate();
        Fitness = 0;
        fitnessWall = (2 * reachableWalls - numberOfWalls) * reachableWeight;
        fitnessPath = (2 * reachablePaths - numberOfPaths) * reachableWeight;
        fitnessTreasure = treasureDistance * treasureWeight;
        Fitness = fitnessWall + fitnessPath + fitnessTreasure;
        return Fitness;
    }
    public void Print(string addedMessage)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(addedMessage);
        stringBuilder.Append("Fitness: ");
        stringBuilder.Append(Fitness);
        for (int x = 0; x < Width; x++)
        {
            stringBuilder.AppendLine();
            for (int y = 0; y < Height; y++)
            {
                stringBuilder.Append(GetSymbol(GetType(x, y)));
                stringBuilder.Append(' ');
            }
        }

        Debug.Log(stringBuilder.ToString());

        char GetSymbol(MazeComponent mazeComponent)
        {
            return mazeComponent switch
            {
                MazeComponent.Wall => 'X',
                MazeComponent.Path => '-',
                MazeComponent.Entrance => 'E',
                MazeComponent.Treasure => 'T',
                _ => '0',
            };
        }
    }

    public float[,] GetHeightMap()
    {
        float[,] heightMap = new float[Width, Height];
        int currentHeight = 0;
        for (int x = 0; x < MazeGrid.GetLength(0); x++)
        {
            for (int y = 0; y < MazeGrid.GetLength(1); y++)
            {
                currentHeight = MazeGrid[x,y] == MazeComponent.Wall ? 5 : 0;
                if (EqualsPoint(x, y, Entrance) || EqualsPoint(x, y, Treasure))
                    currentHeight = 0;
                heightMap[x,y] = currentHeight;
            }
        }

        return heightMap;
    }

    private Point GetRandomPoint() => new Point(Random.Range(0, Width), Random.Range(0, Height));
    private MazeComponent GetType(Point point) => GetType(point.X, point.Y);
    private MazeComponent GetType(int x, int y)
    {
        if (EqualsPoint(x, y, Entrance))
            return MazeComponent.Entrance;
        else if (EqualsPoint(x, y, Treasure))
            return MazeComponent.Treasure;
        return MazeGrid[x, y];
    }


    private bool EqualsPoint(int x, int y, Point point) => x == point.X && y == point.Y;
    private Point MovePointRandomly(Point point, int distance)
    {
        Point negativeMovement = new Point(Mathf.Max(-distance, -point.X), Mathf.Max(-distance, -point.Y));
        Point positiveMovement = new Point(Mathf.Min(distance, Width - point.X), Mathf.Min(distance, Height - point.Y));
        point.X += Random.Range(negativeMovement.X, positiveMovement.X);
        point.Y += Random.Range(negativeMovement.Y, positiveMovement.Y);
        return point;
    }
    
    private void GenerateRandomMaze(Point mazeSize)
    {
        MazeGrid = new MazeComponent[mazeSize.X, mazeSize.Y];

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                MazeGrid[x, y] = GetRandomComponent();
        Entrance = GetRandomPoint();
        Treasure = GetRandomPoint();
    }
    private MazeComponent GetRandomComponent()
    {
        float pathProbability = 0.45f;
        float randomValue = Random.value;
        if (pathProbability > randomValue)
            return MazeComponent.Path;
        return MazeComponent.Wall;
    }
    private void CombineAndMutate(Maze parentA, Maze parentB, int crossoverStart)
    {
        MazeGrid = (MazeComponent[,])parentA.MazeGrid.Clone();
        Entrance = parentA.Entrance;
        Treasure = parentA.Treasure;

        Point start = new Point(crossoverStart / Width, crossoverStart % Width);
        int y;
        int moveDistance = 3;
        for (int x = start.X; x < Width; x++)
        {
            for (y = start.Y; y < Height; y++)
            {
                bool mutate = Random.value < mutationFactor;
                MazeGrid[x, y] = mutate ? GetRandomComponent() : parentB.MazeGrid[x, y];

                if (EqualsPoint(x, y, parentB.Entrance))
                    Entrance = mutate ? MovePointRandomly(Entrance, moveDistance) : parentB.Entrance;
                if (EqualsPoint(x, y, parentB.Treasure))
                    Treasure = mutate ? MovePointRandomly(Treasure, moveDistance) : parentB.Treasure;
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

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                switch (GetType(x,y))
                {
                    case MazeComponent.Wall:
                        ++numberOfWalls;
                        break;
                    case MazeComponent.Path:
                        ++numberOfPaths;
                        break;
                }
            }
        }
    }
    private void TraverseMaze()
    {
        hasBeenSearched = new bool[Width, Height];
        searchQueue = new Queue<Point>();
        int distanceFromEntrance = 0;
        reachablePaths = 0;
        reachableWalls = 0;
        treasureDistance = -10;
        AddPointToSearch(Entrance, 0);

        while (searchQueue.Count > 0)
        {
            ++distanceFromEntrance;
            int currentCount = searchQueue.Count;
            for (int i = 0; i < currentCount; i++)
            {
                Point currentPoint = searchQueue.Dequeue();
                var adjacent = GetNeumannAdjacent(currentPoint);
                foreach (var point in adjacent)
                    AddPointToSearch(point, distanceFromEntrance);
            }
        }
    }

    private void AddPointToSearch(Point point, int distanceFromEntrance)
    {
        if (hasBeenSearched[point.X, point.Y])
            return;

        hasBeenSearched[point.X, point.Y] = true;
        var type = GetType(point);

        if (type != MazeComponent.Wall)
            searchQueue.Enqueue(point);
        else
            ++reachableWalls;

        if (type == MazeComponent.Path)
            ++reachablePaths;
        else if (type == MazeComponent.Treasure)
            treasureDistance = distanceFromEntrance;
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

    #region Expressivity
    public Vector2 GetExpressivity()
    {
        int searchedTiles = 0;
        int branches = 0;
        float narrowness = 0;
        float branching = 0;
        hasBeenSearched = new bool[Width, Height];
        searchQueue = new Queue<Point>();
        AddPointToSearch(Entrance, 0);

        while (searchQueue.Count > 0)
        {
            int currentCount = searchQueue.Count;
            for (int i = 0; i < currentCount; i++)
            {
                searchedTiles++;
                Point currentPoint = searchQueue.Dequeue();
                var adjacent = GetNeumannAdjacent(currentPoint);
                narrowness += GetNarrowness(adjacent);
                if (IsDeadEnd(adjacent))
                    branches++;
                foreach (var point in adjacent)
                {
                    if (!hasBeenSearched[point.X, point.Y])
                    {
                        hasBeenSearched[point.X, point.Y] = true;
                        if (GetType(point) != MazeComponent.Wall)
                            searchQueue.Enqueue(point);
                    }
                }
            }
        }

        narrowness /= searchedTiles * 2;
        branching = (branches * 2) / (float)searchedTiles;
        return new Vector2(narrowness, branching);
    }

    private int GetNarrowness(List<Point> neighbours)
    {
        int adjacentWalls = 0;
        for (int i = 0; i < neighbours.Count; i++)
            if (GetType(neighbours[i]) == MazeComponent.Wall)
                adjacentWalls++;
        return Mathf.Min(adjacentWalls, 2);
    }

    private bool IsDeadEnd(List<Point> neighbours)
    {
        int adjacentNonWall = 0;
        for (int i = 0; i < neighbours.Count; i++)
            if (GetType(neighbours[i]) != MazeComponent.Wall)
                adjacentNonWall++;
        return adjacentNonWall <= 1;
    }
    #endregion
}
