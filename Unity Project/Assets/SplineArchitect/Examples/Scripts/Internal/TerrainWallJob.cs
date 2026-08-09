// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: TerrainWallJob.cs
//
// Author: Mikael Danielsson
// Date Created: 22-07-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

namespace SplineArchitect.Examples
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct TerrainWallJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> edgeRight;
        [ReadOnly] public NativeArray<float> edgeLeft;
        [ReadOnly] public NativeArray<float> edgeTop;
        [ReadOnly] public NativeArray<float> edgeBottom;

        [NativeDisableParallelForRestriction] public NativeArray<int> triangles;
        [NativeDisableParallelForRestriction] public NativeArray<Vector3> vertices;
        [NativeDisableParallelForRestriction] public NativeArray<Vector3> normals;
        [NativeDisableParallelForRestriction] public NativeArray<Vector2> uv;

        public float3 terrainPosition;
        public float2 terrainSize;
        public float endYPoint;
        public float4x4 worldToLocalMatrix;
        public int resolution;

        public void Execute(int i)
        {
            float uvTileSize = 10;
            int segmentCount = resolution - 1;

            int edgeIndex = i / segmentCount;
            int segmentIndex = i - edgeIndex * segmentCount;

            float startT = segmentIndex / (float)segmentCount;
            float endT = (segmentIndex + 1) / (float)segmentCount;

            float startX = terrainPosition.x + terrainSize.x * startT;
            float endX = terrainPosition.x + terrainSize.x * endT;

            float startZ = terrainPosition.z + terrainSize.y * startT;
            float endZ = terrainPosition.z + terrainSize.y * endT;

            float3 topStart;
            float3 topEnd;
            float3 bottomStart;
            float3 bottomEnd;

            bool reverseTriangles;

            // Build the wall geometry in world space.
            switch (edgeIndex)
            {
                // Bottom edge: Z = terrain minimum Z.
                default:
                case 0:
                    topStart = new float3(
                        startX,
                        edgeBottom[segmentIndex],
                        terrainPosition.z);

                    topEnd = new float3(
                        endX,
                        edgeBottom[segmentIndex + 1],
                        terrainPosition.z);

                    bottomStart = new float3(
                        startX,
                        endYPoint,
                        terrainPosition.z);

                    bottomEnd = new float3(
                        endX,
                        endYPoint,
                        terrainPosition.z);

                    reverseTriangles = false;
                    break;

                // Right edge: X = terrain maximum X.
                case 1:
                    topStart = new float3(
                        terrainPosition.x + terrainSize.x,
                        edgeRight[segmentIndex],
                        startZ);

                    topEnd = new float3(
                        terrainPosition.x + terrainSize.x,
                        edgeRight[segmentIndex + 1],
                        endZ);

                    bottomStart = new float3(
                        terrainPosition.x + terrainSize.x,
                        endYPoint,
                        startZ);

                    bottomEnd = new float3(
                        terrainPosition.x + terrainSize.x,
                        endYPoint,
                        endZ);

                    reverseTriangles = false;
                    break;

                // Top edge: Z = terrain maximum Z.
                case 2:
                    topStart = new float3(
                        startX,
                        edgeTop[segmentIndex],
                        terrainPosition.z + terrainSize.y);

                    topEnd = new float3(
                        endX,
                        edgeTop[segmentIndex + 1],
                        terrainPosition.z + terrainSize.y);

                    bottomStart = new float3(
                        startX,
                        endYPoint,
                        terrainPosition.z + terrainSize.y);

                    bottomEnd = new float3(
                        endX,
                        endYPoint,
                        terrainPosition.z + terrainSize.y);

                    reverseTriangles = true;
                    break;

                // Left edge: X = terrain minimum X.
                case 3:
                    topStart = new float3(
                        terrainPosition.x,
                        edgeLeft[segmentIndex],
                        startZ);

                    topEnd = new float3(
                        terrainPosition.x,
                        edgeLeft[segmentIndex + 1],
                        endZ);

                    bottomStart = new float3(
                        terrainPosition.x,
                        endYPoint,
                        startZ);

                    bottomEnd = new float3(
                        terrainPosition.x,
                        endYPoint,
                        endZ);

                    reverseTriangles = true;
                    break;
            }

            float safeUvTileSize = math.max(uvTileSize, 0.0001f);

            float perimeterStart;
            float perimeterEnd;

            // Calculate U as distance around the terrain perimeter.
            switch (edgeIndex)
            {
                // Bottom: left to right.
                default:
                case 0:
                    perimeterStart = terrainSize.x * startT;
                    perimeterEnd = terrainSize.x * endT;
                    break;

                // Right: bottom to top.
                case 1:
                    perimeterStart =
                        terrainSize.x +
                        terrainSize.y * startT;

                    perimeterEnd =
                        terrainSize.x +
                        terrainSize.y * endT;
                    break;

                // Top: right to left around the perimeter.
                case 2:
                    perimeterStart =
                        terrainSize.x +
                        terrainSize.y +
                        terrainSize.x * (1f - startT);

                    perimeterEnd =
                        terrainSize.x +
                        terrainSize.y +
                        terrainSize.x * (1f - endT);
                    break;

                // Left: top to bottom around the perimeter.
                case 3:
                    perimeterStart =
                        terrainSize.x * 2f +
                        terrainSize.y +
                        terrainSize.y * (1f - startT);

                    perimeterEnd =
                        terrainSize.x * 2f +
                        terrainSize.y +
                        terrainSize.y * (1f - endT);
                    break;
            }

            // Calculate V from world-space height.
            // This keeps horizontal texture lines level.
            float topStartV = topStart.y - endYPoint;
            float topEndV = topEnd.y - endYPoint;
            float bottomStartV = bottomStart.y - endYPoint;
            float bottomEndV = bottomEnd.y - endYPoint;

            Vector2 topStartUv = new Vector2(
                perimeterStart / safeUvTileSize,
                topStartV / safeUvTileSize);

            Vector2 topEndUv = new Vector2(
                perimeterEnd / safeUvTileSize,
                topEndV / safeUvTileSize);

            Vector2 bottomStartUv = new Vector2(
                perimeterStart / safeUvTileSize,
                bottomStartV / safeUvTileSize);

            Vector2 bottomEndUv = new Vector2(
                perimeterEnd / safeUvTileSize,
                bottomEndV / safeUvTileSize);

            // Convert geometry into the mesh's local space.
            topStart = math.transform(worldToLocalMatrix, topStart);
            topEnd = math.transform(worldToLocalMatrix, topEnd);
            bottomStart = math.transform(worldToLocalMatrix, bottomStart);
            bottomEnd = math.transform(worldToLocalMatrix, bottomEnd);

            int vertexIndex = i * 4;
            int triangleIndex = i * 6;

            vertices[vertexIndex] = topStart;
            vertices[vertexIndex + 1] = topEnd;
            vertices[vertexIndex + 2] = bottomStart;
            vertices[vertexIndex + 3] = bottomEnd;

            if (!reverseTriangles)
            {
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 1;
                triangles[triangleIndex + 2] = vertexIndex + 2;

                triangles[triangleIndex + 3] = vertexIndex + 1;
                triangles[triangleIndex + 4] = vertexIndex + 3;
                triangles[triangleIndex + 5] = vertexIndex + 2;
            }
            else
            {
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 2;
                triangles[triangleIndex + 2] = vertexIndex + 1;

                triangles[triangleIndex + 3] = vertexIndex + 1;
                triangles[triangleIndex + 4] = vertexIndex + 2;
                triangles[triangleIndex + 5] = vertexIndex + 3;
            }

            float3 normal;

            if (!reverseTriangles)
            {
                normal = math.normalizesafe(
                    math.cross(
                        topEnd - topStart,
                        bottomStart - topStart));
            }
            else
            {
                normal = math.normalizesafe(
                    math.cross(
                        bottomStart - topStart,
                        topEnd - topStart));
            }

            normals[vertexIndex] = normal;
            normals[vertexIndex + 1] = normal;
            normals[vertexIndex + 2] = normal;
            normals[vertexIndex + 3] = normal;

            uv[vertexIndex] = topStartUv;
            uv[vertexIndex + 1] = topEndUv;
            uv[vertexIndex + 2] = bottomStartUv;
            uv[vertexIndex + 3] = bottomEndUv;
        }
    }
}