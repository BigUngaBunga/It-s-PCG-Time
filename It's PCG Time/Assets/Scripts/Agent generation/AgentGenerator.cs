using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using static HeightMapGenerator;

public class AgentGenerator : MonoBehaviour
{
    public enum GenerationStatus { Idle, Starting, Coast, Land }
    public enum LayerType { UnderWater, Beach, Grass, Mountain, Peak }

    [Header("Map")]
    [SerializeField] private string seed;
    [SerializeField] private bool useRandomSeed;
    public int width;
    public int height;
    [Range(0, 100)]
    public float landPercentage;
    [Range(3.3f, 33)]
    [SerializeField] private float agentMiddleDistance;
    private float[,] heightMap;
    [DoNotSerialize] public Point size;
    [DoNotSerialize] public Vector2 scale;
    public float[,] HeightMap => heightMap;

    [Header("Layers")]
    [SerializeField] private float peakHeight = 6f;
    [SerializeField] private float mountainHeight = 3f;
    [SerializeField] private float grassHeight = 0.5f;
    [SerializeField] private float beachHeight = 0;
    [SerializeField] private float underWaterHeight = -1f;
    private float[] LayerHeights => new float[] { peakHeight, mountainHeight, grassHeight, beachHeight, underWaterHeight };

    [Header("Other")]
    [SerializeField] private bool useCustomSize = false;
    [SerializeField] private Vector2 customSize = Vector2.zero;
    [SerializeField] private LandArea.WaterRemovalArea waterRemoval;
    private Interpolator interpolator;
    private MeshGenerator meshGenerator;
    private AgentManager agentManager;
    public int SeedValue { get; private set; }

    private void Start()
    {
        interpolator = GetComponent<Interpolator>();
        agentManager = GetComponent<AgentManager>();
        meshGenerator = GetComponent<MeshGenerator>();
        Agent.UpdateGenerator(this);
        StartCoroutine(GenerateTerrain());
    }

    //TODO indexera de olika landmassorna
    //TODO fyll i små hål innan indexeringen

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
    public Point GetPointInMiddle()
    {
        int widthOffset = (int)(width * agentMiddleDistance / 100f);
        int heightOffset = (int)(height * agentMiddleDistance / 100f);
        return new Point(Range(width/2 - widthOffset, width/2 + widthOffset), Range(height/2 - heightOffset, height/2 + heightOffset));

        static int Range(int min, int max) => Random.Range(min, max);
    }

    public float GetHeight(Point point) => HeightMap[point.X, point.Y];
    public void SetHeight(Point point, float height) => HeightMap[point.X, point.Y] = height;
    public void SetHeight(Point point, LayerType layerType) => SetHeight(point, GetLayerHeight(layerType));
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
        if (agentManager.status != GenerationStatus.Idle)
            GenerateMesh(true);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (agentManager.status != GenerationStatus.Idle)
                StopAllCoroutines();
            StartCoroutine(GenerateTerrain());
        }
    }

    private void GenerateMesh(bool useSimpleInterpolation)
    {
        float[,] localHeightMap;
        if (useSimpleInterpolation)
            localHeightMap = interpolator.OverrideInterpolate(HeightMap, 1, Interpolator.InterpolationMethod.None);
        else
            localHeightMap = HeightMap;

        if (useCustomSize)
            meshGenerator.CreateLayeredMesh(localHeightMap, customSize, LayerHeights);
        else
            meshGenerator.CreateLayeredMesh(localHeightMap, new Vector2(width, height), LayerHeights);
    }

    private void UpdateValues()
    {
        size = new Point(heightMap.GetLength(0), heightMap.GetLength(1));
        scale = new Vector2((float)width / size.X, (float)height / size.Y);
        LandArea.removalArea = waterRemoval;
    }

    #region Generation

    private void SeedRandom()
    {
        if (useRandomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue).ToString();

        if (!int.TryParse(seed, out int seedValue))
            seedValue = seed.GetHashCode();
        SeedValue = seedValue;
        Random.InitState(seedValue);
    }

    private IEnumerator GenerateTerrain()
    {
        SeedRandom();
        agentManager.status = GenerationStatus.Starting;
        heightMap = GetPopulatedHeightMap(width, height, GetLayerHeight(LayerType.UnderWater));
        UpdateValues();

        AgentManager.generator = this;
        yield return agentManager.GenerateNewMap();

        agentManager.status = GenerationStatus.Idle;
        PrintHeightMap();
        heightMap = interpolator.Interpolate(heightMap);
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
    #endregion


    public void PrintHeightMap()
    {
        StringBuilder stringBuilder = new StringBuilder();
        float waterHeight = GetLayerHeight(LayerType.UnderWater);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (waterHeight >= heightMap[x, y])
                    stringBuilder.Append('-');
                else
                    stringBuilder.Append('X');
            }
            stringBuilder.AppendLine();
        }

        Debug.Log(stringBuilder.ToString());
    }
}


