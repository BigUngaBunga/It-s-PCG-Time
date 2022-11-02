using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor.PackageManager;
using UnityEngine;
using static AgentGenerator;
using Status = AgentGenerator.GenerationStatus;

public class AgentManager : MonoBehaviour
{
    public Status status;

    public static AgentGenerator generator;

    [Header("Coastal agents fields")]
    [Min(1)]
    [SerializeField] private int initialCoastlineAgents = 1;
    [Range(1, 7)]
    [SerializeField] private int minimumAdjacentWaterTiles;
    [Range(0.0f, 1f)]
    [SerializeField] private float coastAgentFactor;
    [Range(1, 10)]
    [SerializeField] private int coastAgentReach;
    public bool forceNonOverlap = false;

    [Header("Smoothing agents fields")]
    [Range(1, 25)]
    public int smoothDistance;
    [Range(1, 25)]
    [SerializeField] private int smoothTokens;
    [Range(0.01f, 0.25f)]
    [SerializeField] private float smoothAgentFactor;

    [Header("Beach agents fields")]
    [Range(0.01f, 0.25f)]
    [SerializeField] private float beachAgentFactor;
    [Range(0.01f, 0.5f)]
    [SerializeField] private float beachAgentHeightFactor;
    [Range(1, 5)]
    [SerializeField] private int beachWidth;

    [Header("Mountain agents fields")]
    [Range(0.0f, 1f)]
    [SerializeField] private float mountainProbability;
    [Min(1)]
    [SerializeField] private float mountainWidth;

    [Header("Agent information")]
    [SerializeField] private int coastlineAgentTokens;
    [SerializeField] private int coastlineAgentLimit;


    [Header("Other")]
    [SerializeField] private bool visualize = false;
    [Range(0, 0.25f)]
    [SerializeField] private float agentWaitTime;
    [SerializeField] private float agentRadius = 0.25f;

