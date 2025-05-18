using System.Collections.Generic;
using UnityEngine;

public class MeshMapGenerator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int mapWidth;
    [SerializeField] private int mapHeight;
    [SerializeField] private int seed;

    [Space(10)]
    [SerializeField] private float scale;
    [SerializeField] private float zoom;

    [Space(10)]
    [SerializeField] private int octaves = 4;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2.0f;

    [Header("Mesh Rendering")]
    [SerializeField] private MeshFilter meshFilter;
    private Mesh mesh;
    private List<Vector3> vertices;
    private List<int> triangles;
    private Color[] colors;

    [System.Serializable]
    public struct MapRegion
    {
        public string name;
        public float height;
        public Color color;
    };

    [SerializeField] private MapRegion[] regions;

    private System.Random random;

    void Start()
    {
        random = new System.Random(seed);
        mesh = new Mesh();
        meshFilter.mesh = mesh;


        Generate();
        UpdateMesh();
    }

    public void Generate()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        colors = new Color[mapWidth * mapHeight];

        float offset = random.Next(-10000, 10000);
        float halfWidht = (mapWidth / 2);
        float halfHeight = (mapHeight / 2);
        float halfScale = (scale / 2);

        for (float y = 0, i = 0; y < mapHeight; y++)
        {
            for (float x = 0; x < mapWidth; x++)
            {
                float height = GetHeight(x, y, offset);
                vertices.Add(new Vector3(x - halfWidht, (height * scale) - halfScale, y - halfHeight));

                if (x < mapWidth - 1 && y < mapHeight - 1)
                {
                    triangles.Add((int)i + mapWidth);
                    triangles.Add((int)i + mapWidth + 1);
                    triangles.Add((int)i);

                    triangles.Add((int)i + 1);
                    triangles.Add((int)i);
                    triangles.Add((int)i + mapWidth + 1);
                }

                for (int regionIndex = 0; regionIndex < regions.Length; regionIndex++)
                {
                    if (height < regions[regionIndex].height)
                    {
                        colors[(int)i] = regions[regionIndex].color;
                    }
                }

                i++;
            }
        }
    }

    public float[,] GetHeightMap()
    {
        float[,] heights = new float[mapWidth, mapHeight];
        float offset = random.Next(-10000, 10000);

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                heights[x,y] = GetHeight(x, y, offset);
            }
        }

        return heights;
    }

    public float GetHeight(float x, float y, float offset)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            float noise = Mathf.PerlinNoise((x + offset) / zoom * frequency, (y + offset) / zoom * frequency);
            total += noise * amplitude;

            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }

    public void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors;

        mesh.RecalculateNormals();
    }

    public void OnValidate()
    {
        if (octaves <= 0) octaves = 1;
        if (mesh == null) return;

        random = new System.Random(seed);
        Generate();
        UpdateMesh();
    }
}
