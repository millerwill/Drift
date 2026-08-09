// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: NormalsJob.cs
//
// Author: Mikael Danielsson
// Date Created: 05-11-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

using SplineArchitect.Utility;

namespace SplineArchitect.Jobs
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct NormalsJob : IJob
    {
        public NativeList<float3> normals;
        public NativeList<int> normalMap;
        [ReadOnly] public NativeArray<NativeSegment> nativeSegments;
        [ReadOnly] public float resolution;

        public void Execute()
        {
            normalMap.Clear();
            normals.Clear();

            normalMap.Add(0);
            for (int i2 = 0; i2 < nativeSegments.Length - 1; i2++)
            {
                NativeSegment ns = nativeSegments[i2];
                NativeSegment ns2 = nativeSegments[i2 + 1];

                if (!ns.isStatic)
                {
                    // Start normals
                    float3 zNormal = BezierUtility.GetDirection(ns.anchor, ns.tangentA, ns2.tangentB, ns2.anchor, 0);
                    if (i2 == 0) zNormal = math.normalizesafe(ns.anchor - ns.tangentB);
                    float3 xNormal = math.cross(zNormal, math.mul(ns.rotation, -math.up()));
                    xNormal = math.normalizesafe(xNormal);
                    float3 yNormal = math.cross(zNormal, xNormal);
                    yNormal = math.normalizesafe(yNormal);

                    int nIndex = normals.Length;
                    normals.Add(xNormal);
                    normals.Add(yNormal);
                    normals.Add(zNormal);

                    // Calculate steps
                    float step = 1 / (resolution * (ns.length / 100));
                    step = math.clamp(step, 0.00001f, 0.25f);

                    for (float time = step; time < 1.0f; time += step)
                    {
                        bool isLastSample = i2 == nativeSegments.Length - 2 && (time + step) >= 1.0f;

                        float3 nextZNormal = BezierUtility.GetDirection(ns.anchor, ns.tangentA, ns2.tangentB, ns2.anchor, time);
                        if (isLastSample) nextZNormal = math.normalizesafe(ns2.tangentA - ns2.anchor);

                        float3 v = zNormal + nextZNormal;
                        v = math.normalizesafe(v);

                        float3 nextXNormal = xNormal - 2f * math.dot(xNormal, v) * v;
                        float3 nextYNormal = math.cross(nextZNormal, nextXNormal);
                        nextYNormal = math.normalizesafe(nextYNormal);

                        normals.Add(nextXNormal);
                        normals.Add(nextYNormal);
                        normals.Add(nextZNormal);

                        xNormal = nextXNormal;
                        yNormal = nextYNormal;
                        zNormal = nextZNormal;
                    }

                    float3 zNormalEnd = BezierUtility.GetDirection(ns.anchor, ns.tangentA, ns2.tangentB, ns2.anchor, 1);
                    float3 xNormalEnd = math.cross(zNormalEnd, math.mul(ns2.rotation, -math.up()));
                    xNormalEnd = math.normalizesafe(xNormalEnd);
                    float3 yNormalEnd = math.cross(zNormalEnd, xNormalEnd);
                    yNormalEnd = math.normalizesafe(yNormalEnd);

                    float degreeDif = GeneralUtility.SignedAngle(normals[normals.Length - 2], yNormalEnd, zNormalEnd);
                    int frameCount = (normals.Length - nIndex) / 3;

                    if (frameCount > 1 && math.abs(degreeDif) > 0.0001f)
                    {
                        float totalAngle = math.radians(degreeDif);

                        for (int i = 0; i < frameCount; i++)
                        {
                            int index = nIndex + i * 3;

                            float3 xFrame = normals[index];
                            float3 yFrame = normals[index + 1];

                            float t = (float)i / (frameCount - 1);
                            float contrastedT = SplineUtilityNative.GetContrastedSegmentTime(nativeSegments, i2 + 1, t);

                            float angle = totalAngle * contrastedT;
                            float sin = math.sin(angle);
                            float cos = math.cos(angle);

                            normals[index] = xFrame * cos + yFrame * sin;
                            normals[index + 1] = yFrame * cos - xFrame * sin;
                        }
                    }
                }

                normalMap.Add(normals.Length);
            }
        }
    }
}
