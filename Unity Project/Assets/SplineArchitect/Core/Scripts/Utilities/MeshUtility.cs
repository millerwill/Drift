// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MeshUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 29-10-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Object = UnityEngine.Object;

using SplineArchitect.Jobs;

namespace SplineArchitect.Utility
{
    internal class MeshUtility
    {
        private static List<int> tempTriangleContainer = new List<int>();
        private static List<int> triangleContainer = new List<int>();
        private static List<Vector3> verticesContainer = new List<Vector3>();
        private static List<Vector2> uvContainer = new List<Vector2>();
        private static List<Vector2> uv2Container = new List<Vector2>();
        private static List<Vector2> uv3Container = new List<Vector2>();
        private static List<Vector2> uv4Container = new List<Vector2>();
        private static List<Vector2> uv5Container = new List<Vector2>();
        private static List<Vector2> uv6Container = new List<Vector2>();
        private static List<Vector2> uv7Container = new List<Vector2>();
        private static List<Vector2> uv8Container = new List<Vector2>();
        private static List<Vector3> normalsContainer = new List<Vector3>();
        private static List<Vector4> tangentsContainer = new List<Vector4>();

        internal static void ReverseTriangles(Mesh mesh)
        {
            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                triangleContainer.Clear();

                mesh.GetTriangles(triangleContainer, subMeshIndex);

                for (int i = 0; i < triangleContainer.Count; i += 3)
                {
                    int temp = triangleContainer[i];
                    triangleContainer[i] = triangleContainer[i + 1];
                    triangleContainer[i + 1] = temp;
                }

                mesh.SetTriangles(triangleContainer, subMeshIndex);
            }
        }

        internal static void SetSeamlessTriangles(MeshContainer mc)
        {
            Mesh mesh = mc.GetInstanceMesh();

            NativeHashMap<int, int> vertexMap = new NativeHashMap<int, int>(mesh.vertexCount, Allocator.TempJob);
            NativeArray<Vector3> vertecies = new NativeArray<Vector3>(HandleCachedResources.FetchOriginVertices(mc), Allocator.TempJob);
            JobHandle jobHandle;

            //Iterate through all submeshes
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] subMeshTriangles = mesh.GetTriangles(i);
                if (subMeshTriangles == null || subMeshTriangles.Length == 0) continue;

                triangleContainer.Clear();

                //Create and schedule a TriangleLinkingJob for the current submesh
                TriangleLinkingJob triangleLinkingJob = new TriangleLinkingJob()
                {
                    triangles = new NativeArray<int>(subMeshTriangles, Allocator.TempJob),
                    vertices = vertecies,
                    vertextMap = vertexMap
                };

                jobHandle = triangleLinkingJob.Schedule();
                jobHandle.Complete();

                foreach(int i2 in triangleLinkingJob.triangles)
                    triangleContainer.Add(i2);

                //Set the linked triangles for the current submesh
                mesh.SetTriangles(triangleContainer, i);

