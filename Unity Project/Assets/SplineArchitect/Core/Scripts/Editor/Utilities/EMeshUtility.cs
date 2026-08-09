// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EMeshUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 09-02-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

using SplineArchitect.Jobs;

namespace SplineArchitect.Utility
{
    public class EMeshUtility
    {
        public static bool WasMeshIntersectedByRayJob(Mesh mesh, Ray ray, Transform transform, ref List<Vector3> positions)
        {
            positions.Clear();
            bool didIntersect = false;

            RayIntersectMeshJob rayIntersectMeshJob = new RayIntersectMeshJob()
            {
                didIntersect = new NativeArray<Vector3>(mesh.triangles.Length / 3, Allocator.TempJob),
                vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.TempJob),
                triangles = new NativeArray<int>(mesh.triangles, Allocator.TempJob),
                transform = float4x4.TRS(transform.position, transform.rotation, transform.localScale),
                rayOrigin = ray.origin,
                rayDirection = ray.direction
            };

            JobHandle jobHandle = rayIntersectMeshJob.Schedule(mesh.triangles.Length / 3, 1);
            jobHandle.Complete();

            foreach(Vector3 v in rayIntersectMeshJob.didIntersect)
            {
                if(!GeneralUtility.IsEqual(v.y, -999999))
                {
                    positions.Add(v);
                    didIntersect = true;
                }
            }

            rayIntersectMeshJob.didIntersect.Dispose();
            rayIntersectMeshJob.vertices.Dispose();
            rayIntersectMeshJob.triangles.Dispose();

            return didIntersect;
        }

        public static bool WasMeshIntersectedByRay(Mesh mesh, Ray ray, Transform transform)
        {
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                Vector3 p1 = transform.TransformPoint(mesh.vertices[mesh.triangles[i]]);
                Vector3 p2 = transform.TransformPoint(mesh.vertices[mesh.triangles[i + 1]]);
                Vector3 p3 = transform.TransformPoint(mesh.vertices[mesh.triangles[i + 2]]);

                if (RayIntersectedWithTriangle(ray.direction, ray.origin, p1, p2, p3) != null)
                    return true;
            }

            return false;
        }

        public static Vector3? RayIntersectedWithTriangle(Vector3 rayDirection, Vector3 rayOrigin, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // Edges of the triangle
            Vector3 e1 = p2 - p1;
            Vector3 e2 = p3 - p1;

            // Calculate determinant
            Vector3 h = Vector3.Cross(rayDirection, e2);
            float det = Vector3.Dot(e1, h);

            // If determinant is near zero, the ray is parallel to the triangle
            const float epsilon = 0.0001f;
            if (det > -epsilon && det < epsilon) return null;

            float invDet = 1.0f / det;

            // Calculate the distance from the first vertex to the ray origin
            Vector3 s = rayOrigin - p1;
            float u = Vector3.Dot(s, h) * invDet;
            if (u < 0.0f || u > 1.0f) return null;

            // Continue with the calculation
            Vector3 q = Vector3.Cross(s, e1);
            float v = Vector3.Dot(rayDirection, q) * invDet;
            if (v < 0.0f || u + v > 1.0f) return null;

            float t = Vector3.Dot(e2, q) * invDet;
            if (t > epsilon)
            {
                return rayOrigin + t * rayDirection;
            }

            return null;
        }
    }
}
