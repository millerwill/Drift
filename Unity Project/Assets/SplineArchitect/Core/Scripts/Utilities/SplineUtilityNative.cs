// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineUtilityNative.cs
//
// Author: Mikael Danielsson
// Date Created: 11-03-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace SplineArchitect.Utility
{
    public static class SplineUtilityNative
    {
        public static float TimeToFixedTime(NativeList<float> distanceMap, float splineResolution, float time, bool loop)
        {
            // bring time into [0,1) or [0,1] depending on loop
            time = SplineUtility.GetValidatedTime(time, loop);
            int count = distanceMap.Length;

            float rawIndex = time / splineResolution;
            rawIndex = math.clamp(rawIndex, 0f, count - 1);

            int i0 = (int)math.floor(rawIndex);
            int i1 = math.min(i0 + 1, count - 1);

            float frac = rawIndex - i0;

            i0 = math.clamp(i0, 0, distanceMap.Length);
            i1 = math.clamp(i1, 0, distanceMap.Length);

            return math.lerp(distanceMap[i0], distanceMap[i1], frac);
        }

        public static float FixedTimeToTime(NativeList<float> distanceMap, float splineResolution, float fixedTime, bool loop)
        {
            return TimeToFixedTime(distanceMap, splineResolution, fixedTime, loop);
        }

        public static float3 GetPosition(NativeArray<NativeSegment> nativeSegments, float time)
        {
            float anchorsCount = nativeSegments.Length;
            int segment = SplineUtility.GetSegmentIndex(nativeSegments.Length, time);
            return GetSegmentPosition(nativeSegments, segment - 1, SplineUtility.GetSegmentTime(segment, anchorsCount, time));
        }

        public static float3 GetSegmentPosition(NativeArray<NativeSegment> nativeSegments, int segment, float time)
        {
            segment = math.clamp(segment, 0, nativeSegments.Length - 2);

            float3 a = nativeSegments[segment].anchor;
            float3 ata = nativeSegments[segment].tangentA;
            float3 b = nativeSegments[segment + 1].anchor;
            float3 btb = nativeSegments[segment + 1].tangentB;

            return BezierUtility.Cubic(a, ata, btb, b, time);
        }

        public static float3 GetPositionFast(
            NativeList<float3> positionMap,
            NativeArray<NativeSegment> nativeSegments,
            float resolution,
            float time)
        {
            int count = positionMap.Length;
            float rawIndex = math.saturate(time) * (count - 1);

            int i0 = (int)rawIndex;
            int i1 = math.min(i0 + 1, count - 1);

            float frac = rawIndex - i0;

            return math.lerp(positionMap[i0], positionMap[i1], frac);
        }

        public static float3 GetPositionExtended(NativeArray<NativeSegment> nativeSegments, float splineLength, float time)
        {
            int segementIndex = 0;
            float3 position = nativeSegments[0].anchor;
            if (time >= 1)
            {
                segementIndex = nativeSegments.Length - 1;
                position = nativeSegments[segementIndex].anchor;
            }

            float3 direction = -GetSegmentDirection(nativeSegments, segementIndex);

            if (time < 0)
            {
                direction = -direction;
                time = math.abs(time);
            }
            else if (time > 1)
                time -= 1;
            else
                return position;

            return position + direction * (time * splineLength);
        }

        public static float3 GetSegmentDirection(NativeArray<NativeSegment> nativeSegments, int segment)
        {
            if (segment == 0)
            {
                return math.normalize(nativeSegments[segment].tangentB - nativeSegments[segment].anchor);
            }

            return math.normalize(nativeSegments[segment].anchor - nativeSegments[segment].tangentA);
        }

        public static float3 GetDirection(NativeArray<NativeSegment> nativeSegments, float time)
        {
            if (time <= 0.00001f)
                return math.normalize(nativeSegments[0].anchor - nativeSegments[0].tangentB);
            else if (time >= 0.99999f)
                return math.normalize(nativeSegments[nativeSegments.Length - 1].tangentA - nativeSegments[nativeSegments.Length - 1].anchor);

            time = math.clamp(time, 0, 1);

            int segement = SplineUtility.GetSegmentIndex(nativeSegments.Length, time);
            if (segement < 1) segement = 1;

            float segementTime = SplineUtility.GetSegmentTime(segement, nativeSegments.Length, time);
            return BezierUtility.GetDirection(nativeSegments[segement - 1].anchor, nativeSegments[segement - 1].tangentA, 
                                              nativeSegments[segement].tangentB, nativeSegments[segement].anchor, segementTime);
        }

        public static float2 GetSadleSkew(NativeArray<NativeSegment> nativeSegments, int segment, float contrastedSegmentTime)
        {
            float2 sadleSkew = new float2(math.lerp(nativeSegments[segment - 1].saddleSkew.x, nativeSegments[segment].saddleSkew.x, contrastedSegmentTime),
                                          math.lerp(nativeSegments[segment - 1].saddleSkew.y, nativeSegments[segment].saddleSkew.y, contrastedSegmentTime));

            return sadleSkew * 0.1f;
        }

        public static float2 GetScale(NativeArray<NativeSegment> nativeSegments, int segment, float contrastedSegmentTime)
        {
            float2 scale = new float2(math.lerp(nativeSegments[segment - 1].scale.x, nativeSegments[segment].scale.x, contrastedSegmentTime),
                                      math.lerp(nativeSegments[segment - 1].scale.y, nativeSegments[segment].scale.y, contrastedSegmentTime));

            return scale;
        }

        public static float GetNoise(NativeArray<NativeSegment> nativeSegments, int segment, float contrastedSegmentTime)
        {
            float noise = math.lerp(nativeSegments[segment - 1].noise, nativeSegments[segment].noise, contrastedSegmentTime);

            return noise;
        }

        public static float GetContrastedSegmentTime(NativeArray<NativeSegment> nativeSegments, int segment, float segmentTime)
        {
            //Apply contrast
            float contrast = nativeSegments[segment - 1].contrast - nativeSegments[segment].contrast;
            contrast = nativeSegments[segment].contrast + contrast - (contrast * segmentTime);
            float numerator = math.pow(segmentTime, contrast);
            float denominator = numerator + math.pow(1 - segmentTime, contrast);

            return numerator / denominator;
        }

        public static float GetZRotationRadians(NativeArray<NativeSegment> nativeSegments, float time, float contrastedSegmentTime)
        {
            int segement = SplineUtility.GetSegmentIndex(nativeSegments.Length, time);

            if (segement < 1)
                segement = 1;

            //Get rotation value
            float rotDif = nativeSegments[segement - 1].zRot - nativeSegments[segement].zRot;
            return math.radians(nativeSegments[segement].zRot + rotDif - (rotDif * contrastedSegmentTime));
        }

        public static quaternion GetZRotation(NativeArray<NativeSegment> nativeSegments, float3 splineDirection, float fixedTime, float contrastedSegmentTime)
        {
            float radians = GetZRotationRadians(nativeSegments, fixedTime, contrastedSegmentTime);
            return quaternion.AxisAngle(splineDirection, radians);
        }

        public static (float3, float3, float3) GetNormal(int segment, float segmentTime, bool alignToEnd, 
                                                            NativeArray<NativeSegment> nativeSegments, 
                                                            NativeList<float3> normals,
                                                            NativeList<int> normalsMap,
                                                            RotationMode rotationMode = RotationMode.FOLLOW_SPLINE) 
        {
            float3 xDirection = math.right();
            float3 yDirection = math.up();
            float3 zDirection = math.forward();

            bool isStatic = nativeSegments.Length > 1 && nativeSegments[segment - 1].isStatic;

            if (!isStatic && rotationMode == RotationMode.FOLLOW_SPLINE)
            {
#if UNITY_EDITOR
                // Can only happen when trying to draw the splines triangles and the normalsMap is still dirty when changing spline type.
                if (segment >= normalsMap.Length)
                    return (xDirection, yDirection, zDirection);
#endif

                int mapStart = normalsMap[segment - 1];
                int mapEnd = normalsMap[segment];

                int frameStart = mapStart / 3;
                int frameCount = (mapEnd - mapStart) / 3;

                if (frameCount <= 0)
                    return (xDirection, yDirection, zDirection);

                float n = segmentTime * (frameCount - 1);
                int normalIndex = frameStart + (int)math.floor(n);

                // Note: Need to look in to this more later. Why is this needed?
                if (normalIndex < frameStart || (normalIndex * 3) + 2 >= mapEnd)
                    normalIndex = math.clamp(normalIndex, frameStart, (frameStart + frameCount) - 1);

                // Get directions
                xDirection = normals[normalIndex * 3];
                yDirection = normals[normalIndex * 3 + 1];
                zDirection = normals[normalIndex * 3 + 2];

                if (alignToEnd)
                {
                    xDirection = -xDirection;
                    zDirection = -zDirection;
                }
            }
            else
            {
#if UNITY_EDITOR
                // Can only happen when trying to draw the splines triangles and the normalsMap is still dirty when changing spline type.
                if (segment >= nativeSegments.Length)
                    return (xDirection, yDirection, zDirection);
#endif
                if (segment < 1) 
                    segment = 1;

                if (segment == 1 && segmentTime <= 0.00001f)
                    zDirection = math.normalizesafe(nativeSegments[0].anchor - nativeSegments[0].tangentB);
                else if (segment == nativeSegments.Length - 1 && segmentTime >= 0.99999f)
                    zDirection = math.normalizesafe(nativeSegments[nativeSegments.Length - 1].tangentA - nativeSegments[nativeSegments.Length - 1].anchor);
                else
                {
                    zDirection = BezierUtility.GetDirection(nativeSegments[segment - 1].anchor, nativeSegments[segment - 1].tangentA,
                                                            nativeSegments[segment].tangentB, nativeSegments[segment].anchor, segmentTime);
                }

                if (rotationMode == RotationMode.LOCK_UPWARDS || rotationMode == RotationMode.LOCK_UPWARDS_KEEP_Z_ROTATION)
                {
                    zDirection = new float3(zDirection.x, 0, zDirection.z);
                    zDirection = math.normalizesafe(zDirection);
                }

                if (alignToEnd)
                {
                    zDirection = -zDirection;
                }

                //Calculate directions
                xDirection = math.cross(zDirection, -math.up());
                xDirection = math.normalizesafe(xDirection);
                yDirection = math.cross(zDirection, xDirection);
                yDirection = math.normalizesafe(yDirection);
            }

            return (xDirection, yDirection, zDirection);
        }

        public static float GetNearestTimeRough(NativeArray<NativeSegment> sgements,
                                  NativeList<float3> positionMap,
                                  float splineResolution,
                                  bool loop,
                                  Vector3 point,
                                  float fixedStep,
                                  bool ignoreYAxel = false)
        {
            float timeValue = -1;
            float distance = 999999;

            for (float t = 0; t < 1; t += fixedStep)
            {
                float3 bezierPoint = GetPositionFast(positionMap, sgements, splineResolution, t);
                float d2 = ignoreYAxel ? Vector2.Distance(new Vector2(bezierPoint.x, bezierPoint.z), new Vector2(point.x, point.z)) : Vector3.Distance(bezierPoint, point);

                if (d2 < distance)
                {
                    timeValue = t;
                    distance = d2;
                }
            }

            return timeValue;
        }

        public static float GetNearestTime(NativeArray<NativeSegment> segments,
                                        NativeList<float3> positionMap,
                                        float resolution,
                                        float splineLength,
                                        bool loop,
                                        Vector3 point,
                                        int precision,
                                        float steps = 5,
                                        bool ignoreYAxel = false)
        {
            float fixedStep = 100f / splineLength / steps;
            if (fixedStep > 0.2f) fixedStep = 0.2f;
            if (fixedStep < 0.0001f) fixedStep = 0.0001f;

            float timeValue = GetNearestTimeRough(segments, positionMap, resolution, loop, point, fixedStep, ignoreYAxel);

            for (int i = precision; i > 0; i--)
            {
                //Needs to be lower then 1.999f here.
                fixedStep = fixedStep / 1.66f;
                float timeForwards = timeValue + fixedStep;
                float timeBackwards = timeValue - fixedStep;
                timeForwards = SplineUtility.GetValidatedTime(timeForwards, loop);
                timeBackwards = SplineUtility.GetValidatedTime(timeBackwards, loop);

                Vector3 pForward = GetPositionFast(positionMap, segments, resolution, timeForwards);
                float dForward = ignoreYAxel ? Vector2.Distance(new Vector2(pForward.x, pForward.z), new Vector2(point.x, point.z)) : Vector3.Distance(pForward, point);

                Vector3 pBackwards = GetPositionFast(positionMap, segments, resolution, timeBackwards);
                float dBackwards = ignoreYAxel ? Vector2.Distance(new Vector2(pBackwards.x, pBackwards.z), new Vector2(point.x, point.z)) : Vector3.Distance(pBackwards, point);

                if (dForward > dBackwards)
                {
                    timeValue = timeBackwards;
                }
                else
                {
                    timeValue = timeForwards;
                }
            }

            return timeValue;
        }

        public static NativeSegment ToNative(Vector3 anchor,
                                              Vector3 tangentA,
                                              Vector3 tangentB,
                                              Quaternion localRotation,
                                              bool isStatic,
                                              float length,
                                              float zPosition,
                                              float zRotation,
                                              float contrast,
                                              float noise,
                                              Vector2 sadleSkew,
                                              Vector2 scale)
        {
            return new NativeSegment(anchor,
                                     tangentA,
                                     tangentB,
                                     localRotation,
                                     isStatic,
                                     length,
                                     zPosition,
                                     zRotation,
                                     contrast,
                                     noise,
                                     sadleSkew,
                                     scale);
        }

        public static void CopyToNativeArray(List<Segment> segments, NativeArray<NativeSegment> nativeSegments, Space space)
        {
            int count = segments.Count;
            if (nativeSegments.Length < count)
                throw new ArgumentException($"nativeSegments too small: {nativeSegments.Length} < {count}");

            for (int i = 0; i < count; i++)
            {
                Segment s = segments[i];

                Vector3 a = s.GetPosition(ControlHandle.ANCHOR, space);
                Vector3 ta = s.GetPosition(ControlHandle.TANGENT_A, space);
                Vector3 tb = s.GetPosition(ControlHandle.TANGENT_B, space);

                nativeSegments[i] = ToNative(a, ta, tb, s.LocalRotation, s.IsStatic, s.length, s.zPosition, s.ZRotation, s.Contrast, s.Noise, s.SaddleSkew, s.Scale);
            }
        }

        public static NativeArray<NativeSegment> CreateNativeArray(List<Segment> segments, Space space, Allocator allocator)
        {
            NativeArray<NativeSegment> nativeSegments = new NativeArray<NativeSegment>(segments.Count, allocator);

            for (int i = 0; i < segments.Count; i++)
            {
                Segment s = segments[i];

                Vector3 a = s.GetPosition(ControlHandle.ANCHOR, space);
                Vector3 ta = s.GetPosition(ControlHandle.TANGENT_A, space);
                Vector3 tb = s.GetPosition(ControlHandle.TANGENT_B, space);

                nativeSegments[i] = ToNative(a, ta, tb, s.LocalRotation, s.IsStatic, s.length, s.ZPosition, s.ZRotation, s.Contrast, s.Noise, s.SaddleSkew, s.Scale);
            }

            return nativeSegments;
        }
    }
}
