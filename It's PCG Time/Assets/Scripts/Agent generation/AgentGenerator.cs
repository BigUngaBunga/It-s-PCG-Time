using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEngine;
using static HeightMapGenerator;

public class AgentGenerator : MonoBehaviour
{
    enum GenerationStatus { Idle, Starting, Coast, Land, Erosion}

    [Header("Map")]
    [SerializeField] private GenerationStatus status;
    [SerializeField] int width;
    [SerializeField] int height;
    private float[,] heightMap;

    [Header("Layers")]
    [SerializeField] float peakHeight;
    [SerializeField] float mountainHeight;
    [SerializeField] float grassHeight;
    [SerializeField] float beachHeight;

    [Header("Agents")]
    private List<Agent> agents;
    [SerializeField] int coastlineAgents;
    [SerializeField] int coastlineAgentTokens;

    [Header("Other")]
    [SerializeField] bool visualize = false;
    [SerializeField] float agentRadius = 0.25f;
    private Interpolator interpolator;
    private Mesh mesh;
    private MeshFilter meshFilter;

    private Vector2 RandomDirection => new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));

    private void Start()
    {
        interpolator = GetComponent<Interpolator>();
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        agents = new List<Agent>();
        StartCoroutine(GenerateTerrain());
    }

    private void Update()
    {
        if (status != GenerationStatus.Idle)
            UpdateMesh();
        else if(Input.GetKeyDown(KeyCode.Space))
            StartCoroutine(GenerateTerrain());

    }

    private IEnumerator GenerateTerrain()
    {
        status = GenerationStatus.Starting;
        heightMap = GetPopulatedHeightMap(width, height, -1f);
        agents = new List<Agent>();
        Agent.boundaries = new Point(heightMap.GetLength(0) -1, heightMap.GetLength(1) -1);

        CreateCoastlineAgents();
        yield return SetAgentsToWork();
        CreateLandAgents();
        yield return SetAgentsToWork();
        CreateErosionAgents();
        yield return SetAgentsToWork();

        heightMap = interpolator.Interpolate(heightMap);
        yield return null;
        status = GenerationStatus.Idle;
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
            allAgentsAreFinished = true;
            foreach (var agent in agents)
                if (agent.IsActive)
                {
                    agent.Act(heightMap);
                    allAgentsAreFinished = false;
                }
            if (visualize)
                yield return null;
        }
        yield return null;
    }

    private void CreateCoastlineAgents()
    {
        status = GenerationStatus.Coast;
        agents.Clear();
        for (int i = 0; i < coastlineAgents; i++)
            agents.Add(new CoastAgent(GetPointOnEdge(), RandomDirection, coastlineAgentTokens));
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

    private Point GetPointOnEdge()
    {
        return Range(0, 3) switch
        {
            0 => new Point(Range(0, width - 1), 0),
            1 => new Point(0, Range(0, height - 1)),
            2 => new Point(Range(0, width - 1), height - 1),
            3 => new Point(width - 1, Range(0, height - 1)),
            _ => new Point(0, 0),
        };

    int Range(int min, int max) => Random.Range(min, max);
    }

    private void OnDrawGizmos()
    {
        if (visualize)
        {
            foreach (var agent in agents)
            {
                Gizmos.color = agent.colour;
                Gizmos.DrawSphere(agent.GetPosition(heightMap, agentRadius), agentRadius);
            }
        }
    }


    private void UpdateMesh()
    {
        mesh.SetVertices(GetVertecies(heightMap, out Vector2[] uv));
        var triangles = GetTriangles(heightMap);
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;
        mesh.uv = uv;
    }

    private Vector3[] GetVertecies(float[,] heightMap, out Vector2[] uv)
    {
        List<Vector3> vertecies = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        Vector2 scale;
        Point size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        scale = new Vector2((float)width / size.X, (float)height/ size.Y);

        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
            {
                var vertex = new Vector3((x - size.X / 2f) * scale.x, heightMap[x, y], (y - size.Y) * scale.y);
                vertecies.Add(vertex);
                uvs.Add(new Vector2(vertex.x, vertex.z));
            }

        uv = uvs.ToArray();

        return vertecies.ToArray();
    }

    private int[] GetTriangles(float[,] heightMap)
    {
        List<Quad> quads = new List<Quad>();
        Point size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        for (int i = 0; i < heightMap.Length; i++)
        {
            if (i % size.Y == size.Y - 1 || i >= heightMap.Length - size.Y - 1)
                continue;

            quads.Add(new Quad(i, i + 1, i + size.Y, i + size.Y + 1));
        }

        List<int> triangles = new List<int>();
        foreach (var quad in quads)
            triangles.AddRange(quad.GetTriangles());
        return triangles.ToArray();
    }

    //TODO gör att quad håller koll på höjd för att kunna skapa submesh baserat på terräng
    //Exempelvis sand nära vattnet, grän högre upp och sten högst upp
    public struct HeightQuad
    {

    }
}


