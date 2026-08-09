// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: FollowerWorker.cs
//
// Author: Mikael Danielsson
// Date Created: 23-12-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Mathematics;

using SplineArchitect.Utility;
using SplineArchitect.Jobs;

namespace SplineArchitect.Workers
{
    internal class FollowerWorker : BaseWorker
    {
        private SplineObjectWorker parentWorker;

        // Out data
        private NativeArray<Vector3> newLocalPositions;
        private NativeArray<Quaternion> newLocalRotations;
        private NativeArray<Matrix4x4> newWorldMatrices;
        private NativeArray<float> closestLinkZPositions;
        private NativeArray<int> closestLinkIds;

        // In data
        private NativeArray<int> localSpaceMap;
        private NativeArray<Quaternion> combinedParentRotations;
        private NativeArray<Quaternion> localSplineRotations;
        private NativeArray<Vector3> localSplinePositions;
        private NativeHashMap<int, float4x4> localSpaces;
        private NativeArray<bool> alignToEndMap;
        private NativeArray<float> lockPositions;
        private NativeArray<RotationMode> rotationModes;

        // Empty data
        private NativeArray<Vector3> rightDirs;
        private NativeArray<Vector3> upDirs;
        private NativeArray<Vector3> forwardDirs;

        private JobHandle jobHandle;
        private FollowerJob followerJob;
        private List<SplineObject> splineObjects;
        private int oldsplineObjectsCount = -1;

        public FollowerWorker(SplineObjectWorker splineObjectWorker, Spline spline) : base(spline)
        {
            this.parentWorker = splineObjectWorker;
            splineObjects = new List<SplineObject>();
        }

        public void Add(SplineObject so)
        {
#if UNITY_EDITOR
            if (so.SplineParent == null)
            {
                Debug.LogWarning($"[Spline Architect] Tried to add SplineObject {so.name} with null splineParent.");
                return;
            }

            if(workerState == WorkerState.WORKING)
            {
                Debug.LogWarning($"FollowerWorker allready working! Can't add splineObject {so.name} to worker.");
                return;
            }

            if (so.Type != SplineObjectType.FOLLOWER)
            {
                Debug.LogWarning($"Can't add {so.name} so Follower Worker becouse becouse it's not a follower.");
                return;
            }
#endif
            so.assignedWorkerId = parentWorker.id;
            splineObjects.Add(so);
            workerState = WorkerState.NOT_EMPTY;
        }

        public void Deform(SplineObject so)
        {
            Add(so);
            Complete();
        }

        public override bool Start()
        {
            if (spline == null || spline.segments.Count < 2 || workerState == WorkerState.EMPTY)
            {
                Reset(true);
                return false;
            }

            if(workerState == WorkerState.WORKING)
                return false;

            EnsureNativeCapacity();

            if (spline.RootSplineObjectCount == spline.AllSplineObjectCount)
            {
                localSpaces.Add(0, float4x4.identity);
                for (int i = 0; i < splineObjects.Count; i++)
                {
                    SplineObject so = splineObjects[i];
                    localSpaceMap[i] = 0;
                    alignToEndMap[i] = so.AlignToEnd;
                    combinedParentRotations[i] = Quaternion.identity;
                    localSplineRotations[i] = so.localSplineRotation;
                    localSplinePositions[i] = so.localSplinePosition;
                    lockPositions[i] = so.LockPosition;
                    rotationModes[i] = so.RotationMode;
                }
            }
            else
            {
                for (int i = 0; i < splineObjects.Count; i++)
                {
                    SplineObject so = splineObjects[i];

                    int combinedParentHashCodes = SplineObjectUtility.GetCombinedParentHashCodes(so);
                    if (!localSpaces.ContainsKey(combinedParentHashCodes))
                        localSpaces.Add(combinedParentHashCodes, SplineObjectUtility.GetCombinedParentMatrixs(so.SoParent));

                    localSpaceMap[i] = combinedParentHashCodes;
                    alignToEndMap[i] = so.AlignToEnd;
                    combinedParentRotations[i] = SplineObjectUtility.GetCombinedParentRotations(so.SoParent);
                    localSplineRotations[i] = so.localSplineRotation;
                    localSplinePositions[i] = so.localSplinePosition;
                    lockPositions[i] = so.LockPosition;
                    rotationModes[i] = so.RotationMode;
                }
            }

            followerJob = CreateFollowerJob(
                newLocalPositions,
                newLocalRotations,
                newWorldMatrices,
                closestLinkZPositions,
                closestLinkIds,
                localSplinePositions,
                localSplineRotations,
                combinedParentRotations,
                localSpaces,
                localSpaceMap,
                alignToEndMap,
                lockPositions,
                rightDirs,
                upDirs,
                forwardDirs,
                rotationModes
            );

            jobHandle = followerJob.Schedule(splineObjects.Count, 1);
            workerState = WorkerState.WORKING;

            return true;
        }

