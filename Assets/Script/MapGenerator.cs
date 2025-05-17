using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class MapGenerator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int mapWidth;
    [SerializeField] private int mapHeight;
    [SerializeField] private float scale;
    [SerializeField] private float factor;
    [SerializeField] private int seed;

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

        for (float y = 0, i = 0; y < mapHeight; y++)
        {
            for (float x = 0; x < mapWidth; x++)
            {
                float height = GetHeight(x, y);
                vertices.Add(new Vector3(x - (mapWidth/2), (height * scale) - (scale / 2), y - (mapHeight/2)));

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

    public float GetHeight(float x, float y)
    {
        float height = Mathf.PerlinNoise(x / factor + seed, y / factor + seed);

        return height;
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
        if(mesh == null) return;

        Generate();
        UpdateMesh();
    }
}
