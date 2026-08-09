// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: GeneralUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 12-02-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.IO;

using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SplineArchitect.Utility
{
    public class GeneralUtility
    {
        private static Vector3[] boundsCorners = new Vector3[8];

        public static bool IsEqual(Quaternion a, Quaternion b, float epsilon = 0.0001f)
        {
            if (Math.Abs(a.x - b.x) > epsilon)
                return false;

            if (Math.Abs(a.y - b.y) > epsilon)
                return false;

            if (Math.Abs(a.z - b.z) > epsilon)
                return false;

            if (Math.Abs(a.w - b.w) > epsilon)
                return false;

            return true;
        }

        public static bool IsEqual(Bounds a, Bounds b, float epsilon = 0.0001f)
        {
            if (!IsEqual(a.min, b.min, epsilon))
                return false;

            if (!IsEqual(a.max, b.max, epsilon))
                return false;

            return true;
        }

        public static bool IsEqual(Color a, Color b, float epsilon = 0.0001f)
        {
            if (Math.Abs(a.r - b.r) > epsilon)
                return false;

            if (Math.Abs(a.g - b.g) > epsilon)
                return false;

            if (Math.Abs(a.b - b.b) > epsilon)
                return false;

            if (Math.Abs(a.a - b.a) > epsilon)
                return false;

            return true;
        }

        public static bool IsEqual(Rect a, Rect b, float epsilon = 0.0001f)
        {
            if (Math.Abs(a.x - b.x) > epsilon)
                return false;

            if (Math.Abs(a.y - b.y) > epsilon)
                return false;

            if (Math.Abs(a.width - b.width) > epsilon)
                return false;

            if (Math.Abs(a.height - b.height) > epsilon)
                return false;

            return true;
        }

        public static bool IsEqual(Vector3 a, Vector3 b, float epsilon = 0.0001f)
        {
            if (!IsEqual(a.x, b.x, epsilon))
                return false;

            if (!IsEqual(a.y, b.y, epsilon))
                return false;

            if (!IsEqual(a.z, b.z, epsilon))
                return false;

            return true;
        }

        public static bool IsEqualFast(float3 a, float3 b, float epsilon = 0.0001f)
        {
            return math.lengthsq(a - b) < epsilon * epsilon;
        }

        public static bool IsEqualFast(Quaternion a, Quaternion b, float epsilon = 0.0001f)
        {
            Vector3 av = Vector3.zero;
            av.x = a.x;
            av.y = a.y;
            av.z = a.z + a.w;

            Vector3 ab = Vector3.zero;
            ab.x = b.x;
            ab.y = b.y;
            ab.z = b.z + b.w;

            return IsEqualFast(av, ab);
        }

        public static bool IsEqualFast(Vector3 a, Vector3 b, float epsilon = 0.0001f)
        {
            return (a - b).sqrMagnitude < epsilon * epsilon;
        }

        public static bool IsEqual(float a, float b, float epsilon = 0.0001f)
        {
            return Math.Abs(a - b) < epsilon;
        }

        public static bool IsEqual(AnimationCurve a, AnimationCurve b)
        {
            if (a.length != b.length || a.preWrapMode != b.preWrapMode || a.postWrapMode != b.postWrapMode)
                return false;
            for (int i = 0; i < a.length; i++)
            {
                if (a.keys[i].time != b.keys[i].time || a.keys[i].value != b.keys[i].value)
                    return false;
            }

            return true;
        }

        public static bool IsZero(float value, float epsilon = 0.0001f)
        {
            return epsilon > value && value > -epsilon;
        }

        public static bool IsZero(double value, double epsilon = 0.00000000001f)
        {
            return epsilon > value && value > -epsilon;
        }

        public static bool IsZero(Vector3 a, float epsilon = 0.0001f)
        {
            if (!IsZero(a.x, epsilon))
                return false;

            if (!IsZero(a.y, epsilon))
                return false;

            if (!IsZero(a.z, epsilon))
                return false;

            return true;
        }

        public static bool IsZero(Quaternion a, float epsilon = 0.0001f)
        {
            if (!IsZero(a.x, epsilon))
                return false;

            if (!IsZero(a.y, epsilon))
                return false;

            if (!IsZero(a.z, epsilon))
                return false;

            if (!IsZero(a.w, epsilon))
                return false;

            return true;
        }

        public static bool IsInsideBoundsIgnoringY(Bounds bounds, Vector3 point)
        {
            return point.x >= bounds.min.x && point.x <= bounds.max.x
                && point.z >= bounds.min.z && point.z <= bounds.max.z;
        }

        public static float RoundToClosest(float value, float roundPoint)
        {
            if (roundPoint <= 0)
                return value;

            bool wasNeg = value < 0;
            value = Mathf.Abs(value);

            float valueUpwards = value;
            float valueDownwards = value;

            valueUpwards = (roundPoint + valueUpwards) - (value % roundPoint);
            valueDownwards -= value % roundPoint;

            value = value - valueDownwards < valueUpwards - value ? valueDownwards : valueUpwards;

            return wasNeg ? -value : value;
        }

        public static Vector3 RoundToClosest(Vector3 value, float roundPoint)
        {
            value.x = RoundToClosest(value.x, roundPoint);
            value.y = RoundToClosest(value.y, roundPoint);
            value.z = RoundToClosest(value.z, roundPoint);

            return value;
        }

        public static Vector3 RotateAroundCenter(Vector3 point, Vector3 center, Vector3 upDirection, float amount)
        {
            Quaternion rotation = Quaternion.AngleAxis(amount, upDirection);
            Vector3 dir = point - center;
            dir = rotation * dir;
            return center + dir;
        }

        public static Vector3 RotateAroundCenterXZ(Vector3 point, Vector3 center, float amount)
        {
            float x = point.x - center.x;
            float z = point.z - center.z;

            float rotatedX = x * Mathf.Cos(amount) - z * Mathf.Sin(amount);
            float rotatedZ = x * Mathf.Sin(amount) + z * Mathf.Cos(amount);

            return new Vector3(center.x + rotatedX, point.y, center.z + rotatedZ);
        }

        public static Bounds TransformBounds(Bounds localBounds, float4x4 aToB)
        {
            // Unpack center and extents
            Vector3 c = localBounds.center;
            Vector3 e = localBounds.extents;

            // We'll build the 8 corners of the A-space box and transform them
            Vector3 bMin = new Vector3(+float.PositiveInfinity, +float.PositiveInfinity, +float.PositiveInfinity);
            Vector3 bMax = new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity);

            // Loop over each combination of extents
            for (int ix = -1; ix <= 1; ix += 2)
                for (int iy = -1; iy <= 1; iy += 2)
                    for (int iz = -1; iz <= 1; iz += 2)
                    {
                        // Build the corner in A-space
                        float3 cornerA = new float3(
                            c.x + ix * e.x,
                            c.y + iy * e.y,
                            c.z + iz * e.z
                        );

                        // Transform into B-space
                        float3 cornerBf = math.transform(aToB, cornerA);
                        Vector3 cornerB = new Vector3(cornerBf.x, cornerBf.y, cornerBf.z);

                        // Accumulate min/max
                        bMin = Vector3.Min(bMin, cornerB);
                        bMax = Vector3.Max(bMax, cornerB);
                    }

            // Build the resulting axis-aligned Bounds in B-space
            Bounds b = new Bounds();
            b.SetMinMax(bMin, bMax);
            return b;
        }

        public static Bounds TransformBoundsToLocalSpace(Bounds worldBounds, Transform t)
        {
            Vector3 c = worldBounds.center;
            Vector3 e = worldBounds.extents;
            int idx = 0;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        boundsCorners[idx++] = c + Vector3.Scale(e, new Vector3(x, y, z));

            Vector3 lMin = Vector3.positiveInfinity;
            Vector3 lMax = Vector3.negativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                Vector3 lc = t.InverseTransformPoint(boundsCorners[i]);
                lMin = Vector3.Min(lMin, lc);
                lMax = Vector3.Max(lMax, lc);
            }

            Bounds lb = new Bounds();
            lb.SetMinMax(lMin, lMax);
            return lb;
        }

        public static Bounds TransformBoundsToWorldSpace(Bounds localBounds, Transform t)
        {
            // get all 8 corners in local space
            Vector3 c = localBounds.center;
            Vector3 e = localBounds.extents;
            int idx = 0;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        boundsCorners[idx++] = c + Vector3.Scale(e, new Vector3(x, y, z));

            Vector3 wMin = Vector3.positiveInfinity;
            Vector3 wMax = Vector3.negativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                Vector3 wc = t.TransformPoint(boundsCorners[i]);
                wMin = Vector3.Min(wMin, wc);
                wMax = Vector3.Max(wMax, wc);
            }

            Bounds wb = new Bounds();
            wb.SetMinMax(wMin, wMax);
            return wb;
        }

        internal static string GetMemorySizeFormat(float size)
        {
            string sizeFormat = "bytes";

            if (size > 999)
            {
                size = size / 1024;
                sizeFormat = "kb";
            }
            if (size > 999)
            {
                size = size / 1024;
                sizeFormat = "mb";
            }
            if (size > 999)
            {
                size = size / 1024;
                sizeFormat = "gb";
            }

            return Mathf.Round(size) + " " + sizeFormat;
        }

        internal static float BoostedValue(float x, float k, float p)
        {
            float modifier = 1f + k / Mathf.Pow(x, p);
            return x * modifier;
        }

        public static float SignedAngle(float3 from, float3 to, float3 axis)
        {
            float3 fromN = math.normalizesafe(from);
            float3 toN = math.normalizesafe(to);
            float3 axisN = math.normalizesafe(axis);

            float unsignedAngle = math.degrees(math.acos(math.clamp(math.dot(fromN, toN), -1f, 1f)));
            float crossDot = math.dot(axisN, math.cross(fromN, toN));

            float sign = crossDot < 0f ? -1f : 1f;
            return unsignedAngle * sign;
        }

#if UNITY_EDITOR
        internal static string GetAssetPathOnlyEditor(Object o, bool full = false)
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(o);
            if (path.Length > 0 && full)
            {
                path = path.Substring("Assets/".Length);
                path = Path.Combine(Application.dataPath, path);
            }

            return path.Replace("\\", "/");
        }
#endif
    }
}
