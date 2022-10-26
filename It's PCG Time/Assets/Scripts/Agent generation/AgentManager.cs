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
    public bool forceNonOverlap = false;

    [Header("Smoothing agents fields")]
    [Range(1, 25)]
    public int smoothDistance;
    [Range(1, 25)]
    [SerializeField] private int smoothTokens;
    [Range(0.01f, 0.25f)]
    [SerializeField] private float smoothAgentFactor;

    [Header("Agent information")]
    [SerializeField] private int coastlineAgentTokens;
    [SerializeField] private int coastlineAgentLimit;


    [Header("Other")]
    [SerializeField] private bool visualize = false;
    [Range(0, 0.25f)]
    [SerializeField] private float agentWaitTime;
    [SerializeField] private float agentRadius = 0.25f;

    private List<LandArea> areas = new List<LandArea>();
    private List<Agent> agents = new List<Agent>();
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
                areas.Add(new LandArea(startingPoints[i], generator.HeightMap, generator.GetLayerHeight(AgentGenerator.LayerType.Beach)));
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
        agents = new List<Agent>();
        isOccupied = new bool[generator.width, generator.height];
        yield return CreateCoastlineAgents();
        yield return CreateSmoothingAgents();
        RecalculateLandAreas();
        yield return CreateLandAgents();
        yield return CreateErosionAgents();
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
        yield return null;
    }

    private IEnumerator CreateCoastlineAgents()
    {
        status = Status.Coast;
        agents.Clear();
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
            agents.Add(new CoastAgent(point, RandomDirection, coastlineAgentTokens / initialAgents));
        }
        
        yield return SetAgentsToWork();

        CalculateLandAreas(startingPoints);
    }

    private IEnumerator CreateSmoothingAgents()
    {
        agents.Clear();
        foreach (var area in areas)
            for (int i = 0; i < (int)(area.Size * smoothAgentFactor); i++)
                agents.Add(new SmoothingAgent(Point.Empty, Vector2.zero, smoothTokens, area));
        yield return SetAgentsToWork();
    }

    private IEnumerator CreateLandAgents()
    {
        status = Status.Land;
        agents.Clear();
        yield return SetAgentsToWork();
    }

    private IEnumerator CreateErosionAgents()
    {
        status = Status.Erosion;
        agents.Clear();
        yield return SetAgentsToWork();
    }

    private void OnDrawGizmos()
    {
        if (visualize)
        {
            foreach (var agent in agents)
            {
                Gizmos.color = agent.colour;
                Vector3 agentPosition = agent.GetPosition(generator.HeightMap, agentRadius) + transform.position;
                agentPosition -= new Vector3(generator.size.X * generator.scale.x, 0, generator.size.Y * generator.scale.y) / 2;
                Gizmos.DrawSphere(agentPosition, agentRadius);
            }
        }
    }
}
