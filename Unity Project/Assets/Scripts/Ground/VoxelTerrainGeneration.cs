using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class VoxelTerrainGeneration : MonoBehaviour
{
    [Header("Terrain Size (in blocks)")]
    public int width = 120;
    public int depth = 120;
    public int maxHeightInBlocks = 14;

    [Header("Voxel Size")]
    public float voxelSize = 0.25f;

    [Header("Noise")]
    public float noiseScale = 34f;
    public int seed = 12345;
    public Vector2 noiseOffset;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Shape")]
    public bool useFalloff = false;
    public float falloffStrength = 2f;

    [Header("Water / Lowlands")]
    public int waterHeightInBlocks = 2;
    public bool flattenWater = false;

    [Header("Rendering")]
    public Material terrainMaterial;
    public bool generateBottomFaces = false;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;

    private int[,] heightMap;
    private System.Random prng;

    private enum FaceDirection
    {
        Top,
        Bottom,
        North, // +Z
        South, // -Z
        East,  // +X
        West   // -X
    }

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        Generate();
    }

    [ContextMenu("Generate Voxel Terrain")]
    public void Generate()
    {
        prng = new System.Random(seed);
        GenerateHeightMap();
        BuildVoxelMesh();

        if (terrainMaterial != null)
            meshRenderer.sharedMaterial = terrainMaterial;
    }

    void GenerateHeightMap()
    {
        heightMap = new int[width, depth];

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

                if (useFalloff)
                {
                    float nx = (x / (float)(width - 1)) * 2f - 1f;
                    float nz = (z / (float)(depth - 1)) * 2f - 1f;
                    float dist = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(nz));
                    float falloff = Mathf.Pow(dist, falloffStrength);
                    noiseHeight = Mathf.Clamp01(noiseHeight - falloff);
                }

                int finalHeight = Mathf.RoundToInt(noiseHeight * maxHeightInBlocks);
                finalHeight = Mathf.Clamp(finalHeight, 0, maxHeightInBlocks);

                if (flattenWater && finalHeight < waterHeightInBlocks)
                    finalHeight = waterHeightInBlocks;

                heightMap[x, z] = finalHeight;
            }
        }
    }

    void BuildVoxelMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int columnHeight = heightMap[x, z];
                if (columnHeight <= 0)
                    continue;

                for (int y = 0; y < columnHeight; y++)
                {
                    bool topVisible = (y == columnHeight - 1);
                    bool bottomVisible = generateBottomFaces && y == 0;

                    bool northVisible = GetHeight(x, z + 1) <= y;
                    bool southVisible = GetHeight(x, z - 1) <= y;
                    bool eastVisible = GetHeight(x + 1, z) <= y;
                    bool westVisible = GetHeight(x - 1, z) <= y;

                    if (topVisible) AddFace(vertices, triangles, uvs, x, y, z, FaceDirection.Top);
                    if (bottomVisible) AddFace(vertices, triangles, uvs, x, y, z, FaceDirection.Bottom);

                    if (northVisible) AddFace(vertices, triangles, uvs, x, y, z, FaceDirection.North);
                    if (southVisible) AddFace(vertices, triangles, uvs, x, y, z, FaceDirection.South);
                    if (eastVisible) AddFace(vertices, triangles, uvs, x, y, z, FaceDirection.East);
                    if (westVisible) AddFace(vertices, triangles, uvs, x, y, z, FaceDirection.West);
                }
            }
        }

        Mesh mesh = new Mesh();
        if (vertices.Count > 65000)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

    int GetHeight(int x, int z)
    {
        if (x < 0 || x >= width || z < 0 || z >= depth)
            return 0;

        return heightMap[x, z];
    }

    void AddFace(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs,
        int x, int y, int z,
        FaceDirection dir)
    {
        Vector3 p = new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
        int startIndex = vertices.Count;

        switch (dir)
        {
            case FaceDirection.Top:
                vertices.Add(p + new Vector3(0, voxelSize, 0));
                vertices.Add(p + new Vector3(0, voxelSize, voxelSize));
                vertices.Add(p + new Vector3(voxelSize, voxelSize, 0));
                vertices.Add(p + new Vector3(voxelSize, voxelSize, voxelSize));
                break;

            case FaceDirection.Bottom:
                vertices.Add(p + new Vector3(0, 0, 0));
                vertices.Add(p + new Vector3(voxelSize, 0, 0));
                vertices.Add(p + new Vector3(0, 0, voxelSize));
                vertices.Add(p + new Vector3(voxelSize, 0, voxelSize));
                break;

            case FaceDirection.North: // +Z
                vertices.Add(p + new Vector3(0, 0, voxelSize));
                vertices.Add(p + new Vector3(voxelSize, 0, voxelSize));
                vertices.Add(p + new Vector3(0, voxelSize, voxelSize));
                vertices.Add(p + new Vector3(voxelSize, voxelSize, voxelSize));
                break;

            case FaceDirection.South: // -Z
                vertices.Add(p + new Vector3(0, 0, 0));
                vertices.Add(p + new Vector3(0, voxelSize, 0));
                vertices.Add(p + new Vector3(voxelSize, 0, 0));
                vertices.Add(p + new Vector3(voxelSize, voxelSize, 0));
                break;

            case FaceDirection.East: // +X
                vertices.Add(p + new Vector3(voxelSize, 0, 0));
                vertices.Add(p + new Vector3(voxelSize, voxelSize, 0));
                vertices.Add(p + new Vector3(voxelSize, 0, voxelSize));
                vertices.Add(p + new Vector3(voxelSize, voxelSize, voxelSize));
                break;

            case FaceDirection.West: // -X
                vertices.Add(p + new Vector3(0, 0, 0));
                vertices.Add(p + new Vector3(0, 0, voxelSize));
                vertices.Add(p + new Vector3(0, voxelSize, 0));
                vertices.Add(p + new Vector3(0, voxelSize, voxelSize));
                break;
        }

        triangles.Add(startIndex + 0);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);

        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 3);

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));
    }

    public Vector3 GetTopCenterWorldPosition(int x, int z)
    {
        int h = GetHeight(x, z);
        return transform.TransformPoint(new Vector3(
            x * voxelSize + voxelSize * 0.5f,
            h * voxelSize,
            z * voxelSize + voxelSize * 0.5f
        ));
    }
}