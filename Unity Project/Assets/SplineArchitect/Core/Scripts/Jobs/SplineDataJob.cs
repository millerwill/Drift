// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineDataJob.cs
//
// Author: Mikael Danielsson
// Date Created: 16-04-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using SplineArchitect.Utility;

namespace SplineArchitect.Jobs
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct SplineDataJob : IJob
    {
        public NativeList<float> distanceMap;
        public NativeList<float3> positionMapLocal;
        public NativeArray<float> splineLength;
        public NativeArray<float> segmentZPositions;
        public NativeArray<float> segmentLengths;

        [ReadOnly] public NativeArray<NativeSegment> nativeSegments;
        [ReadOnly] public float resolution;
        [ReadOnly] public float samplingStep;
        [ReadOnly] public bool calculateLocalPositions;

        public void Execute()
        {
            int segCount = nativeSegments.Length - 1;
            int lowSteps = (int)math.floor(1f / resolution);
            int highSteps = (int)math.floor(1f / samplingStep);
            float invLow = 1f / lowSteps;
            float invHigh = 1f / highSteps;
            float lowTotal = 0f;

            // --- Low-Res Pass ---
            if (calculateLocalPositions)
            {
                positionMapLocal.Clear();
                positionMapLocal.Capacity = lowSteps + 2;
                positionMapLocal.Add(SplineUtilityNative.GetPosition(nativeSegments, 0f));
            }

            float3 lastLow = SplineUtilityNative.GetPosition(nativeSegments, 0f);
            for (int i = 1; i <= lowSteps; i++)
            {
                float t = (i == lowSteps) ? 1f : i * invLow;
                float3 p = SplineUtilityNative.GetPosition(nativeSegments, t);
                lowTotal += math.distance(lastLow, p);
                lastLow = p;

                if (calculateLocalPositions)
                    positionMapLocal.Add(p);
            }

            // --- High-Res Pass ---
            distanceMap.Clear();
            distanceMap.Capacity = highSteps + 2;
            distanceMap.Add(0f);

            float totalLen = 0f;
            float nextThreshold = invLow;
            float3 lastHigh = SplineUtilityNative.GetPosition(nativeSegments, 0f);

            int currentSegment = 0;
            segmentZPositions[0] = 0f;
            segmentLengths[segmentLengths.Length - 1] = 0f;

            for (int i = 1; i <= highSteps; i++)
            {
                float t = (i == highSteps) ? 1f : i * invHigh;
                float3 p = SplineUtilityNative.GetPosition(nativeSegments, t);

                float d = math.distance(lastHigh, p);
                totalLen += d;
                lastHigh = p;

                if (totalLen > lowTotal * nextThreshold)
                {
                    distanceMap.Add(t);
                    nextThreshold += invLow;
                }

                int newSeg = math.min((int)(t * segCount), segCount - 1);
                if (newSeg != currentSegment)
                {
                    currentSegment = newSeg;
                    segmentZPositions[newSeg] = totalLen;
                    segmentLengths[newSeg - 1] = totalLen - segmentZPositions[newSeg - 1];
                }
            }

            distanceMap.Add(1f);

            // Finalize last segment
            segmentZPositions[currentSegment + 1] = totalLen;
            segmentLengths[currentSegment] = totalLen - segmentZPositions[currentSegment];

            // Write out total spline length
            splineLength[0] = totalLen;
        }
    }
}