// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: TerrainWall.cs
//
// Author: Mikael Danielsson
// Date Created: 22-07-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("")]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class TerrainWall : MonoBehaviour
    {
        public Terrain terrain;
        public float endYPoint = -50;

        private NativeList<float> edgeRight;
        private NativeList<float> edgeLeft;
        private NativeList<float> edgeTop;
        private NativeList<float> edgeBottom;
        private NativeList<Vector3> vertices;
        private NativeList<int> triangles;
        private NativeList<Vector3> normals;
        private NativeList<Vector2> uv;
        private JobHandle jobHandle;

        [NonSerialized] private MeshFilter meshFilter;
        [NonSerialized] private MeshRenderer meshRenderer;
        [NonSerialized] private Mesh mesh;

        [NonSerialized] private bool initalized;
        [SerializeField, HideInInspector] private int version;
        [NonSerialized] private int oldVersion;
        [NonSerialized] private float oldEndYPoint;
        [NonSerialized] private Terrain oldTerrain;
        [NonSerialized] int oldResolution;

        private void OnEnable()
        {
            if (initalized)
                return;

            initalized = true;

            PrepareNativeData();

            if (terrain == null)
                terrain = Terrain.activeTerrain;

            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = "TerrainWall";
            }

            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();

                if (meshFilter == null)
                {
                    meshFilter = gameObject.AddComponent<MeshFilter>();
#if UNITY_EDITOR
                    UnityEditor.Undo.RegisterCreatedObjectUndo(meshFilter, "Created terrain wall");
#endif
                }
            }

            if (meshFilter != null)
            {
                meshFilter.sharedMesh = mesh;
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();

                if (meshRenderer == null)
                {
                    meshRenderer = gameObject.AddComponent<MeshRenderer>();
#if UNITY_EDITOR
                    UnityEditor.Undo.RegisterCreatedObjectUndo(meshRenderer, "Created terrain wall");
#endif
                }
            }

            TerrainCallbacks.heightmapChanged += HeightmapChanged;
            MarkDirty();
        }

        private void OnDisable()
        {
            TerrainCallbacks.heightmapChanged -= HeightmapChanged;
            DisposeNativeData();
            initalized = false;
        }

        private void OnDestroy()
        {
            DisposeNativeData();
            TerrainCallbacks.heightmapChanged -= HeightmapChanged;

            if (mesh != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(mesh);
                else
#endif
                    Destroy(mesh);
            }
        }

        private void Update()
        {
            if (terrain == null || terrain.terrainData == null)
                return;

            bool versionDirty = oldVersion != version;
            bool endYPointDirty = oldEndYPoint != endYPoint;
            bool terrainDirty = oldTerrain != terrain;
            bool resolutionDirty = oldResolution != terrain.terrainData.heightmapResolution;

            if (versionDirty || endYPointDirty || terrainDirty || resolutionDirty)
            {
                oldVersion = version;
                oldEndYPoint = endYPoint;
                oldTerrain = terrain;
                oldResolution = terrain.terrainData.heightmapResolution;

                PrepareNativeData();

                UpdateEdges();
                UpdateMesh();
            }
        }

        private void HeightmapChanged(Terrain changedTerrain, RectInt heightRegion, bool synched)
        {
            if (terrain == null || terrain.terrainData == null)
                return;

            if (changedTerrain != terrain)
                return;

            MarkDirty();
        }

        private void PrepareNativeData()
        {
            if(!edgeRight.IsCreated) edgeRight = new NativeList<float>(0, Allocator.Persistent);
            if (!edgeLeft.IsCreated) edgeLeft = new NativeList<float>(0, Allocator.Persistent);
            if (!edgeTop.IsCreated) edgeTop = new NativeList<float>(0, Allocator.Persistent);
            if (!edgeBottom.IsCreated) edgeBottom = new NativeList<float>(0, Allocator.Persistent);
            if (!vertices.IsCreated) vertices = new NativeList<Vector3>(0, Allocator.Persistent);
            if (!triangles.IsCreated) triangles = new NativeList<int>(0, Allocator.Persistent);
            if (!normals.IsCreated) normals = new NativeList<Vector3>(0, Allocator.Persistent);
            if (!uv.IsCreated) uv = new NativeList<Vector2>(0, Allocator.Persistent);
        }

        private void DisposeNativeData()
        {
            if (edgeRight.IsCreated) edgeRight.Dispose();
            if (edgeLeft.IsCreated) edgeLeft.Dispose();
            if (edgeTop.IsCreated) edgeTop.Dispose();
            if (edgeBottom.IsCreated) edgeBottom.Dispose();
            if (vertices.IsCreated) vertices.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
            if (normals.IsCreated) normals.Dispose();
            if (uv.IsCreated) uv.Dispose();
        }

        private void UpdateMesh()
        {
            int resolution = terrain.terrainData.heightmapResolution;
            int segmentCount = resolution - 1;
            int wallSegmentCount = segmentCount * 4;

            int vertexCount = wallSegmentCount * 4;
            int triangleCount = wallSegmentCount * 6;

            vertices.ResizeUninitialized(vertexCount);
            normals.ResizeUninitialized(vertexCount);
            uv.ResizeUninitialized(vertexCount);
            triangles.ResizeUninitialized(triangleCount);

            TerrainData terrainData = terrain.terrainData;

            TerrainWallJob terrainWallJob = new TerrainWallJob()
            {
                edgeRight = edgeRight.AsArray(),
                edgeLeft = edgeLeft.AsArray(),
                edgeTop = edgeTop.AsArray(),
                edgeBottom = edgeBottom.AsArray(),

                triangles = triangles.AsArray(),
                vertices = vertices.AsArray(),
                normals = normals.AsArray(),
                uv = uv.AsArray(),

                terrainPosition = terrain.transform.position,
                terrainSize = new float2(
                    terrainData.size.x,
                    terrainData.size.z),

                endYPoint = endYPoint,
                worldToLocalMatrix = transform.worldToLocalMatrix,
                resolution = resolution,
            };

            jobHandle = terrainWallJob.Schedule(wallSegmentCount, 32);
            jobHandle.Complete();

            mesh.Clear();

            mesh.indexFormat = vertexCount > ushort.MaxValue ? UnityEngine.Rendering.IndexFormat.UInt32: UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices.AsArray());
            mesh.SetNormals(normals.AsArray());
            mesh.SetUVs(0, uv.AsArray());
            mesh.SetIndices(triangles.AsArray(), MeshTopology.Triangles, 0, false);
            mesh.RecalculateBounds();
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, null);
#endif
            version++;
        }

        private void UpdateEdges()
        {
            TerrainData terrainData = terrain.terrainData;
            int resolution = terrainData.heightmapResolution;

            float terrainPositionY = terrain.transform.position.y;
            float terrainHeight = terrainData.size.y;

            float[,] bottomHeights = terrainData.GetHeights(0, 0, resolution, 1);
            float[,] topHeights = terrainData.GetHeights(0, resolution - 1, resolution, 1);
            float[,] leftHeights = terrainData.GetHeights(0, 0, 1, resolution);
            float[,] rightHeights = terrainData.GetHeights(resolution - 1, 0, 1, resolution);

            edgeBottom.Clear();
            edgeTop.Clear();
            edgeLeft.Clear();
            edgeRight.Clear();

            edgeBottom.ResizeUninitialized(resolution);
            edgeTop.ResizeUninitialized(resolution);
            edgeLeft.ResizeUninitialized(resolution);
            edgeRight.ResizeUninitialized(resolution);

            for (int i = 0; i < resolution; i++)
            {
                edgeBottom[i] = terrainPositionY + bottomHeights[0, i] * terrainHeight;
                edgeTop[i] = terrainPositionY + topHeights[0, i] * terrainHeight;
                edgeLeft[i] = terrainPositionY + leftHeights[i, 0] * terrainHeight;
                edgeRight[i] = terrainPositionY + rightHeights[i, 0] * terrainHeight;
            }
        }
    }
}
