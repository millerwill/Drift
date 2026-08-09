// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObjectWorker.cs
//
// Author: Mikael Danielsson
// Date Created: 16-01-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;

namespace SplineArchitect.Workers
{
    public class SplineObjectWorker
    {
        private static ulong idCounter = 0;
        internal ulong id = 1;

        private bool isWorking;

        private Spline spline;
        private FollowerWorker followerWorker;
        private List<DeformationWorker> deformationWorkers;
        private HashSet<SplineObject> waitingList;
        private HashSet<SplineObject> pendingSplineObjectUpdates;

        public SplineObjectWorker(Spline spline = null)
        {
            idCounter++;
            this.id = idCounter;

            this.spline = spline;

            followerWorker = new FollowerWorker(this, spline);
            deformationWorkers = new List<DeformationWorker>();
            pendingSplineObjectUpdates = new HashSet<SplineObject>();
            waitingList = new HashSet<SplineObject>();
        }

        public void Add(SplineObject so)
        {
            if (isWorking)
            {
                waitingList.Add(so);
                return;
            }

            AddInternal(so);
        }

        private void AddInternal(SplineObject so)
        {
            if (so.Type == SplineObjectType.DEFORMATION)
            {
                DeformationWorker deformationWorker = GetDeformationWorkerFromPool();
                deformationWorker.Add(so);
                pendingSplineObjectUpdates.Add(so);
            }
            else
            {
                followerWorker.Add(so);
            }
        }

        public void Start()
        {
            isWorking = false;

            if (spline.isInvalidShape)
            {
                waitingList.Clear();

                foreach (DeformationWorker dw in deformationWorkers)
                    dw.CompleteWithoutAssignData();

                followerWorker.CompleteWithoutAssignData();
                return;
            }

            foreach (SplineObject so in waitingList) AddInternal(so);
            waitingList.Clear();

            foreach (DeformationWorker dw in deformationWorkers)
            {
                bool startedDw = dw.Start();
                if (startedDw) isWorking = true;
            }

            bool startedFollower = followerWorker.Start();
            if (startedFollower) isWorking = true;
        }

        public void Complete()
        {
            foreach (DeformationWorker dw in deformationWorkers)
                dw.Complete();

            followerWorker.Complete();
            isWorking = false;

            foreach (SplineObject so in pendingSplineObjectUpdates)
            {
                if (so == null)
                    continue;

                so.UpdateExternalComponents();
            }
            pendingSplineObjectUpdates.Clear();
        }

        public void CompleteWithoutAssignData()
        {
            foreach (DeformationWorker dw in deformationWorkers)
                dw.CompleteWithoutAssignData();

            followerWorker.CompleteWithoutAssignData();
            isWorking = false;

            pendingSplineObjectUpdates.Clear();
        }

        public bool Contains(SplineObject so)
        {
            return so.assignedWorkerId == id;
        }

        public void Deform(SplineObject so, bool updateExternalComponents = false)
        {
            if (so.Type == SplineObjectType.DEFORMATION)
            {
                DeformationWorker deformationWorker = GetDeformationWorkerFromPool();
                deformationWorker.Deform(so);

                if(updateExternalComponents)
                    so.UpdateExternalComponents();
            }
            else
            {
                followerWorker.Deform(so);
            }

            if (!so.IsMeshRendered())
                so.RenderMeshInternal(true);
        }

        public void SetSpline(Spline spline)
        {
            this.spline = spline;
            followerWorker.SetSpline(spline);

            foreach (DeformationWorker dw in deformationWorkers)
                dw.SetSpline(spline);
        }

        public bool IsWorking()
        {
            return isWorking;
        }

        public bool HasWork()
        {
            if (followerWorker.GetWorkCount() > 0)
                return true;

            foreach (DeformationWorker dw in deformationWorkers)
            {
                if (dw.GetWorkCount() > 0)
                    return true;
            }

            return false;
        }

        public int GetVerticesCount()
        {
            int count = 0;

            count += followerWorker.GetWorkCount();

            foreach (DeformationWorker dw in deformationWorkers)
                count += dw.totalVertices;

            return count;
        }

        private DeformationWorker GetDeformationWorkerFromPool()
        {
            foreach (DeformationWorker dw in deformationWorkers)
            {
                if (dw.workerState != WorkerState.FULL && dw.workerState != WorkerState.WORKING)
                {
                    return dw;
                }
            }

            DeformationWorker newDw = new DeformationWorker(this, spline);
            deformationWorkers.Add(newDw);

#if UNITY_EDITOR
            if (deformationWorkers.Count > 500)
                Debug.LogWarning($"[Spline Architect] Currently: {deformationWorkers.Count} deformation workers exists!");
#endif

            return newDw;
        }
    }
}
