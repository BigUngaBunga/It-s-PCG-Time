using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using Component = Maze.MazeComponent;

public class MazeGen : MonoBehaviour
{
    [Header("Maze variables")]
    [Min(5)]
    [SerializeField] private int mazeSize;
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private string seed;
    private List<Maze> bestMazePerGeneration; //TODO spara så att de kan inspekteras efter generering
    private List<Maze> currentGeneration;


    //TODO prova att generera en fast position på dörren

    [Header("Search variables")]
    [Range(2, 1000)]
    [SerializeField] private int populationSize;
    [SerializeField] private int maxGenerations;
    [SerializeField] private float targetFitness;
    [SerializeField] private int elites = 5;
    [Range(0, 1)]
    [SerializeField] private float crossoverPercentage;
    [Range(0, 1)]
    [SerializeField] private float mutationFactor;
    private int generation;
    private float rankSum;
    private bool writeInformation = true;

    [Header("Fitness factors")]
    [SerializeField] private float treasureWeight;
    [SerializeField] private float reachableWeight;
    
    private float GetBestFitness() => currentGeneration[0].Fitness;
    private float GetAverageFitness()
    {
        float totalFitness = 0;
        foreach (var maze in currentGeneration)
            totalFitness += maze.Fitness;
        return totalFitness / currentGeneration.Count;
    }
    private Maze GetBest() => currentGeneration[0];
    private Maze GetWorst() => currentGeneration[currentGeneration.Count - 1];

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
        if (Input.GetKeyDown(KeyCode.K))
        {
            StopAllCoroutines();
            StartCoroutine(MessureExpressivity());
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
        StreamWriter streamWriter = null;
        if (writeInformation)
             streamWriter = new StreamWriter("EvolutionMetrics.txt");
        EvaluateRandomSeed();
        bestMazePerGeneration = new List<Maze>();
        generation = 1;
        GenerateNewPopulation();
        EvaluatePopulation();
        WriteGenerationToFile();
        yield return null;

        do
        {
            generation++;
            Reproduce();
            EvaluatePopulation();
            WriteGenerationToFile();
            if (writeInformation)
                yield return null;
        } while (GetBestFitness() < targetFitness && generation < maxGenerations); //  

        if (writeInformation)
        {
            Debug.Log("Finished search");
            streamWriter.Close();
        }

        void WriteGenerationToFile()
        {
            if (streamWriter != null)
                streamWriter.WriteLine($"Generation {generation}, Best: {GetBest().Fitness}, Average: {GetAverageFitness()}, Worst: {GetWorst().Fitness}");
        }
    }

    private void GenerateNewPopulation()
    {
        currentGeneration = new List<Maze>();
        for (int i = 0; i < populationSize; i++)
            currentGeneration.Add(new Maze(mazeSize));
    }

    private void EvaluatePopulation()
    {
        foreach (var maze in currentGeneration)
            maze.CalculateFitness(reachableWeight, treasureWeight);
        currentGeneration.Sort();

        var bestMaze = GetBest();
        if (writeInformation)
            bestMaze.Print("Best in generation " + generation);
        bestMazePerGeneration.Add(bestMaze);
    }

    private void Reproduce()
    {
        var nextGeneration = new List<Maze>();
        Maze.mutationFactor = mutationFactor;
        Maze mazeA, mazeB;
        for (int i = 0; i < elites; i++)
        {
            if (i >= currentGeneration.Count)
                break;
            nextGeneration.Add(currentGeneration[i]);
        }

        while (nextGeneration.Count < currentGeneration.Count)
        {

            mazeA = GetRankedIndividual();
            mazeB = GetRankedIndividual();
            if (Random.value < crossoverPercentage)
            {
                int startIndex = Random.Range(0, mazeB.MazeGrid.Length - 1);
                nextGeneration.Add(new Maze(mazeA, mazeB, startIndex));
                nextGeneration.Add(new Maze(mazeB, mazeA, startIndex));
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

    private IEnumerator MessureExpressivity()
    {
        int numberOfSearches = 1000;
        int expressivityDetail = 20;
        int[] narrowness = new int[expressivityDetail];
        int[] branching = new int[expressivityDetail];

        writeInformation = false;
        for (int i = 0; i < numberOfSearches; i++)
        {
            StartCoroutine(StartNewSearch());
            AddExpressivity(GetBest().GetExpressivity());
            Debug.Log("Calculated " + (i + 1) + " out of " + numberOfSearches);
            yield return null;
        }
        Debug.Log("Finished mesuring expressivity");
        using (var streamWriter = new StreamWriter("Expressivity.txt"))
        {
            for (int i = 0; i < narrowness.Length; i++)
            {
                streamWriter.Write(narrowness[i]);
                streamWriter.Write(" ");
            }
            streamWriter.WriteLine();
            for (int i = 0; i < branching.Length; i++)
            {
                streamWriter.Write(branching[i]);
                streamWriter.Write(" ");
            }
        }
        writeInformation = true;

        void AddExpressivity(Vector2 expressivity)
        {
            int narrowIndex = (int)(expressivity.x * narrowness.Length);
            int branchIndex = (int)(expressivity.y * branching.Length); ;
            narrowness[narrowIndex]++;
            branching[branchIndex]++;
        }
    }
}
