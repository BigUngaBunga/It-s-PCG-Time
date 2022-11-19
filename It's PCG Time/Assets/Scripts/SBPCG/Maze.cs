using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using UnityEditor.VersionControl;
using UnityEngine;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

public class Maze : IComparable //Monobehaviour
{
    public enum MazeComponent { Wall, Path, Entrance, Treasure}

    public MazeComponent[,] MazeGrid { get; private set; }

    public static float mutationFactor;
    public float Fitness { get; private set; }
    public int Width => MazeGrid.GetLength(0);
    public int Height => MazeGrid.GetLength(1);

    private Queue<Point> searchQueue;
    private bool[,] hasBeenSearched;

    private Point Entrance { get; set; }
    private Point Treasure { get; set; }

    private int numberOfPaths;
    private int numberOfWalls;
    private int treasureDistance = -10;
    private int TreasureDistance
    {
        get { return treasureDistance; }
        set { treasureDistance = value; }
    }
    private int reachablePaths;
    private int reachableWalls;

    private float fitnessWall;
    private float fitnessPath;
    private float fitnessTreasure;

    //DEBUG BOOL
    private bool drawGizmos;

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
        //fitnessWall = (2 * reachableWalls - numberOfWalls) * reachableWeight;
        //fitnessPath = (2 * reachablePaths - numberOfPaths) * reachableWeight;
        fitnessTreasure = TreasureDistance * treasureWeight;
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

    public void EvaluateAgain() => Evaluate();

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
        MazeGrid = parentA.MazeGrid;
        Entrance = parentA.Entrance;
        Treasure = parentA.Treasure;


        Point start = new Point(crossoverStart / Width, crossoverStart % Width);
        int y;
        int moveDistance = 1;
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
        TreasureDistance = -10;
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
            TreasureDistance = distanceFromEntrance;
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

    #region DrawDebug

    private void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            float radius = 0.25f;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Gizmos.color = GetDrawColour(x, y);
                    Vector3 position = new Vector3(x, 0, y);
                    Vector3 boxPosition = new Vector3(x, 1, y);
                    Gizmos.DrawSphere(position, radius);
                    if (hasBeenSearched[x, y])
                        Gizmos.DrawCube(boxPosition, Vector3.one * radius);
                }
            }
        }
    }

    private Color GetDrawColour(int x, int y)
    {
        return GetType(x, y) switch
        {
            MazeComponent.Wall => Color.gray,
            MazeComponent.Path => Color.white,
            MazeComponent.Entrance => Color.green,
            MazeComponent.Treasure => Color.yellow,
            _ => Color.red,
        };
    }

    #endregion

    //private void MeasureNarrowness(Point startPoint, List<Point> adjacent)
    //{
    //    int passableNeighbours = 0;
    //    foreach (var neighbour in adjacent)
    //    {
    //        var type = GetType(neighbour);
    //        if (type == MazeComponent.Path || type == MazeComponent.Treasure)
    //            passableNeighbours++;
    //    }

    //    narrowness += 3 - passableNeighbours;
    //}

    //TODO för expressivity beräkna mängden avstickande vägar och hur breda gångarna är
}
