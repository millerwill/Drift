// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: InstanceJob.cs
//
// Author: Mikael Danielsson
// Date Created: 16-06-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using SplineArchitect.Utility;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SplineArchitect.Jobs
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct InstanceJob : IJobParallelFor
    {
        public NativeArray<NativeSplineInstance> nativeSplineInstance;
        public NativeArray<Matrix4x4> matrices;

        // Spline
        [ReadOnly] public float splineLength;
        [ReadOnly] public float splineResolution;
        [ReadOnly] public int splineFrame;
        [ReadOnly] public int frameSpreading;
        [ReadOnly] public bool isSplineLooping;
        [ReadOnly] public Matrix4x4 splinMatrix;
        [ReadOnly] public NativeList<float> distanceMap;
        [ReadOnly] public NativeList<float3> positionMap;
        [ReadOnly] public NativeList<float3> normals;
        [ReadOnly] public NativeList<int> normalsMap;
        [ReadOnly] public NativeArray<NativeSegment> nativeSegments;
        [ReadOnly] public NativeArray<HermiteSegment> scaleCurveSegments;
        [ReadOnly] public NativeArray<NoiseLayer> noises;
        [ReadOnly] public SplineOptimizationFlags optimizationFlags;

        public void Execute(int i)
        {
            int version = nativeSplineInstance[i].version;
            int processedVersion = nativeSplineInstance[i].processedVersion;

            bool useLinkCrossings = (optimizationFlags & SplineOptimizationFlags.DISABLE_LINK_CROSSINGS) == 0;

            if (version == processedVersion)
                return;

            if ((i % frameSpreading) != splineFrame)
                return;

            // General data
            float3 splinePosition = nativeSplineInstance[i].SplinePosition;
            quaternion splineRotation = nativeSplineInstance[i].SplineRotation;
            bool alignToEnd = nativeSplineInstance[i].AlignToEnd;
            RotationMode rotationMode = nativeSplineInstance[i].RotationMode;

            // Get world data
            (float3, float3, float3, float3) tupleData = DeformPointGetNormals(splinePosition, alignToEnd, rotationMode);

            // Directions
            float3 localPosition = tupleData.Item1;
            float3 yDirection = math.normalize(tupleData.Item3);
            float3 zDirection = math.normalize(tupleData.Item4);

            quaternion splineFrameRotation = quaternion.LookRotation(zDirection, yDirection);
            quaternion finalLocalRotation = math.mul(splineFrameRotation, splineRotation);

            float4x4 localMatrix = float4x4.TRS(localPosition, finalLocalRotation, nativeSplineInstance[i].Scale);
            float4x4 worldMatrix = math.mul((float4x4)splinMatrix, localMatrix);

            matrices[i] = (Matrix4x4)worldMatrix;

            // Link corssing
            float3 crossingPoint = splinePosition;
            if (alignToEnd) crossingPoint.z = splineLength - crossingPoint.z;
            float closestDis = float.MaxValue;
            int closestId = -1;
            float closestZ = 0f;
            if (useLinkCrossings)
            {
                for (int i2 = 0; i2 < nativeSegments.Length; i2++)
                {
                    float z = nativeSegments[i2].zPosition;
                    float dis = math.abs(z - crossingPoint.z);

                    if (dis < closestDis)
                    {
                        closestDis = dis;
                        closestId = i2;
                        closestZ = z;
                    }
                }
            }
            float closestLinkZPosition = closestZ;
            float closestLinkId = closestId;

            // Set link data
            NativeSplineInstance newNsi = nativeSplineInstance[i];
            newNsi.closestSegmentZPosition = closestLinkZPosition;
            newNsi.closestSegmentIndex = closestId;
            newNsi.processedVersion = version;
            nativeSplineInstance[i] = newNsi;
        }

        private (float3, float3, float3, float3) DeformPointGetNormals(float3 point, bool alignToEnd, RotationMode rotationMode)
        {
            bool useNoise = (optimizationFlags & SplineOptimizationFlags.DISABLE_NOISE) == 0;
            bool useScale = (optimizationFlags & SplineOptimizationFlags.DISABLE_SCALE) == 0;
            bool useSaddleSkew = (optimizationFlags & SplineOptimizationFlags.DISABLE_SADDLE_SKEW) == 0;
            bool useZRotation = (optimizationFlags & SplineOptimizationFlags.DISABLE_Z_ROTATION) == 0;
            bool useSplineProfile = (optimizationFlags & SplineOptimizationFlags.DISABLE_SPLINE_PROFILE) == 0;

            if (alignToEnd) point.z = splineLength - point.z;
            float time = point.z / splineLength;
            float fixedTime = SplineUtilityNative.TimeToFixedTime(distanceMap, splineResolution, time, isSplineLooping);

            int segment = math.clamp(SplineUtility.GetSegmentIndex(nativeSegments.Length, fixedTime), 1, nativeSegments.Length - 1);
            float segmentTime = SplineUtility.GetSegmentTime(segment, nativeSegments.Length, fixedTime);
            float constratedSegmentTime = SplineUtilityNative.GetContrastedSegmentTime(nativeSegments, segment, segmentTime);
            if (float.IsNaN(constratedSegmentTime)) constratedSegmentTime = 1;

            //Is exstension
            float3 splinePoint;
            if (!isSplineLooping && (time >= 1 || time <= 0)) splinePoint = SplineUtilityNative.GetPositionExtended(nativeSegments, splineLength, time);
            else splinePoint = SplineUtilityNative.GetPositionFast(positionMap, nativeSegments, splineResolution, fixedTime);

            // Get normal
            (float3, float3, float3) normal = SplineUtilityNative.GetNormal(segment, segmentTime, alignToEnd, nativeSegments, normals, normalsMap, rotationMode);
            float3 xDirection = normal.Item1;
            float3 yDirection = normal.Item2;
            float3 zDirection = normal.Item3;

            if (useSaddleSkew)
            {
                //Saddle skew
                float2 saddleSkew = SplineUtilityNative.GetSadleSkew(nativeSegments, segment, constratedSegmentTime);
                point.y += saddleSkew.y * (point.x * point.x);
                point.x += saddleSkew.x * (point.y * point.y) * -math.sign(point.x);
            }

            if (useScale)
            {
                //Scale
                float2 scale = SplineUtilityNative.GetScale(nativeSegments, segment, constratedSegmentTime);

                if (useSplineProfile)
                {
                    if (scaleCurveSegments.Length > 0)
                    {
                        float hermiteScale = HermiteUtility.Evaluate(scaleCurveSegments, fixedTime);

                        if (hermiteScale > 10)
                            hermiteScale = 10;

                        if (hermiteScale < 0)
                            hermiteScale = 0;

                        scale.x *= hermiteScale;
                        scale.y *= hermiteScale;
                    }
                }

                point.x *= scale.x;
                point.y *= scale.y;
            }

            if (useNoise)
            {
                //NoiseLayer
                float noiseModification = SplineUtilityNative.GetNoise(nativeSegments, segment, constratedSegmentTime);
                point.y += NoiseUtility.GetNoiseValue(noises, point, noiseModification);
            }

            if (useZRotation)
            {
                if (rotationMode != RotationMode.LOCK_UPWARDS && rotationMode != RotationMode.FOLLOW_PROJECTION)
                {
                    //Rotation
                    quaternion rotation = SplineUtilityNative.GetZRotation(nativeSegments, zDirection, fixedTime, constratedSegmentTime);
                    if (!alignToEnd) rotation = math.inverse(rotation);
                    xDirection = math.mul(rotation, xDirection);
                    yDirection = math.mul(rotation, yDirection);
                }
            }

            //Calculate the new world position
            point = splinePoint + xDirection * point.x + yDirection * point.y;

            return (point, xDirection, yDirection, zDirection);
        }
    }
}
