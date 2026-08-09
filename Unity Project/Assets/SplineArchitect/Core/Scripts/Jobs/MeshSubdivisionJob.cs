// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MeshSubdivisionJob.cs
//
// Author: Mikael Danielsson
// Date Created: 05-03-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;

namespace SplineArchitect.Jobs
{
    [BurstCompile]
    public struct MeshSubdivisionJob : IJob
    {
        public struct Edge : IEquatable<Edge>
        {
            public int a;
            public int b;

            public Edge(int a, int b)
            {
                if (a < b)
                {
                    this.a = a;
                    this.b = b;
                }
                else
                {
                    this.a = b;
                    this.b = a;
                }
            }

            public bool Equals(Edge other)
            {
                return a == other.a && b == other.b;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (a * 73856093) ^ (b * 19349663);
                }
            }
        }

        public struct QuantizedPos : IEquatable<QuantizedPos>
        {
            public int x;
            public int y;
            public int z;

            public QuantizedPos(Vector3 p, float scale)
            {
                x = (int)math.round(p.x * scale);
                y = (int)math.round(p.y * scale);
                z = (int)math.round(p.z * scale);
            }

            public bool Equals(QuantizedPos other)
            {
                return x == other.x && y == other.y && z == other.z;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
                }
            }
        }

        [ReadOnly] public IndexFormat indexFormat;
        [ReadOnly] public int iterations;
        [ReadOnly] public bool onlyZ;

        public NativeArray<uint> subMeshMap;
        public NativeArray<float> longestEdgeResult;
        public NativeArray<int> maxResolution;

        public NativeList<int> triangles;
        public NativeList<Vector3> vertices;

        public NativeList<Vector2> uv;
        public NativeList<Vector2> uv2;
        public NativeList<Vector2> uv3;
        public NativeList<Vector2> uv4;
        public NativeList<Vector2> uv5;
        public NativeList<Vector2> uv6;
        public NativeList<Vector2> uv7;
        public NativeList<Vector2> uv8;

        public NativeList<Vector3> normals;
        public NativeList<Vector4> tangents;

        public void Execute()
        {
            const float weldScale = 100000f; // tolerance = 0.00001 units
            maxResolution[0] = -1;

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                float longestEdge = 0f;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int vA = triangles[i + 0];
                    int vB = triangles[i + 1];
                    int vC = triangles[i + 2];

                    Vector3 a = vertices[vA];
                    Vector3 b = vertices[vB];
                    Vector3 c = vertices[vC];

                    if (onlyZ)
                    {
                        a.x = 0f;
                        a.y = 0f;

                        b.x = 0f;
                        b.y = 0f;

                        c.x = 0f;
                        c.y = 0f;
                    }

                    float dAB = Vector3.Distance(a, b);
                    float dBC = Vector3.Distance(b, c);
                    float dCA = Vector3.Distance(c, a);

                    longestEdge = Mathf.Max(longestEdge, dAB);
                    longestEdge = Mathf.Max(longestEdge, dBC);
                    longestEdge = Mathf.Max(longestEdge, dCA);
                }

                longestEdgeResult[0] = longestEdge;

                // Build canonical welded vertex ids by quantized position.
                NativeArray<int> canonicalIndexPerVertex = new NativeArray<int>(vertices.Length, Allocator.Temp);
                NativeParallelHashMap<QuantizedPos, int> canonicalFromPos = new NativeParallelHashMap<QuantizedPos, int>(vertices.Length, Allocator.Temp);

                int nextCanonical = 0;
                for (int i = 0; i < vertices.Length; i++)
                {
                    QuantizedPos qp = new QuantizedPos(vertices[i], weldScale);

                    if (canonicalFromPos.ContainsKey(qp))
                    {
                        canonicalIndexPerVertex[i] = canonicalFromPos[qp];
                    }
                    else
                    {
                        canonicalFromPos.Add(qp, nextCanonical);
                        canonicalIndexPerVertex[i] = nextCanonical;
                        nextCanonical++;
                    }
                }

                NativeParallelHashSet<Edge> splitCanonicalEdges = new NativeParallelHashSet<Edge>(triangles.Length, Allocator.Temp);
                NativeParallelHashMap<Edge, int> realMidpointMap = new NativeParallelHashMap<Edge, int>(triangles.Length, Allocator.Temp);
                NativeList<int> newTriangles = new NativeList<int>(triangles.Length * 2, Allocator.Temp);

                // PASS 1: mark canonical edges to split
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int vA = triangles[i + 0];
                    int vB = triangles[i + 1];
                    int vC = triangles[i + 2];

                    Vector3 a = vertices[vA];
                    Vector3 b = vertices[vB];
                    Vector3 c = vertices[vC];

                    if (onlyZ)
                    {
                        a.x = 0f;
                        a.y = 0f;

                        b.x = 0f;
                        b.y = 0f;

                        c.x = 0f;
                        c.y = 0f;
                    }

                    float dAB = Vector3.Distance(a, b);
                    float dBC = Vector3.Distance(b, c);
                    float dCA = Vector3.Distance(c, a);

                    int cA = canonicalIndexPerVertex[vA];
                    int cB = canonicalIndexPerVertex[vB];
                    int cC = canonicalIndexPerVertex[vC];

                    int longestC1 = cA;
                    int longestC2 = cB;
                    float longestD = dAB;

                    if (dBC > longestD)
                    {
                        longestC1 = cB;
                        longestC2 = cC;
                        longestD = dBC;
                    }

                    if (dCA > longestD)
                    {
                        longestC1 = cC;
                        longestC2 = cA;
                        longestD = dCA;
                    }

                    if (longestD >= longestEdge * 0.5f)
                        splitCanonicalEdges.Add(new Edge(longestC1, longestC2));
                }

                NativeParallelHashSet<Edge> splitRealEdges = new NativeParallelHashSet<Edge>(triangles.Length, Allocator.Temp);

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int A = triangles[i + 0];
                    int B = triangles[i + 1];
                    int C = triangles[i + 2];

                    Edge realAB = new Edge(A, B);
                    Edge realBC = new Edge(B, C);
                    Edge realCA = new Edge(C, A);

                    Edge canAB = new Edge(canonicalIndexPerVertex[A], canonicalIndexPerVertex[B]);
                    Edge canBC = new Edge(canonicalIndexPerVertex[B], canonicalIndexPerVertex[C]);
                    Edge canCA = new Edge(canonicalIndexPerVertex[C], canonicalIndexPerVertex[A]);

                    if (splitCanonicalEdges.Contains(canAB))
                        splitRealEdges.Add(realAB);

                    if (splitCanonicalEdges.Contains(canBC))
                        splitRealEdges.Add(realBC);

                    if (splitCanonicalEdges.Contains(canCA))
                        splitRealEdges.Add(realCA);
                }

                int vertexLimit = indexFormat == IndexFormat.UInt16 ? 65536 : 262144;
                if (vertices.Length + splitRealEdges.Count() > vertexLimit)
                {
                    splitRealEdges.Dispose();
                    newTriangles.Dispose();
                    realMidpointMap.Dispose();
                    splitCanonicalEdges.Dispose();
                    canonicalFromPos.Dispose();
                    canonicalIndexPerVertex.Dispose();
                    maxResolution[0] = iteration;
                    break;
                }

                // PASS 2 + 3: rebuild triangles, creating real midpoints on demand
                int subMeshStart = 0;

                for (int subMesh = 0; subMesh < subMeshMap.Length; subMesh++)
                {
                    int oldSubMeshIndexCount = (int)subMeshMap[subMesh];
                    int newSubMeshStart = newTriangles.Length;

                    for (int t = 0; t < oldSubMeshIndexCount; t += 3)
                    {
                        int triIndex = subMeshStart + t;

                        int A = triangles[triIndex + 0];
                        int B = triangles[triIndex + 1];
                        int C = triangles[triIndex + 2];

                        Edge realAB = new Edge(A, B);
                        Edge realBC = new Edge(B, C);
                        Edge realCA = new Edge(C, A);

                        Edge canAB = new Edge(canonicalIndexPerVertex[A], canonicalIndexPerVertex[B]);
                        Edge canBC = new Edge(canonicalIndexPerVertex[B], canonicalIndexPerVertex[C]);
                        Edge canCA = new Edge(canonicalIndexPerVertex[C], canonicalIndexPerVertex[A]);

                        bool splitAB = splitCanonicalEdges.Contains(canAB);
                        bool splitBC = splitCanonicalEdges.Contains(canBC);
                        bool splitCA = splitCanonicalEdges.Contains(canCA);

                        int splitCount = 0;
                        if (splitAB) splitCount++;
                        if (splitBC) splitCount++;
                        if (splitCA) splitCount++;

                        if (splitCount == 0)
                        {
                            AddTriangle(ref newTriangles, A, B, C);
                        }
                        else
                        {

                            int mAB = splitAB ? GetOrCreateMidpoint(realAB, ref realMidpointMap) : -1;
                            int mBC = splitBC ? GetOrCreateMidpoint(realBC, ref realMidpointMap) : -1;
                            int mCA = splitCA ? GetOrCreateMidpoint(realCA, ref realMidpointMap) : -1;


                            if (splitCount == 1)
                            {
                                if (splitAB)
                                {
                                    AddTriangle(ref newTriangles, A, mAB, C);
                                    AddTriangle(ref newTriangles, mAB, B, C);
                                }
                                else if (splitBC)
                                {
                                    AddTriangle(ref newTriangles, B, mBC, A);
                                    AddTriangle(ref newTriangles, mBC, C, A);
                                }
                                else
                                {
                                    AddTriangle(ref newTriangles, C, mCA, B);
                                    AddTriangle(ref newTriangles, mCA, A, B);
                                }
                            }
                            else if (splitCount == 2)
                            {
                                if (splitAB && splitBC)
                                {
                                    AddTriangle(ref newTriangles, A, mAB, C);
                                    AddTriangle(ref newTriangles, mAB, mBC, C);
                                    AddTriangle(ref newTriangles, mAB, B, mBC);
                                }
                                else if (splitBC && splitCA)
                                {
                                    AddTriangle(ref newTriangles, B, mBC, A);
                                    AddTriangle(ref newTriangles, mBC, mCA, A);
                                    AddTriangle(ref newTriangles, mBC, C, mCA);
                                }
                                else
                                {
                                    AddTriangle(ref newTriangles, C, mCA, B);
                                    AddTriangle(ref newTriangles, mCA, mAB, B);
                                    AddTriangle(ref newTriangles, mCA, A, mAB);
                                }
                            }
                            else
                            {
                                AddTriangle(ref newTriangles, A, mAB, mCA);
                                AddTriangle(ref newTriangles, mAB, B, mBC);
                                AddTriangle(ref newTriangles, mCA, mBC, C);
                                AddTriangle(ref newTriangles, mAB, mBC, mCA);
                            }
                        }
                    }

                    subMeshMap[subMesh] = (uint)(newTriangles.Length - newSubMeshStart);
                    subMeshStart += oldSubMeshIndexCount;
                }

                triangles.Clear();
                triangles.AddRange(newTriangles.AsArray());
                newTriangles.Dispose();
                realMidpointMap.Dispose();
                splitCanonicalEdges.Dispose();
                canonicalFromPos.Dispose();
                canonicalIndexPerVertex.Dispose();
            }
        }

        private int GetOrCreateMidpoint(Edge realEdge, ref NativeParallelHashMap<Edge, int> realMidpointMap)
        {
            if (realMidpointMap.ContainsKey(realEdge))
                return realMidpointMap[realEdge];

            int vA = realEdge.a;
            int vB = realEdge.b;

            int vNew = vertices.Length;
            realMidpointMap.Add(realEdge, vNew);

            vertices.Add((vertices[vA] + vertices[vB]) * 0.5f);

            if (uv.Length > 0) uv.Add((uv[vA] + uv[vB]) * 0.5f);
            if (uv2.Length > 0) uv2.Add((uv2[vA] + uv2[vB]) * 0.5f);
            if (uv3.Length > 0) uv3.Add((uv3[vA] + uv3[vB]) * 0.5f);
            if (uv4.Length > 0) uv4.Add((uv4[vA] + uv4[vB]) * 0.5f);
            if (uv5.Length > 0) uv5.Add((uv5[vA] + uv5[vB]) * 0.5f);
            if (uv6.Length > 0) uv6.Add((uv6[vA] + uv6[vB]) * 0.5f);
            if (uv7.Length > 0) uv7.Add((uv7[vA] + uv7[vB]) * 0.5f);
            if (uv8.Length > 0) uv8.Add((uv8[vA] + uv8[vB]) * 0.5f);

            if (normals.Length > 0)
                normals.Add(((normals[vA] + normals[vB]) * 0.5f).normalized);

            if (tangents.Length > 0)
            {
                Vector4 t = (tangents[vA] + tangents[vB]) * 0.5f;
                Vector3 xyz = new Vector3(t.x, t.y, t.z).normalized;
                tangents.Add(new Vector4(xyz.x, xyz.y, xyz.z, t.w >= 0f ? 1f : -1f));
            }

            return vNew;
        }

        private static void AddTriangle(ref NativeList<int> triList, int a, int b, int c)
        {
            triList.Add(a);
            triList.Add(b);
            triList.Add(c);
        }
    }
}
