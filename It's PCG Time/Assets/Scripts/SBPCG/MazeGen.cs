using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Component = Maze.MazeComponent;

public class MazeGen : MonoBehaviour
{
    [Header("Maze variables")]
    [Min(5)]
    [SerializeField] private int mazeSize;
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private string seed;
    private List<Maze> bestMazePerGeneration; //TODO spara s� att de kan inspekteras efter generering
    private List<Maze> currentGeneration;

    //TODO prova att generera en fast position p� d�rren

    [Header("Search variables")]
    [Range(2, 1000)]
    [SerializeField] private int populationSize;
    [SerializeField] private int maxGenerations;
    [SerializeField] private float targetFitness;
    [Range(0, 1)]
    [SerializeField] private float crossoverPercentage;
    [Range(0, 1)]
    [SerializeField] private float mutationFactor;
    private int generation;
    private float rankSum;


    [Header("Fitness function")]
    [SerializeField] private float pathWeight;
    [SerializeField] private float treasureWeight;
    [SerializeField] private float treasureDistanceWeight;
    [SerializeField] private float narrownessWeight;
    [SerializeField] private float reachableWeight;

    private void Start()
    {
        StartCoroutine(StartNewSearch());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StopAllCoroutines();
            StartCoroutine(StartNewSearch());
        }
    }

    private void EvaluateRandomSeed()
    {
        if (randomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue).ToString();

        if (!int.TryParse(seed, out int seedValue))
            seedValue = seed.GetHashCode();
       
        Random.InitState(seedValue);
    }

    private IEnumerator StartNewSearch()
    {
        EvaluateRandomSeed();
        bestMazePerGeneration = new List<Maze>();
        generation = 1;
        GenerateNewPopulation();
        EvaluatePopulation();
        yield return null;

        do
        {
            generation++;
            Reproduce();
            EvaluatePopulation();
            yield return null;
        } while (GetAverageFitness() < targetFitness && generation < maxGenerations);
        Debug.Log("Finished search");
    }

    private void GenerateNewPopulation()
    {
        currentGeneration = new List<Maze>();
        for (int i = 0; i < populationSize; i++)
            currentGeneration.Add(new Maze(mazeSize));
    }

    private void EvaluatePopulation()
    {
        if (generation == maxGenerations)
        {

        }
        foreach (var maze in currentGeneration)
            maze.CalculateFitness(pathWeight, treasureWeight, treasureDistanceWeight, narrownessWeight, reachableWeight);
        currentGeneration.Sort();

        GetBestAndWorst(out Maze best, out _);
        PrintMaze(best, "Best in generation " + generation);
        bestMazePerGeneration.Add(best);

        Debug.Log("Generation " + generation);
        Debug.Log("The best fitness was: " + GetBestFitness() + " the average was: " + GetAverageFitness());
    }

    private void Reproduce()
    {
        var nextGeneration = new List<Maze>();
        Maze.mutationFactor = mutationFactor;
        Maze mazeA, mazeB;
        while (nextGeneration.Count < currentGeneration.Count)
        {

            mazeA = GetRankedIndividual();
            mazeB = GetRankedIndividual();
            if (Random.value < crossoverPercentage)
            {
                int startIndex = Random.Range(0, mazeB.MazeGrid.Length - 1);
                int stopIndex = Random.Range(startIndex, mazeB.MazeGrid.Length - 1);
                nextGeneration.Add(new Maze(mazeA, mazeB, startIndex, stopIndex));
                nextGeneration.Add(new Maze(mazeB, mazeA, startIndex, stopIndex));
            }
            else
            {
                nextGeneration.Add(mazeA);
                nextGeneration.Add(mazeB);
            }
        }
        currentGeneration = nextGeneration;
    }

    private Maze GetRankedIndividual()
    {
        rankSum = (populationSize + 1) * populationSize / (float)2;
        float value = Random.Range(0, rankSum);
        float rank = -0.5f + Mathf.Sqrt(0.25f + value * 2);
        int index = currentGeneration.Count - Mathf.CeilToInt(rank) - 1;
        return currentGeneration[Mathf.Max(0, index)];
    }

    private float GetBestFitness() => currentGeneration[0].Fitness;

    private float GetAverageFitness()
    {
        float totalFitness = 0;
        foreach (var maze in currentGeneration)
            totalFitness += maze.Fitness;
        return totalFitness / currentGeneration.Count;
    }

    private void GetBestAndWorst(out Maze best, out Maze worst)
    {
        best = currentGeneration[0];
        worst = currentGeneration[currentGeneration.Count -1];
    }

    private void PrintMaze(Maze maze, string message)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(message);
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
