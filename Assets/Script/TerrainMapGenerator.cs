using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.LightTransport;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEditor.ShaderData;

public class TerrainMapGenerator : MonoBehaviour
{
    private System.Random random;

    [Header("Settings")]
    [SerializeField] private int mapWidth;
    [SerializeField] private int mapHeight;
    [SerializeField] private int mapDepth;
    [SerializeField] private float scale;
    [SerializeField] private int seed;

    [Space(10)]
    [SerializeField] private int octaves = 4;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2.0f;

    [Header("Falloff")]
    [SerializeField] private float falloffDistancePower = 3f;
    [SerializeField] private float falloffScale = 2.2f;

    [Header("Rendering")]
    private Terrain mainTerrain;

    [Serializable]
    public struct TerrainPart
    {
        public Terrain terrain;
        public Vector2Int direction;
    }

    [SerializeField] private TerrainPart[] terrainParts;

    [SerializeField] private Material walkMaterial, climbMaterial, unableMaterial;

    [Header("Pathfinding")]
    
    [SerializeField] private float maxWalkableSteepness;
    [SerializeField] private float maxClimbableSteepness;
    [SerializeField,Range(0,1000)] private int piorityDistancePrimary = 100;
    [SerializeField,Range(0 ,1000)] private int piorityDistanceSecondary = 100;
    [SerializeField,Range(0,1000)] private int piorityDistanceTetriary = 100;

    [Header("Debug")]
    [SerializeField] private bool debug_drawMap;
    [SerializeField] private bool debug_drawPath;

    [SerializeField] private Transform topIndicator;
    [SerializeField] private Transform pathParent;
    [SerializeField] private Transform pathPartPrefab;
    [SerializeField] private Transform debugParent;

    float[,] mainPartHeightMap;
    private int heightmapResolution;

    Vector2Int[] directions =
    {
            new Vector2Int(0,1),
            new Vector2Int(0,-1),
            new Vector2Int(1,0),
            new Vector2Int(-1,0),

            new Vector2Int(-1,-1),
            new Vector2Int(1,1),
            new Vector2Int(-1,-1),
            new Vector2Int(1,-1),
    };

    void Start()
    {
        //InitializeMap();
    }

    void InitializeMap()
    {
        random = new System.Random(seed);
        Generate();

        if (!EditorApplication.isPlaying) return;

        FindPath();
    }

    #region Terrain Generator
    void Generate()
    {
        heightmapResolution = mapWidth + 1;
        mainTerrain = terrainParts[0].terrain;

        Vector2Int offset = new Vector2Int(random.Next(-10000, 10000), random.Next(-10000, 10000));
        for (int i = 0; i < terrainParts.Length; i++)
        {
            GeneratePart(terrainParts[i].terrain, terrainParts[i].direction, offset);
        }
    }

    void GeneratePart(Terrain terrain, Vector2Int direction, Vector2Int offset)
    {
        bool main = mainTerrain == terrain;

        float[,] heightMap = GenerateHeightMap(direction, offset, main);

        if (main) mainPartHeightMap = heightMap;

        terrain.terrainData.terrainLayers = new TerrainLayer[]
        {
            Resources.Load<TerrainLayer>("Grass_A_TerrainLayer"),
            Resources.Load<TerrainLayer>("Pebbles_C_TerrainLayer"),
            Resources.Load<TerrainLayer>("Rock_TerrainLayer"),
            Resources.Load<TerrainLayer>("Snow_TerrainLayer")
        };

        terrain.terrainData.heightmapResolution = heightmapResolution;
        terrain.terrainData.size = new Vector3(mapWidth, mapDepth, mapHeight);
        terrain.terrainData.SetHeights(0, 0, heightMap);

        terrain.transform.position = new Vector3(
            mapWidth * direction.x,
            0,
            mapHeight * direction.y
        );

        ApplyTextures(terrain.terrainData, direction);
    }

