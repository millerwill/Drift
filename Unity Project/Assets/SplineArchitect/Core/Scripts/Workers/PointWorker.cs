// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: PointWorker.cs
//
// Author: Mikael PointWorker
// Date Created: 14-01-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

using SplineArchitect.Jobs;

namespace SplineArchitect.Workers
{
    public class PointWorker : BaseWorker
    {
        // Out data
        private NativeList<Vector3> newLocalPositions;
        private NativeList<Quaternion> newLocalRotations;
        private NativeList<Vector3> rightDirs;
        private NativeList<Vector3> upDirs;
        private NativeList<Vector3> forwardDirs;
        private NativeArray<Matrix4x4> newWorldMatrices;
        private NativeArray<float> closestLinkZPositions;
        private NativeArray<int> closestLinkIds;

        // In data
        private NativeList<int> localSpaceMap;
        private NativeList<bool> alignToEndMap;
        private NativeList<float> lockPositions;
        private NativeList<Vector3> localSplinePositions;
        private NativeList<Quaternion> localSplineRotations;
        private NativeList<Quaternion> combinedParentRotations;
        private NativeList<RotationMode> rotationModes;
        private NativeHashMap<int, float4x4> localSpaces;

        private List<PointWorkerData> pointWorkerDataContainer = new List<PointWorkerData>();
        private JobHandle jobHandle;
        private FollowerJob followerJob;

        public PointWorker(Spline spline = null) : base(spline)
        {

        }

        public void Add(Vector3 splinePosition)
        {
            Add(splinePosition, float4x4.identity, false, RotationMode.FOLLOW_SPLINE);
        }

        public void Add(Vector3 splinePosition, float4x4 matrix, bool alignToEnd, RotationMode rotationMode)
        {
            if (!newLocalPositions.IsCreated)
            {
                // Out data
                newLocalPositions = new NativeList<Vector3>(4, Allocator.Persistent);
                newLocalRotations = new NativeList<Quaternion>(4, Allocator.Persistent);
                rightDirs = new NativeList<Vector3>(4, Allocator.Persistent);
                upDirs = new NativeList<Vector3>(4, Allocator.Persistent);
                forwardDirs = new NativeList<Vector3>(4, Allocator.Persistent);
                newWorldMatrices = new NativeArray<Matrix4x4>(0, Allocator.Persistent);
                closestLinkZPositions = new NativeArray<float>(0, Allocator.Persistent);
                closestLinkIds = new NativeArray<int>(0, Allocator.Persistent);

                // In data
                localSplinePositions = new NativeList<Vector3>(4, Allocator.Persistent);
                localSplineRotations = new NativeList<Quaternion>(4, Allocator.Persistent);
                localSpaceMap = new NativeList<int>(4, Allocator.Persistent);
                alignToEndMap = new NativeList<bool>(4, Allocator.Persistent);
                lockPositions = new NativeList<float>(4, Allocator.Persistent);
                combinedParentRotations = new NativeList<Quaternion>(4, Allocator.Persistent);
                rotationModes = new NativeList<RotationMode>(4, Allocator.Persistent);
                localSpaces = new NativeHashMap<int, float4x4>(4, Allocator.Persistent);
            }

            newLocalPositions.Add(splinePosition);
            newLocalRotations.Add(Quaternion.identity);
            rightDirs.Add(Vector3.zero);
            upDirs.Add(Vector3.zero);
            forwardDirs.Add(Vector3.zero);

            localSplinePositions.Add(splinePosition);
            localSplineRotations.Add(Quaternion.identity);
            localSpaceMap.Add(localSplinePositions.Length - 1);
            alignToEndMap.Add(alignToEnd);
            rotationModes.Add(rotationMode);
            lockPositions.Add(0);
            combinedParentRotations.Add(Quaternion.identity);
            localSpaces.Add(localSplinePositions.Length - 1, matrix);

            workerState = WorkerState.NOT_EMPTY;
        }

        public override bool Start()
        {
            if (spline == null || spline.segments.Count < 2)
            {
                Reset();
                return false;
            }

#if UNITY_EDITOR
            if (workerState == WorkerState.EMPTY)
            {
                Debug.LogWarning("Can't start job becouse FollowerWorker is empty.");
                return false;
            }

            if(workerState == WorkerState.WORKING)
            {
                Debug.LogWarning("FollowerWorker allready working! Can't start job.");
                return false;
            }
#endif

            followerJob = CreateFollowerJob(
                newLocalPositions.AsArray(),
                newLocalRotations.AsArray(),
                newWorldMatrices,
                closestLinkZPositions,
                closestLinkIds,
                localSplinePositions.AsArray(),
                localSplineRotations.AsArray(),
                combinedParentRotations.AsArray(),
                localSpaces,
                localSpaceMap.AsArray(),
                alignToEndMap.AsArray(),
                lockPositions.AsArray(),
                rightDirs.AsArray(),
                upDirs.AsArray(),
                forwardDirs.AsArray(),
                rotationModes.AsArray()
            );

            jobHandle = followerJob.Schedule(localSplinePositions.Length, 1);
            workerState = WorkerState.WORKING;

            return true;
        }

