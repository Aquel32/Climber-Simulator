using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainMapGenerator : MonoBehaviour
{
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

    [Header("Rendering")]
    [SerializeField] private Terrain terrain;

    private System.Random random;

    // Scale factors for tweaking
    public float grassScale = 0.4f;  // Reduce grass influence to 40%
    public float snowScale = 1.6f;   // Increase snow influence to 160%

    public Vector3 heighestPoint;

    void Start()
    {
        random = new System.Random(seed);

        Generate();
    }

    public void Generate()
    {
        TerrainData terrainData = terrain.terrainData;

        terrain.terrainData.terrainLayers = new TerrainLayer[]
        {
            Resources.Load<TerrainLayer>("Grass_A_TerrainLayer"),
            Resources.Load<TerrainLayer>("Pebbles_C_TerrainLayer"),
            Resources.Load<TerrainLayer>("Rock_TerrainLayer"),
            Resources.Load<TerrainLayer>("Snow_TerrainLayer")
        };

        terrainData.heightmapResolution = mapWidth + 1;
        terrainData.size = new Vector3(mapWidth, mapDepth, mapHeight);
        terrainData.SetHeights(0, 0, GenerateHeightMap());

        ApplyTextures();
    }

    void ApplyTextures()
    {
        TerrainData terrainData = terrain.terrainData;
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float x_01 = (float)x / terrainData.alphamapWidth;
                float y_01 = (float)y / terrainData.alphamapHeight;

                float height = terrainData.GetInterpolatedHeight(x_01, y_01);
                float normalizedHeight = height / terrainData.size.y;

                float steepness = terrainData.GetSteepness(x_01, y_01);
                float flatness = 1f - (steepness / 90f);

                Vector3 normal = terrainData.GetInterpolatedNormal(x_01, y_01);

                float[] splatWeights = new float[terrainData.alphamapLayers];

                

                // Grass: flat and low, reduced
                float grassWeight = Mathf.Clamp01(flatness * (1f - normalizedHeight)) * grassScale;

                // Gravel: steep and low, keep unchanged
                float gravelWeight = Mathf.Clamp01((1f - flatness) * (1f - normalizedHeight));

                // Rock: steep and high, keep unchanged
                float rockWeight = Mathf.Clamp01((1f - flatness) * normalizedHeight);

                // Snow: high and facing north, increased
                float snowWeight = Mathf.Clamp01(normalizedHeight * Mathf.Clamp01(normal.z)) * snowScale;

                splatWeights[0] = grassWeight;
                splatWeights[1] = gravelWeight;
                splatWeights[2] = rockWeight;
                splatWeights[3] = snowWeight;

                // Normalize weights so they sum to 1
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
        float[,] heights = new float[terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution];

        float offsetX = random.Next(-10000, 10000);
        float offsetY = random.Next(-10000, 10000);

        for (int x = 0; x < terrain.terrainData.heightmapResolution; x++)
        {
            for (int y = 0; y < terrain.terrainData.heightmapResolution; y++)
            {
                float baseHeight = RidgedPerlinNoise(x, y, offsetX, offsetY);
                float falloffValue = GetFalloffValue(x, y);

                heights[x, y] = baseHeight * falloffValue;  // Apply falloff here

                if(heighestPoint != null)
                {
                    if (heights[x,y] > heighestPoint.y)
                    {
                        heighestPoint = new Vector3(x, heights[x, y], y);
                    }
                }
            }
        }

        return heights;
    }

    public float RidgedPerlinNoise(float x, float y, float offsetX, float offsetY)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            // Sample Perlin noise in [0,1]
            float xCoord = ((float)x / mapWidth * scale + offsetX) * frequency;
            float yCoord = ((float)y / mapHeight * scale + offsetY) * frequency;
            float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);

            // Convert Perlin noise to ridged noise:
            // Peaks near 1, valleys near 0
            float ridgedValue = 1f - Mathf.Abs(2f * perlinValue - 1f);

            // Square to sharpen ridges (optional but common)
            ridgedValue *= ridgedValue;

            total += ridgedValue * amplitude;

            maxAmplitude += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        // Normalize result to [0,1]
        return total / maxAmplitude;
    }

    public float fa = 3f;
    public float fb = 2.2f;

    float GetFalloffValue(int x, int y)
    {
        // Normalize x and y to range [-1, 1]
        float nx = (float)x / (mapWidth - 1) * 2f - 1f;
        float ny = (float)y / (mapHeight - 1) * 2f - 1f;

        // Calculate Euclidean distance from center (0,0)

        float distance = GetNormalizedDistanceFromCenter(x, y);

        // Clamp distance to [0,1], where 0 is center, 1 or more is edge or outside circle
        distance = Mathf.Clamp01(distance);

        // Smooth falloff curve using the same formula for softness
        float falloff = Mathf.Pow(distance, fa) / (Mathf.Pow(distance, fa) + Mathf.Pow(fb - fb * distance, fa));

        return Mathf.Clamp01(1f - falloff);  // 1 in center, 0 near edges
    }

    float GetNormalizedDistanceFromCenter(int x, int y)
    {
        float centerX = (mapWidth - 1) / 2f;
        float centerY = (mapHeight - 1) / 2f;

        float dx = x - centerX;
        float dy = y - centerY;

        float distance = Mathf.Sqrt(dx * dx + dy * dy);

        float maxDistance = Mathf.Sqrt(centerX * centerX + centerY * centerY);

        return distance / maxDistance;  // 0 at center, 1 at furthest corner
    }

    public void OnValidate()
    {
        if (octaves <= 0) octaves = 1;

        random = new System.Random(seed);
        Generate();
    }
}
