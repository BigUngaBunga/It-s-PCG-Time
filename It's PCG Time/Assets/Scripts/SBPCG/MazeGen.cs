using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Component = Maze.MazeComponent;

public class MazeGen : MonoBehaviour
{
    [Header("Maze")]
    [Min(5)]
    [SerializeField] private int mazeSize;
    [Range(2, 1000)]
    [SerializeField] private int populationSize;
    private List<Maze> mazes;

    [Header("Fitness function")]
    [SerializeField] private float pathWeight;
    [SerializeField] private float treasureWeight;
    [SerializeField] private float treasureDistanceWeight;
    [SerializeField] private float narrownessWeight;
    [SerializeField] private float reachableWeight;

    private void Start()
    {
        GenerateNewPopulation();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GenerateNewPopulation();
    }

    private void GenerateNewPopulation()
    {
        mazes = new List<Maze>();
        for (int i = 0; i < populationSize; i++)
            mazes.Add(new Maze(mazeSize));

        GetBestAndWorst(out Maze best, out _);
        PrintMaze(best);
    }

    private void GetBestAndWorst(out Maze best, out Maze worst)
    {
        best = worst = null;
        float worstFitness = float.MaxValue, bestFitness = float.MinValue;
        foreach (var maze in mazes)
        {
            float fitness = maze.CalculateFitness(pathWeight, treasureWeight, treasureDistanceWeight, narrownessWeight, reachableWeight);
            if (fitness > bestFitness)
            {
                bestFitness = fitness;
                best = maze;
            }
            if (fitness < worstFitness)
            {
                worstFitness = fitness;
                worst = maze;
            }
        }
    }

    private void PrintMaze(Maze maze)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("Printing maze:");
        for (int x = 0; x < maze.Width; x++)
        {
            stringBuilder.AppendLine();
            for (int y = 0; y < maze.Height; y++)
            {
                stringBuilder.Append(GetSymbol(maze.GetValueAt(x, y)));
                stringBuilder.Append(' ');
            }
        }

        Debug.Log(stringBuilder.ToString());

        char GetSymbol(Component component)
        {
            return component switch
            {
                Component.Wall => 'X',
                Component.Path => '-',
                Component.Entrance => 'E',
                Component.Treasure => 'T',
                _ => '0',
            };
        }
    }
}