    void ApplyTextures(TerrainData terrainData, Vector2Int direction)
    {
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int x = 0; x < terrainData.alphamapWidth; x++)
        {
            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                float x_01 = (float)x / terrainData.alphamapWidth;
                float y_01 = (float)y / terrainData.alphamapHeight;

                float height = terrainData.GetInterpolatedHeight(x_01, y_01) / terrainData.size.y;
                float stepness = terrainData.GetSteepness(x_01, y_01) / 90f;
                Vector3 normal = terrainData.GetInterpolatedNormal(x_01, y_01);

                float[] splatWeights = new float[terrainData.alphamapLayers];

                float grassWeight = 0; //nisko plasko
                float gravelWeight = 0; //nisko stromo
                float rockWeight = stepness; //wysoko stromo
                float snowWeight = (1f - stepness); //wysoko plasko

                splatWeights[0] = grassWeight;
                splatWeights[1] = gravelWeight;
                splatWeights[2] = rockWeight;
                splatWeights[3] = snowWeight;

                float total = splatWeights.Sum();
                if (total == 0f) total = 1f;

                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {
                    splatmapData[x, y, i] = splatWeights[i] / total;
                }

                int featherWidth = 8;
                float edgeFactor = 1f;

                if (x < featherWidth)
                    edgeFactor *= (float)x / featherWidth;
                else if (x >= terrainData.alphamapWidth - featherWidth)
                    edgeFactor *= (float)(terrainData.alphamapWidth - 1 - x) / featherWidth;

                if (y < featherWidth)
                    edgeFactor *= (float)y / featherWidth;
                else if (y >= terrainData.alphamapWidth - featherWidth)
                    edgeFactor *= (float)(terrainData.alphamapWidth - 1 - y) / featherWidth;

                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {
                    splatWeights[i] = Mathf.Lerp(0.5f, splatWeights[i], edgeFactor);
                    splatmapData[x, y, i] = splatWeights[i];
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    float[,] GenerateHeightMap(Vector2Int direction, Vector2Int offset, bool main)
    {
        float[,] heights = new float[heightmapResolution, heightmapResolution];

        for (int x = 0; x < heightmapResolution; x++)
        {
            for (int y = 0; y < heightmapResolution; y++)
            {
                heights[x, y] = GenerateHeightAtPoint(x + (mapWidth*direction.y), y + (mapWidth * direction.x), offset.x, offset.y, main);
            }
        }
        
        return heights;
    }

    float GenerateHeightAtPoint(int x, int y, int offsetX, int offsetY, bool main)
    {
        float ridgedPerlinNoiseValue = RidgedPerlinNoise(x, y, offsetX, offsetY);

        if(main) ridgedPerlinNoiseValue *= GetFalloffValue(x, y);

        return ridgedPerlinNoiseValue;
    }

    float RidgedPerlinNoise(float x, float y, float offsetX, float offsetY)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float xCoord = ((float)x / heightmapResolution * scale + offsetX) * frequency;
            float yCoord = ((float)y / heightmapResolution * scale + offsetY) * frequency;
            float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);

            float ridgedValue = 1f - Mathf.Abs(2f * perlinValue - 1f);

            ridgedValue *= ridgedValue;

            total += ridgedValue * amplitude;

            maxAmplitude += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxAmplitude;
    }

    float GetFalloffValue(int x, int y)
    {
        float distance = GetNormalizedDistanceFromCenter(x, y);

        float falloff = distance - 0.1f;

        if (falloff > 1f) falloff = 1f;
        else if (falloff < 0f) falloff = 0f;

        return falloff;
    }

    float GetNormalizedDistanceFromCenter(int x, int y)
    {
        float centerX = (heightmapResolution - 1f) / 2f;
        float centerY = (heightmapResolution - 1f) / 2f;

        float dx = x - centerX;
        float dy = y - centerY;

        float distance = Mathf.Sqrt(dx * dx + dy * dy);

        float maxDistance = heightmapResolution/5;

        return distance / maxDistance;
    }

    #endregion

    #region Pathfinding

    void FindPath()
    {
        Vector2Int end = new Vector2Int(heightmapResolution / 2, heightmapResolution / 2);
        //end = FindLowestPeak();
        Vector2Int start = FindHighestPeak();

        topIndicator.position = ConvertPointToWorldPosition(end);

        Vector2Int[,] before = Dijkstra(start, end);
        //Vector2Int[] path = Backtrack(start, end, before);
        Vector2Int[] path = FindFinalPath(Backtrack(start, end, before));

        ClearParents();
        if (debug_drawMap) DebugDraw(before);
        if (debug_drawPath) DrawPath(path);
    }