        public override void Complete()
        {
            pointWorkerDataContainer.Clear();
            Complete(pointWorkerDataContainer, 1);
        }

        public void Complete(List<PointWorkerData> pointWorkerData, int maxAmount = int.MaxValue)
        {
            if ((int)workerState > 1)
                Start();

            if (workerState != WorkerState.WORKING)
            {
                Reset();
                return;
            }

            jobHandle.Complete();

            for (int i = 0; i < followerJob.newLocalPositions.Length; i++)
            {
                Vector3 point = followerJob.newLocalPositions[i];
                Vector3 forward = followerJob.forwardDirs[i];
                Vector3 up = followerJob.upDirs[i];
                Vector3 right = followerJob.rightDirs[i];
                pointWorkerData.Add(new PointWorkerData(point, forward, up, right));

                if (i >= maxAmount)
                    break;
            }

            Reset();
        }

        public void Complete(out PointWorkerData p0)
        {
            pointWorkerDataContainer.Clear();
            Complete(pointWorkerDataContainer, 1);

            for (int i = pointWorkerDataContainer.Count; i < 1; i++)
                pointWorkerDataContainer.Add(new PointWorkerData());

            p0 = pointWorkerDataContainer[0];
        }

        public void Complete(out PointWorkerData p0, out PointWorkerData p1)
        {
            pointWorkerDataContainer.Clear();
            Complete(pointWorkerDataContainer, 2);

            for (int i = pointWorkerDataContainer.Count; i < 2; i++)
                pointWorkerDataContainer.Add(new PointWorkerData());

            p0 = pointWorkerDataContainer[0];
            p1 = pointWorkerDataContainer[1];
        }

        public void Complete(out PointWorkerData p0, out PointWorkerData p1, out PointWorkerData p2)
        {
            pointWorkerDataContainer.Clear();
            Complete(pointWorkerDataContainer, 3);

            for (int i = pointWorkerDataContainer.Count; i < 3; i++)
                pointWorkerDataContainer.Add(new PointWorkerData());

            p0 = pointWorkerDataContainer[0];
            p1 = pointWorkerDataContainer[1];
            p2 = pointWorkerDataContainer[2];
        }

        public void Complete(out PointWorkerData p0, out PointWorkerData p1, out PointWorkerData p2, out PointWorkerData p3)
        {
            pointWorkerDataContainer.Clear();
            Complete(pointWorkerDataContainer, 4);

            for (int i = pointWorkerDataContainer.Count; i < 4; i++)
                pointWorkerDataContainer.Add(new PointWorkerData());

            p0 = pointWorkerDataContainer[0];
            p1 = pointWorkerDataContainer[1];
            p2 = pointWorkerDataContainer[2];
            p3 = pointWorkerDataContainer[3];
        }

        public void Complete(out PointWorkerData p0, out PointWorkerData p1, out PointWorkerData p2, out PointWorkerData p3, out PointWorkerData p4)
        {
            pointWorkerDataContainer.Clear();
            Complete(pointWorkerDataContainer, 5);

            for (int i = pointWorkerDataContainer.Count; i < 5; i++)
                pointWorkerDataContainer.Add(new PointWorkerData());

            p0 = pointWorkerDataContainer[0];
            p1 = pointWorkerDataContainer[1];
            p2 = pointWorkerDataContainer[2];
            p3 = pointWorkerDataContainer[3];
            p4 = pointWorkerDataContainer[4];
        }

        public override int GetWorkCount()
        {
            return localSplinePositions.Length;
        }

        public override void CompleteWithoutAssignData()
        {
            jobHandle.Complete();
            Reset();
        }

        public override void DisposeNativeData()
        {
            if(newLocalPositions.IsCreated)
            {
                // Out data
                newLocalPositions.Dispose();
                newLocalRotations.Dispose();
                rightDirs.Dispose();
                forwardDirs.Dispose();
                upDirs.Dispose();
                newWorldMatrices.Dispose();
                closestLinkZPositions.Dispose();
                closestLinkIds.Dispose();

                // In data
                localSpaceMap.Dispose();
                alignToEndMap.Dispose();
                localSplinePositions.Dispose();
                localSplineRotations.Dispose();
                combinedParentRotations.Dispose();
                localSpaces.Dispose();
                lockPositions.Dispose();
                rotationModes.Dispose();
            }
        }

        private void Reset()
        {
            if (newLocalPositions.IsCreated)
            {
                newLocalPositions.Clear();
                newLocalRotations.Clear();
                rightDirs.Clear();
                forwardDirs.Clear();
                upDirs.Clear();

                localSpaceMap.Clear();
                alignToEndMap.Clear();
                localSplinePositions.Clear();
                localSplineRotations.Clear();
                combinedParentRotations.Clear();
                localSpaces.Clear();
                lockPositions.Clear();
                rotationModes.Clear();
            }
            workerState = WorkerState.EMPTY;
        }
    }
}
