using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class MapGenerator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private float scale;
    [SerializeField] private float factor;

    [Header("Mesh Rendering")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Gradient gradient;
    private Mesh mesh;
    private List<Vector3> vertices;
    private List<int> triangles;
    private Color[] colors;

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
        colors = new Color[width * height];

        for (float y = 0, i = 0; y < height; y++)
        {
            for (float x = 0; x < width; x++)
            {
                float elevation = Mathf.PerlinNoise(x * factor + 0.1f, y * factor + 0.1f);

                colors[(int)i] = gradient.Evaluate(elevation);
                vertices.Add(new Vector3(x - (width/2), elevation * scale, y - (height/2)));

                if (x < width - 1 && y < height - 1)
                {
                    triangles.Add((int)i + width);
                    triangles.Add((int)i + width + 1);
                    triangles.Add((int)i);

                    triangles.Add((int)i + 1);
                    triangles.Add((int)i);
                    triangles.Add((int)i + width + 1);
                }

                i++;
            }
        }
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
