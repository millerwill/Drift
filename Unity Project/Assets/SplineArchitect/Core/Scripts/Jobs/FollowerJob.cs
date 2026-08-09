// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: FollowerJob.cs
//
// Author: Mikael Danielsson
// Date Created: 06-04-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

using SplineArchitect.Utility;

namespace SplineArchitect.Jobs
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct FollowerJob : IJobParallelFor
    {
        public NativeArray<Vector3> newLocalPositions;
        public NativeArray<Quaternion> newLocalRotations;
        public NativeArray<Matrix4x4> newWorldMatrices;
        public NativeArray<float> closestLinkZPositions;
        public NativeArray<int> closestLinkIds;
        public NativeArray<Vector3> rightDirs;
        public NativeArray<Vector3> upDirs;
        public NativeArray<Vector3> forwardDirs;

        // Spline
        [ReadOnly] public float splineLength;
        [ReadOnly] public float splineResolution;
        [ReadOnly] public bool loop;
        [ReadOnly] public Matrix4x4 matrix;
        [ReadOnly] public NativeList<float> distanceMap;
        [ReadOnly] public NativeList<float3> positionMap;
        [ReadOnly] public NativeList<float3> normals;
        [ReadOnly] public NativeList<int> normalsMap;
        [ReadOnly] public NativeArray<NativeSegment> nativeSegments;
        [ReadOnly] public NativeArray<HermiteSegment> scaleCurveSegments;
        [ReadOnly] public NativeArray<NoiseLayer> noises;

        // Follower
        [ReadOnly] public NativeArray<Vector3> localSplinePositions;
        [ReadOnly] public NativeArray<Quaternion> localSplineRotations;
        [ReadOnly] public NativeArray<Quaternion> combinedParentRotations;
        [ReadOnly] public NativeArray<RotationMode> rotationModes;
        [ReadOnly] public NativeHashMap<int, float4x4> localSpaces;
        [ReadOnly] public NativeArray<int> localSpaceMap;
        [ReadOnly] public NativeArray<bool> alignToEndMap;
        [ReadOnly] public NativeArray<float> lockPositions;

        public void Execute(int i)
        {
            float3 localSplinePosition = localSplinePositions[i];
            float3 deformPoint = localSplinePosition;
            quaternion localSplineRotation = localSplineRotations[i];
            quaternion combinedParentRotation = combinedParentRotations[i];
            float4x4 localSpace = localSpaces[localSpaceMap[i]];
            bool alignToEnd = alignToEndMap[i];
            bool lockPosition = lockPositions[i] > 0.01f;
            RotationMode rotationMode = rotationModes[i];

            if (lockPosition)
                deformPoint = float3.zero;

            (float3, float3, float3, float3) tupleData = DeformPointGetNormals(i, deformPoint, localSpace, alignToEnd, rotationMode);

            float3 newLocalPosition = tupleData.Item1;
            float3 xDirection = tupleData.Item2;
            float3 yDirection = tupleData.Item3;
            float3 zDirection = tupleData.Item4;

            if (rightDirs.Length > 0) rightDirs[i] = xDirection;
            if (upDirs.Length > 0) upDirs[i] = yDirection;
            if (forwardDirs.Length > 0) forwardDirs[i] = zDirection;

            // Set new rotation
            if (lockPosition)
            {
                float3 lockPoint = newLocalPosition;
                lockPoint += zDirection * localSplinePosition.z;
                lockPoint += yDirection * localSplinePosition.y;
                lockPoint += xDirection * localSplinePosition.x;

                float3 splinePosition = math.transform(localSpace, localSplinePosition);
                float fixedTime2 = SplineUtilityNative.TimeToFixedTime(distanceMap, splineResolution, splinePosition.z / splineLength, loop);
                int segment = math.clamp(SplineUtility.GetSegmentIndex(nativeSegments.Length, fixedTime2), 1, nativeSegments.Length - 1);
                float segmentTime = SplineUtility.GetSegmentTime(segment, nativeSegments.Length, fixedTime2);
                float constratedSegmentTime = SplineUtilityNative.GetContrastedSegmentTime(nativeSegments, segment, segmentTime);
                if (float.IsNaN(constratedSegmentTime)) constratedSegmentTime = 1;

                (float3, float3, float3) normals2 = SplineUtilityNative.GetNormal(segment, segmentTime, alignToEnd, nativeSegments, normals, normalsMap);
                quaternion newLocalRotation = math.mul(quaternion.LookRotation(normals2.Item3, normals2.Item2), localSplineRotation);

                if (!GeneralUtility.IsEqual(lockPositions[i], 1, 0.01f))
                {
                    (float3, float3, float3, float3) tupleData2 = DeformPointGetNormals(i, localSplinePosition, localSpace, alignToEnd, rotationMode);
                    lockPoint = math.lerp(tupleData2.Item1, lockPoint, lockPositions[i]);
                }

                newLocalRotations[i] = newLocalRotation;
                newLocalPositions[i] = lockPoint;
            }
            else
            {
                quaternion rotation2 = quaternion.LookRotation(zDirection, yDirection);
                quaternion newLocalRotation = math.mul(math.mul(math.inverse(combinedParentRotation), rotation2),
                                              math.mul(combinedParentRotation, localSplineRotation)
                );

                newLocalRotations[i] = newLocalRotation;
                newLocalPositions[i] = newLocalPosition;
            }

            if (newWorldMatrices.Length > 0)
            {
                float4x4 localTransform = float4x4.TRS(newLocalPositions[i], newLocalRotations[i], new float3(1f, 1f, 1f));
                float4x4 worldTransform = math.mul(matrix, localTransform);
                newWorldMatrices[i] = worldTransform;
            }

            float3 spCrossing = math.transform(localSpace, localSplinePositions[i]);

            if (alignToEnd)
                spCrossing.z = splineLength - spCrossing.z;

            if (closestLinkZPositions.Length > 0)
            {
                float closestDis = float.MaxValue;
                int closestId = -1;
                float closestZ = 0f;

                for (int i2 = 0; i2 < nativeSegments.Length; i2++)
                {
                    float z = nativeSegments[i2].zPosition;
                    float dis = math.abs(z - spCrossing.z);

                    if (dis < closestDis)
                    {
                        closestDis = dis;
                        closestId = i2;
                        closestZ = z;
                    }
                }

                closestLinkZPositions[i] = closestZ;
                closestLinkIds[i] = closestId;
            }
        }

        private (float3, float3, float3, float3) DeformPointGetNormals(int i, float3 deformPoint, float4x4 localSpace, bool alignToEnd, RotationMode rotationMode)
        {
            // To spline space
            deformPoint = math.transform(localSpace, deformPoint);

            if (alignToEnd) deformPoint.z = splineLength - deformPoint.z;
            float time = deformPoint.z / splineLength;
            float fixedTime = SplineUtilityNative.TimeToFixedTime(distanceMap, splineResolution, time, loop);

            int segment = math.clamp(SplineUtility.GetSegmentIndex(nativeSegments.Length, fixedTime), 1, nativeSegments.Length - 1);
            float segmentTime = SplineUtility.GetSegmentTime(segment, nativeSegments.Length, fixedTime);
            float constratedSegmentTime = SplineUtilityNative.GetContrastedSegmentTime(nativeSegments, segment, segmentTime);
            if (float.IsNaN(constratedSegmentTime)) constratedSegmentTime = 1;

            //Is exstension
            float3 splinePoint;
            if (!loop && (time >= 1 || time <= 0)) splinePoint = SplineUtilityNative.GetPositionExtended(nativeSegments, splineLength, time);
            else splinePoint = SplineUtilityNative.GetPositionFast(positionMap, nativeSegments, splineResolution, fixedTime);

            // Get normal
            (float3, float3, float3) normal = SplineUtilityNative.GetNormal(segment, segmentTime, alignToEnd, nativeSegments, normals, normalsMap, rotationMode);
            float3 xDirection = normal.Item1;
            float3 yDirection = normal.Item2;
            float3 zDirection = normal.Item3;

            //Saddle skew
            float2 saddleSkew = SplineUtilityNative.GetSadleSkew(nativeSegments, segment, constratedSegmentTime);
            deformPoint.y += saddleSkew.y * (deformPoint.x * deformPoint.x);
            deformPoint.x += saddleSkew.x * (deformPoint.y * deformPoint.y) * -math.sign(deformPoint.x);

            //Scale
            float2 scale = SplineUtilityNative.GetScale(nativeSegments, segment, constratedSegmentTime);

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

            deformPoint.x *= scale.x;
            deformPoint.y *= scale.y;

            //NoiseLayer
            float noiseModification = SplineUtilityNative.GetNoise(nativeSegments, segment, constratedSegmentTime);
            deformPoint.y += NoiseUtility.GetNoiseValue(noises, deformPoint, noiseModification);

            if (rotationMode != RotationMode.LOCK_UPWARDS && rotationMode != RotationMode.FOLLOW_PROJECTION)
            {
                //Rotation
                quaternion rotation = SplineUtilityNative.GetZRotation(nativeSegments, zDirection, fixedTime, constratedSegmentTime);
                if (!alignToEnd) rotation = math.inverse(rotation);
                xDirection = math.mul(rotation, xDirection);
                yDirection = math.mul(rotation, yDirection);
            }

            //Calculate the new world position
            deformPoint = splinePoint + xDirection * deformPoint.x + yDirection * deformPoint.y;

            // Set new position
            float3 newLocalPosition = math.transform(math.inverse(localSpace), deformPoint);

            return (newLocalPosition, xDirection, yDirection, zDirection);
        }
    }
}