                //Dispose of the nativeArray.
                triangleLinkingJob.triangles.Dispose();
            }

            vertexMap.Dispose();
            vertecies.Dispose();
        }

        internal static Mesh GetSubdividedOriginalMesh(Mesh originalMesh, int resolution, bool onlyZ, out float longestEdge, out int maxResolution)
        {
            if (originalMesh == null)
            {
                longestEdge = 0; 
                maxResolution = 0;
                return null;
            }

            triangleContainer.Clear();
            verticesContainer.Clear();
            uvContainer.Clear();
            uv2Container.Clear();
            uv3Container.Clear();
            uv4Container.Clear();
            uv5Container.Clear();
            uv6Container.Clear();
            uv7Container.Clear();
            uv8Container.Clear();
            normalsContainer.Clear();
            tangentsContainer.Clear();

            Mesh subdividedMesh = Object.Instantiate(originalMesh);

            //Get triangles and subMeshMap.
            NativeArray<uint> subMeshMap = new NativeArray<uint>(subdividedMesh.subMeshCount, Allocator.TempJob);
            for (int i = 0; i < subMeshMap.Length; i++)
            {
                subMeshMap[i] = subdividedMesh.GetIndexCount(i);

                tempTriangleContainer.Clear();
                subdividedMesh.GetTriangles(tempTriangleContainer, i);

                for (int i2 = 0; i2 < tempTriangleContainer.Count; i2++)
                {
                    triangleContainer.Add(tempTriangleContainer[i2]);
                }
            }

            //Fill triangle list
            NativeList<int> nativeTriangles = new NativeList<int>(Allocator.TempJob);
            foreach (int i in triangleContainer) nativeTriangles.Add(i);

            subdividedMesh.GetVertices(verticesContainer);
            subdividedMesh.GetUVs(0, uvContainer);
            subdividedMesh.GetUVs(1, uv2Container);
            subdividedMesh.GetUVs(2, uv3Container);
            subdividedMesh.GetUVs(3, uv4Container);
            subdividedMesh.GetUVs(4, uv5Container);
            subdividedMesh.GetUVs(5, uv6Container);
            subdividedMesh.GetUVs(6, uv7Container);
            subdividedMesh.GetUVs(7, uv8Container);
            subdividedMesh.GetNormals(normalsContainer);
            subdividedMesh.GetTangents(tangentsContainer);

            NativeList<Vector3> nativeVertices = new NativeList<Vector3>(Allocator.TempJob);
            NativeList<Vector2> nativeUv = new NativeList<Vector2>(Allocator.TempJob);
            NativeList<Vector2> nativeUv2 = new NativeList<Vector2>(Allocator.TempJob);
            NativeList<Vector2> nativeUv3 = new NativeList<Vector2>(Allocator.TempJob);
            NativeList<Vector2> nativeUv4 = new NativeList<Vector2>(Allocator.TempJob);
            NativeList<Vector2> nativeUv5 = new NativeList<Vector2>(Allocator.TempJob);
            NativeList<Vector2> nativeUv6 = new NativeList<Vector2>(Allocator.TempJob);
            NativeList<Vector2> nativeUv7 = new NativeList<Vector2>(Allocator.TempJob);
            NativeList<Vector2> nativeUv8 = new NativeList<Vector2>(Allocator.TempJob);
            NativeList<Vector3> nativeNormals = new NativeList<Vector3>(Allocator.TempJob);
            NativeList<Vector4> nativeTangents = new NativeList<Vector4>(Allocator.TempJob);

            for (int i = 0; i < verticesContainer.Count; i++)
            {
                nativeVertices.Add(verticesContainer[i]);
                nativeNormals.Add(normalsContainer[i]);
                nativeTangents.Add(tangentsContainer[i]);
                if (i < uvContainer.Count) nativeUv.Add(uvContainer[i]);
                if (i < uv2Container.Count) nativeUv2.Add(uv2Container[i]);
                if (i < uv3Container.Count) nativeUv3.Add(uv3Container[i]);
                if (i < uv4Container.Count) nativeUv4.Add(uv4Container[i]);
                if (i < uv5Container.Count) nativeUv5.Add(uv5Container[i]);
                if (i < uv6Container.Count) nativeUv6.Add(uv6Container[i]);
                if (i < uv7Container.Count) nativeUv7.Add(uv7Container[i]);
                if (i < uv8Container.Count) nativeUv8.Add(uv8Container[i]);
            }

            MeshSubdivisionJob meshSubdivisionJob = new MeshSubdivisionJob
            {
                indexFormat = subdividedMesh.indexFormat,
                longestEdgeResult = new NativeArray<float>(1, Allocator.TempJob),
                maxResolution = new NativeArray<int>(1, Allocator.TempJob),
                iterations = resolution,
                onlyZ = onlyZ,
                subMeshMap = subMeshMap,
                triangles = nativeTriangles,
                vertices = nativeVertices,
                uv = nativeUv,
                uv2 = nativeUv2,
                uv3 = nativeUv3,
                uv4 = nativeUv4,
                uv5 = nativeUv5,
                uv6 = nativeUv6,
                uv7 = nativeUv7,
                uv8 = nativeUv8,
                normals = nativeNormals,
                tangents = nativeTangents
            };

            JobHandle jobHandle = meshSubdivisionJob.Schedule();
            jobHandle.Complete();

            // Apply to instance mesh
            subdividedMesh.SetVertices(nativeVertices.AsArray());

            int startIndex = 0;
            for (int i = 0; i < subMeshMap.Length; i++)
            {
                uint subMeshIndex = subMeshMap[i];
                triangleContainer.Clear();

                for (int i2 = startIndex; i2 < subMeshIndex + startIndex; i2++)
                {
                    triangleContainer.Add(nativeTriangles[i2]);
                }

                subdividedMesh.SetIndices(triangleContainer, MeshTopology.Triangles, i);
                startIndex += (int)subMeshIndex;
            }

            if (nativeUv.Length > 0) subdividedMesh.SetUVs(0, nativeUv.AsArray());
            if (nativeUv2.Length > 0) subdividedMesh.SetUVs(1, nativeUv2.AsArray());
            if (nativeUv3.Length > 0) subdividedMesh.SetUVs(2, nativeUv3.AsArray());
            if (nativeUv4.Length > 0) subdividedMesh.SetUVs(3, nativeUv4.AsArray());
            if (nativeUv5.Length > 0) subdividedMesh.SetUVs(4, nativeUv5.AsArray());
            if (nativeUv6.Length > 0) subdividedMesh.SetUVs(5, nativeUv6.AsArray());
            if (nativeUv7.Length > 0) subdividedMesh.SetUVs(6, nativeUv7.AsArray());
            if (nativeUv8.Length > 0) subdividedMesh.SetUVs(7, nativeUv8.AsArray());
            if (nativeNormals.Length > 0) subdividedMesh.SetNormals(nativeNormals.AsArray());
            if (nativeTangents.Length > 0) subdividedMesh.SetTangents(nativeTangents.AsArray());
            longestEdge = meshSubdivisionJob.longestEdgeResult[0];
            maxResolution = meshSubdivisionJob.maxResolution[0];

            nativeTriangles.Dispose();
            nativeVertices.Dispose();
            nativeUv.Dispose();
            nativeUv2.Dispose();
            nativeUv3.Dispose();
            nativeUv4.Dispose();
            nativeUv5.Dispose();
            nativeUv6.Dispose();
            nativeUv7.Dispose();
            nativeUv8.Dispose();
            nativeNormals.Dispose();
            nativeTangents.Dispose();
            subMeshMap.Dispose();

            meshSubdivisionJob.longestEdgeResult.Dispose();
            meshSubdivisionJob.maxResolution.Dispose();

            return subdividedMesh;
        }
    }
}
