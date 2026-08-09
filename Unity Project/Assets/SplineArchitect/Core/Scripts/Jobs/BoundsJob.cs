// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: BoundsJob.cs
//
// Author: Mikael Danielsson
// Date Created: 01-04-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;

namespace SplineArchitect.Jobs
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct BoundsJob : IJob
    {
        public NativeArray<Vector3> result;
        [ReadOnly] public NativeArray<Vector3> points;

        public void Execute()
        {
            if (points.Length == 0)
                return;

            Vector3 min = points[0];
            Vector3 max = points[0];

            for (int i = 1; i < points.Length; i++)
            {
                Vector3 p = points[i];

                if (p.x < min.x) min.x = p.x;
                if (p.y < min.y) min.y = p.y;
                if (p.z < min.z) min.z = p.z;

                if (p.x > max.x) max.x = p.x;
                if (p.y > max.y) max.y = p.y;
                if (p.z > max.z) max.z = p.z;
            }

            result[0] = min;
            result[1] = max;
        }
    }
}
