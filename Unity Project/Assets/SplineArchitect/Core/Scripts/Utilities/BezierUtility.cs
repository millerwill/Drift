// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: BezierUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 11-06-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using Unity.Mathematics;

namespace SplineArchitect.Utility
{
    public static class BezierUtility
    {
        public static float3 Linear(float3 a, float3 b, float t)
        {
            float u = 1f - t;
            return a * u + b * t;
        }

        public static float3 Quadratic(float3 a, float3 at, float3 b, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            return a * uu + at * (2f * u * t) + b * tt;
        }

        public static float3 Cubic(float3 a, float3 ata, float3 btb, float3 b, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return a * uuu
                   + ata * (3f * uu * t)
                   + btb * (3f * u * tt)
                   + b * ttt;
        }

        public static float3 GetDirection(float3 a, float3 ata, float3 btb, float3 b, float t)
        {
            float u = 1 - t;
            float3 tangent = 3 * u * u * (ata - a);
            tangent += 6 * u * t * (btb - ata);
            tangent += 3 * t * t * (b - btb);
            return math.normalizesafe(tangent);
        }
    }
}
