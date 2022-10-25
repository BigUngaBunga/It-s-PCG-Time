using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using static HeightMapGenerator;

public class AgentGenerator : MonoBehaviour
{
    enum GenerationStatus { Idle, Starting, Coast, Land, Erosion }
    public enum LayerType { UnderWater, Beach, Grass, Mountain, Peak }

    [Header("Map")]
    [SerializeField] private GenerationStatus status;
    [SerializeField] private int width;
    [SerializeField] private int height;
    [Range(0, 100)]
    [SerializeField] private float landPercentage;
    private float[,] heightMap;
    private Point size;
    private Vector2 scale;
    public float[,] HeightMap => heightMap;

    [Header("Layers")]
    [SerializeField] private float peakHeight = 6f;
    [SerializeField] private float mountainHeight = 3f;
    [SerializeField] private float grassHeight = 0.5f;
    [SerializeField] private float beachHeight = 0;
    [SerializeField] private float underWaterHeight = -1f;
    private float[] layerHeights => new float[] { peakHeight, mountainHeight, grassHeight, beachHeight, underWaterHeight };

    [Header("Agents")]
    [SerializeField] private int coastlineAgentTokens;
    [SerializeField] private int coastlineAgentLimit;
    [SerializeField] private int coastlineAgentWalkDistance;
    private List<Agent> agents = new List<Agent>();

    [Header("Other")]
    [SerializeField] private bool visualize = false;
    [SerializeField] private float agentRadius = 0.25f;
    [SerializeField] private bool useCustomSize = false;
    [SerializeField] private Vector2 customSize = Vector2.zero;
    private Interpolator interpolator;
    private MeshGenerator meshGenerator;

    private Vector2 RandomDirection => new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));

    private void Start()
    {
        interpolator = GetComponent<Interpolator>();
        meshGenerator = gameObject.AddComponent<MeshGenerator>();
        Agent.UpdateGenerator(this);
        StartCoroutine(GenerateTerrain());
    }

    #region AgentMethods
    public Point GetPointOnEdge()
    {
        return Range(0, 3) switch
        {
            0 => new Point(Range(0, width - 1), 0),
            1 => new Point(0, Range(0, height - 1)),
            2 => new Point(Range(0, width - 1), height - 1),
            3 => new Point(width - 1, Range(0, height - 1)),
            _ => new Point(0, 0),
        };

        static int Range(int min, int max) => Random.Range(min, max);
    }
    public void AddAgent(Agent agent) => agents.Add(agent);
    public void RemoveAgent(Agent agent) => agents.Remove(agent);
    public bool CanHaveMoreAgents()
    {
        return status switch
        {
            GenerationStatus.Coast => agents.Count < coastlineAgentLimit,
            _ => false,
        };
    }
    public bool IsLand(Point point) => heightMap[point.X, point.Y] >= beachHeight;
    public float GetLayerHeight(LayerType layer)
    {
        return layer switch
        {
            LayerType.Beach => beachHeight,
            LayerType.Grass => grassHeight,
            LayerType.Mountain => mountainHeight,
            LayerType.Peak => peakHeight,
            _ => underWaterHeight,
        };
    }
    #endregion

    private void Update()
    {
        if (status != GenerationStatus.Idle)
            GenerateMesh(true);



        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (status != GenerationStatus.Idle)
                StopAllCoroutines();
            StartCoroutine(GenerateTerrain());
        }
    }

    private void GenerateMesh(bool useSimpleInterpolation)
    {
        float[,] localHeightMap;
        if (useSimpleInterpolation)
            localHeightMap = interpolator.OverrideInterpolate(HeightMap, 1, Interpolator.InterpolationMethod.Bilinear);
        else
            localHeightMap = HeightMap;

        if (useCustomSize)
            meshGenerator.CreateLayeredMesh(localHeightMap, customSize, layerHeights);
        else
            meshGenerator.CreateLayeredMesh(localHeightMap, new Vector2(width, height), layerHeights);
    }

    private void UpdateValues()
    {
        size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        scale = new Vector2((float)width / size.X, (float)height / size.Y);
    }

    #region Generation

    private IEnumerator GenerateTerrain()
    {
        status = GenerationStatus.Starting;
        heightMap = GetPopulatedHeightMap(width, height, -1f);
        agents = new List<Agent>();
        UpdateValues();

        CreateCoastlineAgents();
        yield return SetAgentsToWork();
        CreateLandAgents();
        yield return SetAgentsToWork();
        CreateErosionAgents();
        yield return SetAgentsToWork();

        heightMap = interpolator.Interpolate(heightMap);
        status = GenerationStatus.Idle;
        GenerateMesh(false);
    }

    private float[,] GetPopulatedHeightMap(int width, int height, float defaultValue)
    {
        float[,] heightMap = new float[width, height];
        for (int x = 0; x < heightMap.GetLength(0); x++)
            for (int y = 0; y < heightMap.GetLength(1); y++)
                heightMap[x, y] = defaultValue;
        return heightMap;
    }

    private IEnumerator SetAgentsToWork()
    {
        bool allAgentsAreFinished = false;
        while (!allAgentsAreFinished)
        {
            Agent.UpdateGenerator(this);
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
                yield return new WaitForSeconds(0.1f);
        }
        yield return null;
    }

    private void CreateCoastlineAgents()
    {
        status = GenerationStatus.Coast;
        agents.Clear();
        coastlineAgentTokens = (int)(landPercentage / 100f * width * height);
        coastlineAgentWalkDistance = (width + height) / 4;
        agents.Add(new CoastAgent(GetPointOnEdge(), RandomDirection, coastlineAgentTokens, coastlineAgentWalkDistance));
    }

    private void CreateLandAgents()
    {
        status = GenerationStatus.Land;
        agents.Clear();
    }

    private void CreateErosionAgents()
    {
        status = GenerationStatus.Erosion;
        agents.Clear();
    }

    private void OnDrawGizmos()
    {
        if (visualize)
        {
            foreach (var agent in agents)
            {
                Gizmos.color = agent.colour;
                Vector3 agentPosition = agent.GetPosition(heightMap, agentRadius) + transform.position;
                agentPosition -= new Vector3(size.X * scale.x, 0, size.Y * scale.y) / 2;
                Gizmos.DrawSphere(agentPosition, agentRadius);
            }
        }
    }
    #endregion
}


