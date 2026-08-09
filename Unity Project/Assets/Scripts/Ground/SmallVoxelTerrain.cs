using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class SmallVoxelTerrain : MonoBehaviour
{
    [Header("Terrain Size")]
    public int width = 64;
    public int depth = 64;
    public int maxHeight = 20;

    [Header("Voxel Size")]
    public float blockSize = 0.25f;

    [Header("Noise")]
    public float noiseScale = 18f;
    public int seed = 12345;
    public Vector2 noiseOffset;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Height Quantization")]
    public int heightStep = 1;

    [Header("Colors")]
    public Color grassTopColor = new Color(0.32f, 0.55f, 0.26f);
    public Color dirtColor = new Color(0.45f, 0.32f, 0.18f);
    public Color rockColor = new Color(0.75f, 0.75f, 0.75f);

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private int[,] heights;
    private List<Vector3> vertices;
    private List<int> triangles;
    private List<Color> colors;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        Generate();
    }

    [ContextMenu("Generate Voxel Terrain")]
    public void Generate()
    {
        GenerateHeightMap();
        BuildMesh();
    }

    void GenerateHeightMap()
    {
        heights = new int[width, depth];

        System.Random prng = new System.Random(seed);
        float offsetX = prng.Next(-100000, 100000) + noiseOffset.x;
        float offsetZ = prng.Next(-100000, 100000) + noiseOffset.y;

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;
                float maxPossibleHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = ((x + offsetX) / noiseScale) * frequency;
                    float sampleZ = ((z + offsetZ) / noiseScale) * frequency;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                    noiseHeight += perlin * amplitude;

                    maxPossibleHeight += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseHeight /= Mathf.Max(maxPossibleHeight, 0.0001f);
                noiseHeight = (noiseHeight + 1f) * 0.5f;

                int h = Mathf.RoundToInt(noiseHeight * maxHeight);
                h = Mathf.Max(1, h);

                if (heightStep > 1)
                    h = (h / heightStep) * heightStep;

                heights[x, z] = h;
            }
        }
    }

    void BuildMesh()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        colors = new List<Color>();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int h = heights[x, z];

                for (int y = 0; y < h; y++)
                {
                    bool isTop = (y == h - 1);

                    // top
                    AddFaceIfVisible(x, y, z, FaceDirection.Top, isTop ? grassTopColor : dirtColor);

                    // bottom not needed for terrain prototype

                    // sides only if neighbor is lower than this block level
                    if (IsFaceVisible(x - 1, z, y))
                        AddFaceIfVisible(x, y, z, FaceDirection.Left, y >= h - 3 ? dirtColor : rockColor);

                    if (IsFaceVisible(x + 1, z, y))
                        AddFaceIfVisible(x, y, z, FaceDirection.Right, y >= h - 3 ? dirtColor : rockColor);

                    if (IsFaceVisible(x, z - 1, y))
                        AddFaceIfVisible(x, y, z, FaceDirection.Back, y >= h - 3 ? dirtColor : rockColor);

                    if (IsFaceVisible(x, z + 1, y))
                        AddFaceIfVisible(x, y, z, FaceDirection.Forward, y >= h - 3 ? dirtColor : rockColor);
                }
            }
        }

        Mesh mesh = new Mesh();
        if (vertices.Count > 65000)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetColors(colors);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    bool IsFaceVisible(int nx, int nz, int y)
    {
        if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
            return true;

        return heights[nx, nz] <= y;
    }

    enum FaceDirection
    {
        Top,
        Left,
        Right,
        Back,
        Forward
    }

    void AddFaceIfVisible(int x, int y, int z, FaceDirection dir, Color color)
    {
        Vector3 p = new Vector3(x * blockSize, y * blockSize, z * blockSize);

        Vector3 v0, v1, v2, v3;

        switch (dir)
        {
            case FaceDirection.Top:
                v0 = p + new Vector3(0, blockSize, 0);
                v1 = p + new Vector3(0, blockSize, blockSize);
                v2 = p + new Vector3(blockSize, blockSize, blockSize);
                v3 = p + new Vector3(blockSize, blockSize, 0);
                break;

            case FaceDirection.Left:
                v0 = p + new Vector3(0, 0, 0);
                v1 = p + new Vector3(0, blockSize, 0);
                v2 = p + new Vector3(0, blockSize, blockSize);
                v3 = p + new Vector3(0, 0, blockSize);
                break;

            case FaceDirection.Right:
                v0 = p + new Vector3(blockSize, 0, blockSize);
                v1 = p + new Vector3(blockSize, blockSize, blockSize);
                v2 = p + new Vector3(blockSize, blockSize, 0);
                v3 = p + new Vector3(blockSize, 0, 0);
                break;

            case FaceDirection.Back:
                v0 = p + new Vector3(blockSize, 0, 0);
                v1 = p + new Vector3(blockSize, blockSize, 0);
                v2 = p + new Vector3(0, blockSize, 0);
                v3 = p + new Vector3(0, 0, 0);
                break;

            default: // Forward
                v0 = p + new Vector3(0, 0, blockSize);
                v1 = p + new Vector3(0, blockSize, blockSize);
                v2 = p + new Vector3(blockSize, blockSize, blockSize);
                v3 = p + new Vector3(blockSize, 0, blockSize);
                break;
        }

        AddQuad(v0, v1, v2, v3, color);
    }

    void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color color)
    {
        int start = vertices.Count;

        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);

        triangles.Add(start + 0);
        triangles.Add(start + 2);
        triangles.Add(start + 1);

        triangles.Add(start + 0);
        triangles.Add(start + 3);
        triangles.Add(start + 2);
    }
}