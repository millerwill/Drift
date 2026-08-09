// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: HermiteUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 10-04-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Collections;
using UnityEngine;

namespace SplineArchitect.Utility
{
    public static class HermiteUtility
    {
        public static float Evaluate(NativeArray<HermiteSegment> segments, float t)
        {
            if (segments.Length < 1)
                return 0;

            for (int i = 0; i < segments.Length; i++)
            {
                HermiteSegment s = segments[i];
                if (t >= s.timeStart && t <= s.timeEnd)
                {
                    float dt = s.timeEnd - s.timeStart;
                    float nt = (t - s.timeStart) / dt;
                    float nt2 = nt * nt;
                    float nt3 = nt2 * nt;

                    return
                        (2 * nt3 - 3 * nt2 + 1) * s.valueStart +
                        (nt3 - 2 * nt2 + nt) * s.tangentStart +
                        (-2 * nt3 + 3 * nt2) * s.valueEnd +
                        (nt3 - nt2) * s.tangentEnd;
                }
            }

            // Outside curve bounds: clamp or extrapolate
            if (t < segments[0].timeStart) return segments[0].valueStart;
            return segments[segments.Length - 1].valueEnd;
        }

        public static NativeArray<HermiteSegment> ConvertToNative(AnimationCurve curve, Allocator allocator)
        {
            int count = curve.length - 1;
            if (count < 1)
                return new NativeArray<HermiteSegment>(0, allocator);

            NativeArray<HermiteSegment> segments = new NativeArray<HermiteSegment>(count, allocator);

            for (int i = 0; i < count; i++)
            {
                var k0 = curve[i];
                var k1 = curve[i + 1];

                float dt = k1.time - k0.time;

                segments[i] = new HermiteSegment
                {
                    timeStart = k0.time,
                    timeEnd = k1.time,
                    valueStart = k0.value,
                    valueEnd = k1.value,
                    tangentStart = k0.outTangent * dt,
                    tangentEnd = k1.inTangent * dt
                };
            }

            return segments;
        }
    }
}