    private readonly List<LandArea> areas = new List<LandArea>();
    private readonly List<Agent> agents = new List<Agent>();
    private bool[,] isOccupied;
    private Vector2 RandomDirection => new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));

    public void AddAgent(Agent agent)
    {
        agents.Add(agent);
        SetOccupation(agent.Point, true);
    }
    public void RemoveAgent(Agent agent)
    {
        agents.Remove(agent);
        SetOccupation(agent.Point, false);
    }
    public bool CanHaveMoreAgents()
    {
        return status switch
        {
            Status.Coast => agents.Count < coastlineAgentLimit,
            _ => false,
        };
    }
    public void SetOccupation(Point point, bool isOccupied) => this.isOccupied[point.X, point.Y] = isOccupied;
    public bool IsOccupied(Point point) => isOccupied[point.X, point.Y];

    private void CalculateLandAreas(List<Point> startingPoints)
    {
        areas.Clear();
        for (int i = 0; i < startingPoints.Count; i++)
        {
            bool isOnNewIsland = true;
            foreach (var area in areas)
            {
                if (area.ContainsPoint(startingPoints[i]))
                {
                    isOnNewIsland = false;
                    break;
                }
            }

            if (isOnNewIsland)
            {
                LandArea newIsland = new LandArea(startingPoints[i], generator.HeightMap, generator.GetLayerHeight(LayerType.UnderWater));
                if (newIsland.Size > 0)
                    areas.Add(newIsland);
            }
                
        }

        Debug.Log("Number of islands: " + areas.Count);
    }

    private void RecalculateLandAreas()
    {
        for (int i = 0; i < areas.Count; i++)
        {
            areas[i].CalculateLandArea(generator.HeightMap);
            for (int j = areas.Count - 1; j > i; --j)
            {
                if (areas[i].ContainsPoint(areas[j].Start))
                    areas.RemoveAt(j);
            }
        }
        Debug.Log("New number of islands: " + areas.Count);
    }

    public IEnumerator GenerateNewMap()
    {
        agents.Clear();
        areas.Clear();
        isOccupied = new bool[generator.width, generator.height];
        yield return CreateCoastlineAgents();
        yield return CreateBeachAgents();
        RecalculateLandAreas();
        yield return CreateMountainAgents();
        yield return CreateSmoothingAgents();
    }

    private IEnumerator SetAgentsToWork()
    {
        bool allAgentsAreFinished = false;
        while (!allAgentsAreFinished)
        {
            Agent.UpdateGenerator(generator);
            Agent.UpdateManager(this);
            allAgentsAreFinished = true;
            int i = agents.Count;
            while (--i >= 0)
            {
                if (agents[i].IsActive)
                {
                    agents[i].Act();
                    allAgentsAreFinished = false;
                }
            }
            if (visualize)
                yield return new WaitForSeconds(agentWaitTime);
        }
        agents.Clear();
        yield return null;
    }

    private IEnumerator CreateCoastlineAgents()
    {
        status = Status.Coast;
        coastlineAgentTokens = (int)(generator.landPercentage / 100f * generator.width * generator.height);
        coastlineAgentLimit = (int)(generator.width * generator.height / 100 * coastAgentFactor + 1);

        CoastAgent.minimumAdjacentWaterTiles = minimumAdjacentWaterTiles;

        Point point;
        int initialAgents = initialCoastlineAgents < coastlineAgentLimit ? initialCoastlineAgents : coastlineAgentLimit;
        List<Point> startingPoints = new List<Point>();
        for (int i = 0; i < initialAgents; i++)
        {
            do
            {
                point = generator.GetPointInMiddle();
            } while (IsOccupied(point));
            startingPoints.Add(point);
            SetOccupation(point, true);
            agents.Add(new CoastAgent(point, RandomDirection, coastlineAgentTokens / initialAgents, coastAgentReach));
        }
        
        yield return SetAgentsToWork();

        CalculateLandAreas(startingPoints);
    }

    private IEnumerator CreateSmoothingAgents()
    {
        foreach (var area in areas)
            for (int i = 0; i < (int)(area.Size * smoothAgentFactor); i++)
                agents.Add(new SmoothingAgent(Point.Empty, Vector2.zero, smoothTokens, area));
        yield return SetAgentsToWork();
    }

    private IEnumerator CreateBeachAgents()
    {
        status = Status.Land;


        foreach (var area in areas)
        {
            int numberOfAgents = (int)(area.CoastPoints.Count * beachAgentFactor + 1);
            int beachTokens = 10 * area.CoastPoints.Count / numberOfAgents;
            for (int i = 0; i < numberOfAgents; i++)
                agents.Add(new BeachAgent(Point.Empty, Vector2.zero, beachTokens, area, beachAgentHeightFactor, beachWidth));
        }

        yield return SetAgentsToWork();
    }

    private IEnumerator CreateMountainAgents()
    {
        status = Status.Land;

        foreach (var area in areas)
            agents.Add(new MountainAgent(Point.Empty, Vector2.zero, 1, area, mountainWidth, mountainProbability));

        yield return SetAgentsToWork();
    }

    private void OnDrawGizmos()
    {
        if (visualize && generator != null)
        {
            Vector3 offset = new Vector3(generator.size.X * generator.scale.x, 0, generator.size.Y * generator.scale.y) / 2;

            foreach (var agent in agents)
            {
                Gizmos.color = agent.colour;
                Vector3 agentPosition = agent.GetPosition(generator.HeightMap, agentRadius) + transform.position;
                agentPosition -= offset;
                Gizmos.DrawSphere(agentPosition, agentRadius);
            }

            Gizmos.color = UnityEngine.Color.magenta;
            foreach (var area in areas)
            {
                foreach (var point in area.CoastPoints)
                {
                    var position = new Vector3(point.X, generator.GetHeight(point), point.Y);
                    position -= offset;
                    Gizmos.DrawWireSphere(position, 0.5f);
                }
            }
        }
    }
}
