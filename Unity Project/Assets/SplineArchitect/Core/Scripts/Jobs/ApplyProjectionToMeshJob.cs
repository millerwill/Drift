// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ApplyProjectionToMeshJob.cs
//
// Author: Mikael Danielsson
// Date Created: 22-05-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace SplineArchitect.Jobs
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct ApplyProjectionToMeshJob : IJobParallelFor
    {
        public NativeArray<Vector3> vertices;
        [ReadOnly] public NativeArray<RaycastCommand> raycastCommands;
        [ReadOnly] public NativeArray<RaycastHit> results;
        [ReadOnly] public NativeArray<float> deltaData;
        [ReadOnly] public float4x4 worldToMeshLocal;
        [ReadOnly] public Vector3 splinePosition;

        public void Execute(int i)
        {
            RaycastHit hit = results[i];

            if (hit.distance <= 0f)
                return;
            
            float delta = deltaData[i];
            Vector3 worldMove = raycastCommands[i].direction * hit.distance;
            float3 localMove = math.mul((float3x3)worldToMeshLocal, worldMove);

            vertices[i] = new Vector3(vertices[i].x, vertices[i].y + localMove.y + delta, vertices[i].z) + new Vector3(0, splinePosition.y, 0);
        }
    }
}
