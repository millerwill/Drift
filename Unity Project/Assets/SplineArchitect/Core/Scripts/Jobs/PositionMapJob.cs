// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: PositionMapJob.cs
//
// Author: Mikael Danielsson
// Date Created: 24-03-2023
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
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct PositionMapJob : IJob
    {
        public NativeList<float3> positionMap;

        [ReadOnly] public NativeArray<NativeSegment> nativeSegments;
        [ReadOnly] public float resolution;

        public void Execute()
        {
            positionMap.Clear();

            for(float t = 0; t < 1; t += resolution)
            {
                positionMap.Add(SplineUtilityNative.GetPosition(nativeSegments, t));
            }

            positionMap.Add(SplineUtilityNative.GetPosition(nativeSegments, 1));
        }
    }
}
