// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: NearestPointsSplinesJob.cs
//
// Author: Mikael Danielsson
// Date Created: 04-11-2025
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
    public struct NearestPointsSplinesJob : IJob
    {
        public NativeArray<Vector3> points;
        public NativeArray<double> times;

        [ReadOnly] public NativeArray<NativeSegment> nativeSegmentsSpline1;
        [ReadOnly] public float spline1Length;
        [ReadOnly] public NativeArray<NativeSegment> nativeSegmentsSpline2;
        [ReadOnly] public float spline2Length;
        [ReadOnly] public float stepsPer100Meter;
        [ReadOnly] public int precision;

        public void Execute()
        {
            if (GeneralUtility.IsZero(spline1Length)) spline1Length = 1;
            if (GeneralUtility.IsZero(spline2Length)) spline2Length = 1;

            double steps1 = 100 / spline1Length / stepsPer100Meter;
            steps1 = math.clamp(steps1, 0.0001f, 0.2f);
            double steps2 = 100 / spline2Length / stepsPer100Meter;
            steps2 = math.clamp(steps2, 0.0001f, 0.2f);

            double disCheck = 99999;
            double nearestTime1 = 0.5f;
            double nearestTime2 = 0.5f;
            double range1 = 0.5f;
            double range2 = 0.5f;
            float3 point1 = float3.zero;
            float3 point2 = float3.zero;

            for (int i = 0; i < precision; i++)
            {
                double st1 = nearestTime1 - range1;
                double et1 = nearestTime1 + range1;
                double st2 = nearestTime2 - range2;
                double et2 = nearestTime2 + range2;

                for (double t = st1; t < et1 + steps1; t += steps1)
                {
                    float3 pos1 = SplineUtilityNative.GetPosition(nativeSegmentsSpline1, (float)math.clamp(t, 0, 1));

                    for (double t2 = st2; t2 < et2 + steps2; t2 += steps2)
                    {
                        float3 pos2 = SplineUtilityNative.GetPosition(nativeSegmentsSpline2, (float)math.clamp(t2, 0, 1));

                        double dis = math.distance(pos1, pos2);

                        if (dis < disCheck)
                        {
                            disCheck = dis;
                            point1 = pos1;
                            point2 = pos2;
                            nearestTime1 = t;
                            nearestTime2 = t2;
                        }
                    }
                }

                range1 = steps1 * 2;
                range2 = steps2 * 2;
                steps1 = steps1 / 2;
                steps2 = steps2 / 2;

                if (GeneralUtility.IsZero(steps1) || GeneralUtility.IsZero(steps2))
                    break;
            }

            points[0] = point1;
            points[1] = point2;
            times[0] = nearestTime1;
            times[1] = nearestTime2;
        }
    }
}