    int[,] colors;
    Vector2Int[,] Dijkstra(Vector2Int start, Vector2Int end)
    {
        colors = new int[heightmapResolution, heightmapResolution];
        bool[,] visited = new bool[heightmapResolution, heightmapResolution];
        int[,] dist = new int[heightmapResolution, heightmapResolution];
        Vector2Int[,] before = new Vector2Int[heightmapResolution, heightmapResolution];

        var pq = new Utils.PriorityQueue<Vector3Int, int>();

        before[start.x, start.y] = start;
        dist[start.x, start.y] = 0;
        colors[start.x, start.y] = 0;

        pq.Enqueue(new Vector3Int(start.x,start.y, 0), 0);
        while (pq.Count != 0) {
            Vector3Int tmp = pq.Dequeue();
            Vector2Int top = new Vector2Int(tmp.x, tmp.y);
            if (visited[top.x, top.y])
                continue;
                
            visited[top.x, top.y] = true;
            colors[top.x, top.y] = tmp.z;
            dist[top.x, top.y] = dist[before[top.x, top.y].x, before[top.x, top.y].y] + 1;
            //print(before[top.x, top.y].x + " " + before[top.x, top.y].y + " --> " + top.x + " " + top.y + " | s = " + colors[top.x,top.y]);

            if (top == end)
                return before;

            for (int i = 0; i < directions.Length; i++)
            {
               Vector2Int newPoint = top + directions[i];
                int color = 0;

                if (newPoint.x < 0 || newPoint.x >= heightmapResolution || newPoint.y < 0 || newPoint.y >= heightmapResolution)
                    continue;

                if (visited[newPoint.x, newPoint.y])
                    continue;

                float newPointHeightDiffrence = math.abs(ConvertPointToWorldPosition(newPoint).y - ConvertPointToWorldPosition(top).y);
                //int priority = dist[top.x, top.y] + (int)((piorityDistancePrimary * (1 - mainTerrain.terrainData.GetSteepness(ConvertPointToWorldPosition(newPoint).x, ConvertPointToWorldPosition(newPoint).y))/90));
                int priority = dist[top.x, top.y] + piorityDistancePrimary;

                before[newPoint.x, newPoint.y] = top;

                if (newPointHeightDiffrence > maxWalkableSteepness)
                {
                    priority += piorityDistanceSecondary;
                    color = 1;
                }
                if (newPointHeightDiffrence > maxClimbableSteepness)
                {
                    priority += piorityDistanceTetriary;
                    color = 2;
                }


                pq.Enqueue(new Vector3Int(newPoint.x,newPoint.y,color), priority);
            }
        }
        
        return before;
    }

    Vector2Int[] FindFinalPath(Vector2Int[] raw)
    {
        bool[,] visited = new bool[heightmapResolution, heightmapResolution];
        Vector2Int[,] before = new Vector2Int[heightmapResolution, heightmapResolution];
        bool[,] mapToSearch = new bool[heightmapResolution, heightmapResolution];
        for (int i = 0; i < raw.Length; i++)
        {
            mapToSearch[raw[i].x, raw[i].y] = true;
        }

        Vector2Int start = raw[0];
        Vector2Int end = raw[raw.Length - 1];

        before[start.x,start.y] = start;
        visited[start.x, start.y] = true;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while(queue.Count > 0) 
        {
            Vector2Int point = queue.Dequeue();

            if (point == end)
                break;

            //print(before[point.x, point.y].x + " " + before[point.x, point.y].y + " --> " + point.x + " " + point.y);

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int newPoint = point + directions[i];

                if (newPoint.x < 0 || newPoint.x >= heightmapResolution || newPoint.y < 0 || newPoint.y >= heightmapResolution)
                    continue;

                if (mapToSearch[newPoint.x, newPoint.y] == false)
                    continue;

                if (visited[newPoint.x, newPoint.y])
                    continue;

                before[newPoint.x, newPoint.y] = point;
                visited[newPoint.x, newPoint.y] = true;
                queue.Enqueue(newPoint);
            }
        }

