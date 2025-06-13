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
    [SerializeField] private Terrain terrain;
    private TerrainData terrainData;
    [SerializeField] private Material walkMaterial, climbMaterial, unableMaterial;

    [Header("Pathfinding")]
    
    [SerializeField] private float maxWalkableSteepness;
    [SerializeField] private float maxClimbableSteepness;
    [SerializeField,Range(10,1000)] private int piorityDistancePrimary = 100;
    [SerializeField,Range(10,1000)] private int piorityDistanceSecondary = 100;
    [SerializeField,Range(10,1000)] private int piorityDistanceTetriary = 100;

    [Header("Debug")]
    [SerializeField] private bool debug_drawMap;
    [SerializeField] private bool debug_drawPath;

    [SerializeField] private Transform topIndicator;
    [SerializeField] private Transform pathParent;
    [SerializeField] private Transform pathPartPrefab;
    [SerializeField] private Transform debugParent;

    float[,] heightMap;
    float[,] steepnessMap;
    Vector2Int[] path;

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
        terrainData = terrain.terrainData;

        terrain.terrainData.terrainLayers = new TerrainLayer[]
        {
            Resources.Load<TerrainLayer>("Grass_A_TerrainLayer"),
            Resources.Load<TerrainLayer>("Pebbles_C_TerrainLayer"),
            Resources.Load<TerrainLayer>("Rock_TerrainLayer"),
            Resources.Load<TerrainLayer>("Snow_TerrainLayer")
        };

        terrainData.heightmapResolution = mapWidth + 1;
        terrainData.size = new Vector3(mapWidth, mapDepth, mapHeight);

        heightMap = GenerateHeightMap();
        terrainData.SetHeights(0, 0, heightMap);
        steepnessMap = GenerateSteepnessMap();

        ApplyTextures();
    }


    void ApplyTextures()
    {
        TerrainData terrainData = terrain.terrainData;
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int x = 0; x < terrainData.alphamapWidth; x++)
        {
            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                float x_01 = (float)x / terrainData.alphamapWidth;
                float y_01 = (float)y / terrainData.alphamapHeight;

                float height = terrainData.GetInterpolatedHeight(x_01, y_01);
                float normalizedHeight = height / terrainData.size.y;

                float steepness = terrainData.GetSteepness(x_01, y_01);
                float flatness = 1f - (steepness / 90f);

                Vector3 normal = terrainData.GetInterpolatedNormal(x_01, y_01);

                float[] splatWeights = new float[terrainData.alphamapLayers];

                float grassWeight = Mathf.Clamp01(flatness * (1f - normalizedHeight)) * 0.3f;
                float gravelWeight = Mathf.Clamp01((1f - flatness) * (1f - normalizedHeight));
                float rockWeight = Mathf.Clamp01((1f - flatness) * normalizedHeight);
                float snowWeight = Mathf.Clamp01(normalizedHeight * Mathf.Clamp01(normal.z)) * 1.7f;

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
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    float[,] GenerateHeightMap()
    {
        float[,] heights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];

        int offsetX = random.Next(-10000, 10000);
        int offsetY = random.Next(-10000, 10000);

        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                heights[x, y] = GenerateHeightAtPoint(x, y, offsetX, offsetY);
            }
        }

        return heights;
    }

    float[,] GenerateSteepnessMap()
    {
        float[,] steepnesses = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                float xCoord = (float)x / (terrainData.heightmapResolution - 1);
                float yCoord = (float)y / (terrainData.heightmapResolution - 1);
                steepnesses[x, y] = terrain.terrainData.GetSteepness(xCoord, yCoord);
            }
        }
        return steepnesses;
    }

    float GenerateHeightAtPoint(int x, int y, int offsetX, int offsetY)
    {
        return RidgedPerlinNoise(x, y, offsetX, offsetY) * GetFalloffValue(x,y);
    }

    float RidgedPerlinNoise(float x, float y, float offsetX, float offsetY)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float xCoord = ((float)x / terrainData.heightmapResolution * scale + offsetX) * frequency;
            float yCoord = ((float)y / terrainData.heightmapResolution * scale + offsetY) * frequency;
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
        float nx = (float)x / (terrainData.heightmapResolution - 1) * 2f - 1f;
        float ny = (float)y / (terrainData.heightmapResolution - 1) * 2f - 1f;

        float distance = GetNormalizedDistanceFromCenter(x, y);

        distance = Mathf.Clamp01(distance);

        float falloff = Mathf.Pow(distance, falloffDistancePower) / (Mathf.Pow(distance, falloffDistancePower) + Mathf.Pow(falloffScale - falloffScale * distance, falloffDistancePower));

        return Mathf.Clamp01(1f - falloff);
    }

    float GetNormalizedDistanceFromCenter(int x, int y)
    {
        float centerX = (terrain.terrainData.heightmapResolution - 1) / 2f;
        float centerY = (terrain.terrainData.heightmapResolution - 1) / 2f;

        float dx = x - centerX;
        float dy = y - centerY;

        float distance = Mathf.Sqrt(dx * dx + dy * dy);

        float maxDistance = Mathf.Sqrt(centerX * centerX + centerY * centerY);

        return distance / maxDistance;
    }

    #endregion

    #region Pathfinding

    void FindPath()
    {
        Vector2Int end = new Vector2Int(terrainData.heightmapResolution / 2, terrainData.heightmapResolution / 2);
        //end = new Vector2Int(92, 22);
        Vector2Int start = FindHighestPeak();

        topIndicator.position = ConvertPointToWorldPosition(end);

        Vector2Int[,] before = Dijkstra(start, end);
        path = Backtrack(start, end, before);

        ClearParents();
        if (debug_drawMap) DebugDraw(before);
        if (debug_drawPath) DrawPath(before);
    }

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

    int[,] colors;
    Vector2Int[,] Dijkstra(Vector2Int start, Vector2Int end)
    {
        colors = new int[terrainData.heightmapResolution, terrainData.heightmapResolution];
        bool[,] visited = new bool[terrainData.heightmapResolution, terrainData.heightmapResolution];
        int[,] dist = new int[terrainData.heightmapResolution, terrainData.heightmapResolution];
        Vector2Int[,] before = new Vector2Int[terrainData.heightmapResolution, terrainData.heightmapResolution];

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
            print(before[top.x, top.y].x + " " + before[top.x, top.y].y + " --> " + top.x + " " + top.y + " | s = " + colors[top.x,top.y]);

            for (int i = 0; i < directions.Length; i++)
            {
               Vector2Int newPoint = top + directions[i];
                int color = 0;

                if (newPoint.x < 0 || newPoint.x >= terrainData.heightmapResolution || newPoint.y < 0 || newPoint.y >= terrainData.heightmapResolution)
                    continue;

                if (visited[newPoint.x, newPoint.y])
                    continue;

                float newPointHeightDiffrence = math.abs(ConvertPointToWorldPosition(newPoint).y - ConvertPointToWorldPosition(top).y);
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

    /*
    bool[,] visited;
    Vector2Int[,] before;
    Vector2Int[] BFS(Vector2Int start, Vector2Int end)
    {
        visited = new bool[terrainData.heightmapResolution, terrainData.heightmapResolution];
        before = new Vector2Int[terrainData.heightmapResolution, terrainData.heightmapResolution];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(start);
        visited[start.x, start.y] = true;
        before[start.x, start.y] = start;

        Vector2Int lastKnown = start;

        while (queue.Count > 0)
        {
            Vector2Int point = queue.Dequeue();
            lastKnown = point;
            print(before[point.x, point.y].x + " " + before[point.x, point.y].y + " --> " + point.x + " " + point.y + " | s = " + steepnessMap[point.x, point.y]);

            if (point == end)
            {
                print("backtracking from " + point.x + " " + point.y + " to " + start.x + " " + start.y);
                return Backtrack(start, point);
            }

            float heightDiffrence = math.abs(heightMap[point.x, point.y] - heightMap[before[point.x, point.y].x, before[point.x, point.y].y]);
            if (heightDiffrence > maxWalkableSteepness && heightDiffrence <= maxClimbableSteepness)
            {
                //SZUKAJ NAJBLIZSZEGO CHODZENIA

                //na indeksie zero poczatek, na ostatnim chodznie
                Vector2Int[] PathToClosestWalk = PathToClosestWalking(point);
                if (PathToClosestWalk.Length > 0)
                {
                    //queue.Clear();
                    print("FOUND PATH TO CLOSEST WALK");
                    queue.Enqueue(PathToClosestWalk[PathToClosestWalk.Length - 1]);
                    continue;
                }
            }

            int aggg = 0;
            int visi = 0;

            //PIORYTET W DODAWANIU DO KOLEJKI DLA CHODZENIA
            //najpierw dodawac chodzenia
            for (int i = 0; i < directions.Length; i++)
            {
               Vector2Int newPoint = point + directions[i];

                if (newPoint.x < 0 || newPoint.x >= terrainData.heightmapResolution || newPoint.y < 0 || newPoint.y >= terrainData.heightmapResolution)
               {
                    aggg++;
                    continue;
               }

                float newPointHeightDiffrence = math.abs(heightMap[newPoint.x, newPoint.y] - heightMap[point.x, point.y]);

                if (newPointHeightDiffrence > maxWalkableSteepness) { continue; }

                if (newPointHeightDiffrence > maxClimbableSteepness) { aggg++; continue; }

                if (visited[newPoint.x, newPoint.y] == false)
                {
                    visited[newPoint.x, newPoint.y] = true;
                    before[newPoint.x, newPoint.y] = point;

                    queue.Enqueue(point + directions[i]);
                }
                else
                {
                    visi++;
                    aggg++;
                }
            }

            
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int newPoint = point + directions[i];

                if (newPoint.x < 0 || newPoint.x >= terrainData.heightmapResolution || newPoint.y < 0 || newPoint.y >= terrainData.heightmapResolution)
                {
                    aggg++;
                    continue;
                }
                float newPointHeightDiffrence = math.abs(heightMap[newPoint.x, newPoint.y] - heightMap[point.x, point.y]);


                if (newPointHeightDiffrence <= maxWalkableSteepness) { continue; }

                if (newPointHeightDiffrence > maxClimbableSteepness) { aggg++; continue; }

                if (visited[newPoint.x, newPoint.y] == false)
                {
                    visited[newPoint.x, newPoint.y] = true;
                    before[newPoint.x, newPoint.y] = point;

                    queue.Enqueue(point + directions[i]);
                }
                else
                {
                    visi++;
                    aggg++;
                }
            }

            if (aggg == 4)
            {
                print("CZTERY " + visi.ToString() + " " + queue.Count);
            }
        }


        return Backtrack(start, lastKnown);
    }

    Vector2Int[] PathToClosestWalking(Vector2Int start)
    {
        List <Vector2Int> path = new List<Vector2Int>();

       Queue<Vector2Int> queue = new Queue<Vector2Int> ();

        print("looking for walking from " + start.x + ", " + start.y);

        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int point = queue.Dequeue();
            float heightDiffrence = math.abs(heightMap[point.x, point.y] - heightMap[before[point.x, point.y].x, before[point.x, point.y].y]);

            if (heightDiffrence < maxWalkableSteepness)
            {
                print("found walking at " + point.x + " " + point.y);
                return Backtrack(start, point);
            }

            //PIORYTET DLA SCHODZENIA NIZEJ SZUKAJAC CHODZENIA  ?
            //najpierw dodawac te ktore schodza jak najnizej    ? 

            //ZROBIC ABY SZUKAC NAJBLIZSZEGO SCHODZENIA
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int newPoint = point + directions[i];

                if (newPoint.x < 0 || newPoint.x >= terrainData.heightmapResolution || newPoint.y < 0 || newPoint.y >= terrainData.heightmapResolution)
                {
                    continue;
                }

                float newPointHeightDiffrence = math.abs(heightMap[newPoint.x, newPoint.y] - heightMap[point.x, point.y]);


                if (newPointHeightDiffrence > maxClimbableSteepness) { continue; }

                if (visited[newPoint.x, newPoint.y] == false)
                {
                    visited[newPoint.x, newPoint.y] = true;
                    before[newPoint.x, newPoint.y] = point;

                    queue.Enqueue(point + directions[i]);
                }
            }
        }
        

        return path.ToArray();
    }

    Vector2Int[] Backtrack(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        int b = 0;
        try
        {


        Vector2Int currentPoint = end;

        path.Add(end);
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

        }
        catch (Exception e)
        {
            print(path.Count);
            print(b);
        }
        return path.ToArray();

    }
    */

    Vector2Int FindHighestPeak()
    {
        float maxVal = -1;

        List<Vector2Int> peaks = new List<Vector2Int>();

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                if (heightMap[x, y] >= maxVal)
                {
                    maxVal = heightMap[x, y];

                    peaks.Clear();
                }

                if (heightMap[x,y]  == maxVal)
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
        float xCoord = (float)point.x / (terrainData.heightmapResolution - 1);
        float yCoord = (float)point.y / (terrainData.heightmapResolution - 1);

        return new Vector3(
            terrain.transform.position.x + xCoord * terrainData.size.x,
            terrain.transform.position.y + terrain.terrainData.GetInterpolatedHeight(xCoord, yCoord),
            terrain.transform.position.z + yCoord * terrainData.size.z
        );
    }

    public void DebugDraw(Vector2Int[,] before)
    {
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                Vector2Int point = new Vector2Int(x, y);

                Transform obj = Instantiate(pathPartPrefab, ConvertPointToWorldPosition(point), Quaternion.identity, debugParent);
                obj.name = point.ToString() + " | h: " + heightMap[x,y];

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

    public void DrawPath(Vector2Int[,] before)
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
