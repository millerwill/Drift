// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: LegacyNormalsJob.cs
//
// Author: Mikael Danielsson
// Date Created: 05-03-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;

using SplineArchitect.Utility;

namespace SplineArchitect.Jobs
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct LegacyNormalsJob : IJob
    {
        public NativeList<float3> normals;
        public NativeList<int> normalMap;
        [ReadOnly] public NativeArray<NativeSegment> nativeSegments;
        [ReadOnly] public float resolution;

        public void Execute()
        {
            normals.Clear();
            normalMap.Clear();

            NativeSegment ns = nativeSegments[0];
            NativeSegment ns2 = nativeSegments[1];

            float3 zNormal = BezierUtility.GetDirection(ns.anchor, ns.tangentA, ns2.tangentB, ns2.anchor, 0);
            float3 xNormal = math.cross(zNormal, -math.up());
            xNormal = math.normalizesafe(xNormal);
            float3 yNormal = math.cross(zNormal, xNormal);
            yNormal = math.normalizesafe(yNormal);

            normalMap.Add(0);
            normals.Add(xNormal);
            normals.Add(yNormal);
            normals.Add(zNormal);

            float3 prevT = zNormal;
            float3 prevN = xNormal;

            for (int i2 = 0; i2 < nativeSegments.Length - 1; i2++)
            {
                ns = nativeSegments[i2];
                ns2 = nativeSegments[i2 + 1];

                float step = 1 / (resolution * (ns.length / 100));
                step = Mathf.Clamp(step, 0.00001f, 0.25f);
                for (float time = step; time < 1.0f; time += step)
                {
                    float3 tiT = BezierUtility.GetDirection(ns.anchor, ns.tangentA, ns2.tangentB, ns2.anchor, time);

                    float3 v = (prevT + tiT);
                    v = math.normalizesafe(v);

                    float3 ni = prevN - 2f * math.dot(prevN, v) * v;
                    float3 bi = math.cross(tiT, ni);
                    bi = math.normalizesafe(bi);

                    normals.Add(ni);
                    normals.Add(bi);
                    normals.Add(tiT);

                    prevT = tiT;
                    prevN = ni;
                }

                normalMap.Add(normals.Length);
            }
        }
    }
}