        return Backtrack(start,end,before);
    }

    Vector2Int[] Backtrack(Vector2Int start, Vector2Int end, Vector2Int[,] before)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        path.Add(end);

        Vector2Int currentPoint = end;

        currentPoint = before[currentPoint.x, currentPoint.y];
        Vector2Int lastPoint = currentPoint;

        do
        {
            path.Add(currentPoint);
            lastPoint = currentPoint;
            currentPoint = before[currentPoint.x, currentPoint.y];
        }
        while (currentPoint != lastPoint);

        path.Reverse();

        return path.ToArray();
    }
   
    Vector2Int FindHighestPeak()
    {
        float maxVal = -1;

        List<Vector2Int> peaks = new List<Vector2Int>();

        for (int y = 0; y < heightmapResolution; y++)
        {
            for (int x = 0; x < heightmapResolution; x++)
            {
                if (mainPartHeightMap[x, y] >= maxVal)
                {
                    maxVal = mainPartHeightMap[x, y];

                    peaks.Clear();
                }

                if (mainPartHeightMap[x,y]  == maxVal)
                {
                    peaks.Add(new Vector2Int(y,x));
                }
            }
        }

        if (peaks.Count == 0) return new Vector2Int(0,0);

        return peaks[random.Next(0, peaks.Count)];
    }
    
    Vector2Int FindLowestPeak()
    {
        float minVal = 2;

        List<Vector2Int> peaks = new List<Vector2Int>();

        for (int y = 0; y < heightmapResolution; y++)
        {
            for (int x = 0; x < heightmapResolution; x++)
            {
                if (mainPartHeightMap[x, y] < minVal)
                {
                    minVal = mainPartHeightMap[x, y];

                    peaks.Clear();
                }

                if (mainPartHeightMap[x,y]  == minVal)
                {
                    peaks.Add(new Vector2Int(y,x));
                }
            }
        }

        if (peaks.Count == 0) return new Vector2Int(0,0);

        return peaks[random.Next(0, peaks.Count)];
    }

    Vector3 ConvertPointToWorldPosition(Vector2Int point)
    {
        float xCoord = (float)point.x / (heightmapResolution - 1);
        float yCoord = (float)point.y / (heightmapResolution - 1);

        return new Vector3(
            mainTerrain.transform.position.x + xCoord * mainTerrain.terrainData.size.x,
            mainTerrain.transform.position.y + mainTerrain.terrainData.GetInterpolatedHeight(xCoord, yCoord),
            mainTerrain.transform.position.z + yCoord * mainTerrain.terrainData.size.z
        );
    }

    public void DebugDraw(Vector2Int[,] before)
    {
        for (int y = 0; y < heightmapResolution; y++)
        {
            for (int x = 0; x < heightmapResolution; x++)
            {
                Vector2Int point = new Vector2Int(x, y);

                Transform obj = Instantiate(pathPartPrefab, ConvertPointToWorldPosition(point), Quaternion.identity, debugParent);
                obj.name = point.ToString() + " | h: " + mainPartHeightMap[x,y];

                if (colors[point.x,point.y] == 0)
                {
                    obj.GetComponent<MeshRenderer>().material = walkMaterial;
                }
                else if (colors[point.x, point.y] == 1)
                {
                    obj.GetComponent<MeshRenderer>().material = climbMaterial;
                }
            }
        }
    }

    public void DrawPath(Vector2Int[] path)
    {
        for (int i = 0; i < path.Length; i++)
        {
            Transform obj = Instantiate(pathPartPrefab, ConvertPointToWorldPosition(path[i]), Quaternion.identity, pathParent);
            obj.name = path[i].ToString();
            obj.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            if (colors[path[i].x, path[i].y] == 0)
            {
                obj.GetComponent<MeshRenderer>().material = walkMaterial;
            }
            else if (colors[path[i].x, path[i].y] == 1)
            {
                obj.GetComponent<MeshRenderer>().material = climbMaterial;
            }
        }
    }

    public void ClearParents()
    {
        foreach (Transform child in pathParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in debugParent)
        {
            Destroy(child.gameObject);
        }
    }

    #endregion

    public void OnValidate()
    {
        if (octaves <= 0) octaves = 1;
        
        InitializeMap();
    }
}
