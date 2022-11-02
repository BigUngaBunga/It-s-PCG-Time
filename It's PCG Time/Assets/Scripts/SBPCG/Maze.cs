using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using static MazeGen;

public class Maze
{
    public enum MazeComponent { Wall, Path, Entrance, Treasure}

    public MazeComponent[,] MazeGrid { get; private set; }
    public int DistanceToTreasure { get; private set; }
    public int ReachablePaths { get; private set; }
    public int ReachableTreasures { get; private set; }


    public int NumberOfPaths { get; private set; }
    public int NumberOfEntrances { get; private set; }
    public int NumberOfTreasures { get; private set; }

    public Maze(MazeComponent[,] maze)
    {
        MazeGrid = maze;
        CountElements();
    }

    private void CountElements()
    {
        NumberOfPaths = 0;
        NumberOfEntrances = 0;
        NumberOfTreasures = 0;

        foreach (var value in MazeGrid)
        {
            switch (value)
            {
                case MazeComponent.Wall:
                    break;
                case MazeComponent.Path:
                    NumberOfPaths++;
                    break;
                case MazeComponent.Entrance:
                    NumberOfEntrances++;    
                    break;
                case MazeComponent.Treasure:
                    NumberOfTreasures++;
                    break;
            }
        }
    }

    //TODO räkna antalet nåbara vägar och skatter
    //TODO mät avståndet från ingången till skatterna
    //TODO mät mängden avstickande vägar och deras längd
    private void TraverseMaze()
    {
        bool[,] hasBeenSearched = new bool[MazeGrid.GetLength(0), MazeGrid.GetLength(1)];
        Queue<Point> searchQueue = new Queue<Point>();

        //AddPoint();
        Point currentPoint;
        while (searchQueue.Count > 0)
        {
            currentPoint = searchQueue.Dequeue();
            var adjacent = GetAdjacentPoints(currentPoint);

            foreach (var point in adjacent)
                Evaluate(point);

        }



        void Evaluate(Point point)
        {
            searchQueue.Enqueue(point);
            hasBeenSearched[point.X, point.Y] = true;

        }
    }

    //TODO läs in närliggande
    private List<Point> GetAdjacentPoints(Point currentPoint)
    {
        List<Point> result = new List<Point>();

        return result;
    }
}
