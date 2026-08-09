// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: RayIntersectMeshJob.cs
//
// Author: Mikael Danielsson
// Date Created: 27-08-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

using SplineArchitect.Utility;

namespace SplineArchitect.Jobs
{
    public struct RayIntersectMeshJob : IJobParallelFor
    {
        public NativeArray<Vector3> didIntersect;

        [ReadOnly] public NativeArray<Vector3> vertices;
        [ReadOnly] public NativeArray<int> triangles;
        [ReadOnly] public float4x4 transform;
        [ReadOnly] public Vector3 rayDirection;
        [ReadOnly] public Vector3 rayOrigin;

        public void Execute(int i)
        {
            didIntersect[i] = DidTriangleIntersect(i, transform, vertices, triangles, rayDirection, rayOrigin);
        }

        private static Vector3 DidTriangleIntersect(int i, float4x4 transform, NativeArray<Vector3> vertices, NativeArray<int> triangles, Vector3 rayDirection, Vector3 rayOrigin)
        {
            int baseIndex = i * 3;

            Vector3 p1 = math.transform(transform, vertices[triangles[baseIndex]]);
            Vector3 p2 = math.transform(transform, vertices[triangles[baseIndex + 1]]);
            Vector3 p3 = math.transform(transform, vertices[triangles[baseIndex + 2]]);
            Vector3? intersectPosition = EMeshUtility.RayIntersectedWithTriangle(rayDirection, rayOrigin, p1, p2, p3);

            return intersectPosition != null ? (Vector3)intersectPosition : new Vector3(0, -999999, 0);
        }
    }
}