        public override void Complete()
        {
            if ((int)workerState > 1)
                Start();

            if (workerState != WorkerState.WORKING)
            {
                Reset(true);
                return;
            }

            jobHandle.Complete();

            for (int i = 0; i < splineObjects.Count; i++)
            {
                SplineObject so = splineObjects[i];

                if (so == null)
                    continue;

                if(parentWorker.id != so.assignedWorkerId)
                    continue;

                if (so.ProjectToSurface || so.RotationMode == RotationMode.FOLLOW_PROJECTION)
                {
                    Transform splineTransform = so.SplineParent.transform;
                    Transform parentTransform = so.transform.parent;
                    Vector3 worldPoint = parentTransform.TransformPoint(followerJob.newLocalPositions[i]);

                    if (Physics.Raycast(worldPoint, -splineTransform.up, out RaycastHit hit, float.MaxValue, so.ProjectToSurfaceMask))
                    {
                        if (so.ProjectToSurface)
                        {
                            Vector3 localHitPoint = parentTransform.InverseTransformPoint(hit.point);
                            followerJob.newLocalPositions[i] = localHitPoint + new Vector3(0, so.splinePosition.y, 0);
                        }

                        if (so.RotationMode == RotationMode.FOLLOW_PROJECTION)
                        {
                            Vector3 worldForward = splineTransform.rotation * (followerJob.newLocalRotations[i] * Vector3.forward);
                            worldForward = Vector3.ProjectOnPlane(worldForward, hit.normal).normalized;

                            if (worldForward.sqrMagnitude > 0.0001f)
                            {
                                Quaternion worldRotation = Quaternion.LookRotation(worldForward, hit.normal);
                                followerJob.newLocalRotations[i] = Quaternion.Inverse(splineTransform.rotation) * worldRotation;
                            }
                        }
                    }
                }

                so.transform.SetLocalPositionAndRotation(followerJob.newLocalPositions[i], followerJob.newLocalRotations[i]);
                if (!so.IsMeshRendered()) so.RenderMeshInternal(true);

                so.assignedWorkerId = 0;
            }

            Reset(false);
        }

        public override void CompleteWithoutAssignData()
        {
            jobHandle.Complete();
            Reset(true);
        }

        public override int GetWorkCount()
        {
            return splineObjects.Count;
        }

        public override void DisposeNativeData()
        {
            jobHandle.Complete();

            if (localSplinePositions.IsCreated)
            {
                // Out data
                newLocalPositions.Dispose();
                newLocalRotations.Dispose();
                newWorldMatrices.Dispose();
                closestLinkZPositions.Dispose();
                closestLinkIds.Dispose();

                // In data
                localSpaceMap.Dispose();
                alignToEndMap.Dispose();
                localSpaces.Dispose();
                combinedParentRotations.Dispose();
                localSplineRotations.Dispose();
                localSplinePositions.Dispose();
                lockPositions.Dispose();
                rotationModes.Dispose();

                // Empty data
                rightDirs.Dispose();
                upDirs.Dispose();
                forwardDirs.Dispose();
            }
        }

        private void Reset(bool unassignSplineObjects)
        {
            if (unassignSplineObjects)
            {
                for (int i = 0; i < splineObjects.Count; i++)
                {
                    SplineObject so = splineObjects[i];

                    if (so == null)
                        continue;

                    so.assignedWorkerId = 0;
                    if (!so.IsMeshRendered()) so.RenderMeshInternal(true);
                }
            }

            if (localSpaces.IsCreated)
                localSpaces.Clear();

            splineObjects.Clear();
            workerState = WorkerState.EMPTY;
        }

        private void EnsureNativeCapacity()
        {
            if (oldsplineObjectsCount < splineObjects.Count || !localSplinePositions.IsCreated)
            {
                oldsplineObjectsCount = splineObjects.Count;

                DisposeNativeData();

                // Out data
                newLocalPositions = new NativeArray<Vector3>(splineObjects.Count, Allocator.Persistent);
                newLocalRotations = new NativeArray<Quaternion>(splineObjects.Count, Allocator.Persistent);
                newWorldMatrices = new NativeArray<Matrix4x4>(splineObjects.Count, Allocator.Persistent);
                closestLinkZPositions = new NativeArray<float>(splineObjects.Count, Allocator.Persistent);
                closestLinkIds = new NativeArray<int>(splineObjects.Count, Allocator.Persistent);

                // In data
                localSpaceMap = new NativeArray<int>(splineObjects.Count, Allocator.Persistent);
                alignToEndMap = new NativeArray<bool>(splineObjects.Count, Allocator.Persistent);
                localSpaces = new NativeHashMap<int, float4x4>(splineObjects.Count, Allocator.Persistent);
                localSplineRotations = new NativeArray<Quaternion>(splineObjects.Count, Allocator.Persistent);
                localSplinePositions = new NativeArray<Vector3>(splineObjects.Count, Allocator.Persistent);
                combinedParentRotations = new NativeArray<Quaternion>(splineObjects.Count, Allocator.Persistent);
                lockPositions = new NativeArray<float>(splineObjects.Count, Allocator.Persistent);
                rotationModes = new NativeArray<RotationMode>(splineObjects.Count, Allocator.Persistent);

                // Empty data
                rightDirs = new NativeArray<Vector3>(0, Allocator.Persistent);
                upDirs = new NativeArray<Vector3>(0, Allocator.Persistent);
                forwardDirs = new NativeArray<Vector3>(0, Allocator.Persistent);
            }
        }
    }
}
