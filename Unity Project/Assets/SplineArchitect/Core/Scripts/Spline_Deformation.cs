// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Spline_Deformation.cs
//
// Author: Mikael Danielsson
// Date Created: 30-11-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;

using SplineArchitect.Utility;
using SplineArchitect.Workers;
using SplineArchitect.Jobs;

namespace SplineArchitect
{
    public partial class Spline : MonoBehaviour
    {
        // General stored
        [HideInInspector] public JobType jobStartType;
        [HideInInspector] public JobType jobEndType;
        /// <summary>
        /// The number of frames between job start and job completion.
        /// </summary>
        [HideInInspector] public int jobInterval;
        /// <summary>
        /// The number of frames to wait before the first 
        /// job starts after the spline is initialized.
        /// This can be used to offset multiple splines so 
        /// their jobs do not complete on the same frame.
        /// </summary>
        [HideInInspector] public int initialJobDelay;
        [HideInInspector, SerializeField] private int frameSpreading = 1;

        // General runtime
        /// <summary>
        /// The current job state of the spline's internal deformation system.
        /// </summary>
        public JobState jobState { get; internal set; }
        /// <summary>
        /// JobUpdateMode determines how the spline decides whether 
        /// SplineObjects should be included in the update job.
        /// When set to Always Update, the normal dirty check logic is bypassed 
        /// and spline objects are processed every update cycle.
        /// This can improve performance when many spline objects are known to update 
        /// together every frame. In such cases, performance can improve by around 15%.
        /// </summary>
        public JobUpdateMode jobUpdateMode { get; set; }
        [NonSerialized] private int initialJobDelayCounter;
        [NonSerialized] internal int jobIntervalCounter;
        [NonSerialized] internal SplineObjectWorker mainSystemWorker = new SplineObjectWorker();
        [NonSerialized] internal SplineObjectWorker directSystemWorker = new SplineObjectWorker();
        [NonSerialized] internal PointWorker directPointWorker = new PointWorker();
        [NonSerialized] internal HashSet<BaseWorker> allWorkers = new HashSet<BaseWorker>();

        public int FrameSpreading
        {
            get => frameSpreading;
            set
            {
                frameSpreading = value;
                if (frameSpreading < 1) frameSpreading = 1;
            }
        }

        private void InitializeWorkers()
        {
            mainSystemWorker.SetSpline(this);
            directSystemWorker.SetSpline(this);
            directPointWorker.SetSpline(this);
        }

        private void ProcessSplineObjects(bool includeLinks = true)
        {
#if UNITY_EDITOR
            if (!ValidForPlaymode())
                return;
#endif
            ProcessJobs(false);

            if (jobState != JobState.IDLE)
                return;

            if (initialJobDelayCounter > 0)
            {
                initialJobDelayCounter--;
                return;
            }

            int splineFrame = frameCounter % frameSpreading;

            bool transformChange = monitor.TransformChange();
            bool noiseChange = monitor.NoiseChange();
            bool cacheDirty = IsCacheDirty();
            bool splineDirty = cacheDirty || noiseChange || transformChange;

            if (splineDirty)
            {
                RebuildCache();
            }

            foreach (Population population in populations)
            {
                if (splineDirty || population.IsVersionDirty(Length))
                {
#if UNITY_EDITOR
                    if (FrameSpreading > 1)
                    {
                        Debug.LogError($"[Spline Architect] Frame spreading is enabled. This does not work with the splines population system. " +
                                       $"Use the Job Interval feature instead.");
                        continue;
                    }
#endif

                    if (population.UpdateOverTime)
                        PopulateUsingPoolJobSafe(population);
                    else
                    {
                        if (jobInterval > 0 || (jobStartType == JobType.LATE_UPDATE && jobEndType == JobType.UPDATE))
                        {
#if UNITY_EDITOR
                            Debug.LogError($"[Spline Architect] Population update failed on spline '{name}'. " +
                                           $"Update Over Time (on the Population object) must be enabled for splines with jobInterval > 0.");
#endif
                            continue;
                        }

                        PopulateUsingPool(population);
                    }

                    population.InvokeAfterUpdate();
                }
            }

            if (jobUpdateMode == JobUpdateMode.ALWAYS_UPDATE)
                splineDirty = true;

            for (int i = 0; i < allSplineObjects.Count; i++)
            {
                SplineObject so = allSplineObjects[i];

                if (so == null)
                    continue;

                if (so.assignedWorkerId > 0)
                    continue;

                if ((so.spreadFrame % frameSpreading) != splineFrame)
                    continue;

                so.EnsureMeshesInitialized();

                bool versionDirty = so.IsVersionDirty();
                bool soDirty = splineDirty || versionDirty || so.Monitor.PosRotSplineSpaceChange();

                if (so.Type == SplineObjectType.DEFORMATION)
                {
                    if (so.Monitor.TransformScaleChange())
                        soDirty = true;
                }

                if (so.SoParent != null)
                {
                    if (so.Monitor.CombinedParentPosRotScaleChange())
                        soDirty = true;
                }

                if (versionDirty)
                {
                    // Type change
                    if (so.Monitor.SplineObjectTypeChange(out _))
                    {
                        if (so.Type == SplineObjectType.DEFORMATION)
                        {
                            so.SyncMeshContainers();
                            for (int i2 = 0; i2 < so.MeshContainerCount; i2++)
                            {
                                MeshContainer mc = so.GetMeshContainerAtIndex(i2);
                                Mesh instanceMesh = HandleCachedResources.FetchInstanceMesh(mc);
                                mc.SetInstanceMesh(instanceMesh);
                            }
                        }
                        else
                        {
                            for (int i2 = 0; i2 < so.MeshContainerCount; i2++)
                            {
                                MeshContainer mc = so.GetMeshContainerAtIndex(i2);
                                Mesh originMesh = mc.GetOriginMesh();
                                if (originMesh != null) mc.SetInstanceMeshToOriginMesh();
                            }
                        }

                        //If not jobStartType and jobEndType aligned during the same frame we need to deform the newly converted SplineObject now.
                        if (jobInterval > 0 || (jobStartType == JobType.LATE_UPDATE && jobEndType == JobType.UPDATE))
                        {
#if UNITY_EDITOR
                            if (!so.ValidForRuntimeDeformation())
                                continue;
#endif
                            DeformNow(so, true);
                            continue;
                        }
                    }

                    // Mirror change
                    if (so.Monitor.MirrorChange())
                    {
                        so.ReverseTrianglesOnAll();
                    }

                    // Normal change
                    if (so.Monitor.NormalChange())
                    {
                        if (so.NormalType == NormalType.DO_NOT_CALCULATE)
                        {
                            so.SetOriginNormalsOnAll();
                            so.SetOriginTrianglesOnAll();
                            so.SetOriginTangentsOnAll();
                        }
                        else if (so.NormalType == NormalType.UNITY_CALCULATED_SEAMLESS)
                            so.SetSeamlessTrianglesOnAll();
                        else
                            so.SetOriginTrianglesOnAll();
                    }
                }

#if UNITY_EDITOR
                if (soDirty && !so.ValidForRuntimeDeformation())
                    continue;
#endif

                if (soDirty)
                    mainSystemWorker.Add(so);
            }

            if (splineDirty && includeLinks)
            ProcessDeformationLinks();

            if (mainSystemWorker.HasWork())
            {
                InvokeBeforeJobs();
                jobState = JobState.RUNTIME;
                jobIntervalCounter = jobInterval;
                mainSystemWorker.Start();
            }

            frameCounter++;

            void ProcessDeformationLinks()
            {
                foreach (Segment s in segments)
                {
                    if (s.LinkCount == 0)
                        continue;

                    for (int i = 0; i < s.LinkCount; i++)
                    {
                        Segment link = s.GetLinkAtIndex(i);

                        if (link.SplineParent == null)
                            continue;

                        if (link.SplineParent == this)
                            continue;

                        //Set link position
                        Vector3 newPosition = s.GetPosition(ControlHandle.ANCHOR);
                        Vector3 dif = link.GetPosition(ControlHandle.ANCHOR) - newPosition;

                        if (GeneralUtility.IsZero(dif))
                            continue;

                        link.Translate(ControlHandle.ANCHOR, dif);
                        link.Translate(ControlHandle.TANGENT_A, dif);
                        link.Translate(ControlHandle.TANGENT_B, dif);

                        link.SplineParent.MarkCacheDirty();
                        link.SplineParent.ProcessSplineObjects(false);
                        link.SplineParent.ProcessJobs(true);
                    }
                }
            }
        }

        private void ProcessSplineInstances()
        {
#if UNITY_EDITOR
            if (!ValidForPlaymode(true))
                return;
#endif

            foreach (SplineInstance si in switchBatch)
            {
                RemoveSplineInstance(si);
                AddSplineInstance(si);
            }
            switchBatch.Clear();

            foreach (KeyValuePair<ulong, InstanceBatch> item in instanceBatches)
            {
                InstanceBatch batch = item.Value;

                InstanceJob instanceJob = new InstanceJob()
                {
                    nativeSplineInstance = batch.nativeSplineInstances.AsArray(),
                    matrices = batch.matrices.AsArray(),
                    splineLength = Length,
                    splineResolution = GetSplineResolution(),
                    splineFrame = frameCounter % frameSpreading,
                    frameSpreading = frameSpreading,
                    isSplineLooping = Loop,
                    splinMatrix = transform.localToWorldMatrix,
                    distanceMap = DistanceMap,
                    positionMap = PositionMapLocal,
                    normals = NormalsLocal,
                    normalsMap = NormalsMapLocal,
                    nativeSegments = NativeSegmentsLocal,
                    scaleCurveSegments = ScaleProfileSegments,
                    noises = NativeNoises,
                    optimizationFlags = instancingOptimizationFlags
                };

                JobHandle jobHandle = instanceJob.Schedule(batch.Count, 32);
                jobHandle.Complete();
            }
        }

        private void ProcessJobs(bool updateCounter)
        {
            if (jobState != JobState.RUNTIME)
                return;

            if (jobIntervalCounter <= 0)
            {
                mainSystemWorker.Complete();
                jobState = JobState.IDLE;
                InvokeAfterJobs();
            }
            else if(updateCounter)
            {
                jobIntervalCounter--;
            }
        }

        internal void RemoveFromActiveWorkers(SplineObject so)
        {
            so.assignedWorkerId = 0;
        }

        /// <summary>
        /// Gets the total number of vertices currently 
        /// processed by the splines internal system workers.
        /// </summary>
        public int GetVerticesCountInWorkers()
        {
            return mainSystemWorker.GetVerticesCount();
        }

        /// <summary>
        /// Immediately updates the spline object's position 
        /// and deforms it if it is a deformation object.
        /// </summary>
        public void DeformNow(SplineObject so, bool updateExternalComponents = false)
        {
            directSystemWorker.Deform(so, updateExternalComponents);
        }

        /// <summary>
        /// Rebuilds all cached data on the spline and updates all SplineObjects attached to it.
        /// If you are using jobInterval > 0 and have added Populations to the spline,
        /// you can call this function to force the spline to update all populations immediately.
        /// </summary>
        public void DeformSplineObjectsNow()
        {
            CompleteOngoingWorkers();
            MarkCacheDirty();
            finalizePopulateUsingPoolJobSafe = true;
            ProcessSplineObjects();
            finalizePopulateUsingPoolJobSafe = false;
            CompleteOngoingWorkers();
        }

        private int CompleteOngoingWorkers()
        {
            int count = 0;

            if (mainSystemWorker.IsWorking())
            {
                mainSystemWorker.Complete();
                count++;
            }

            foreach (BaseWorker bw in allWorkers)
            {
                if (bw.workerState == WorkerState.WORKING)
                {
                    bw.Complete();
                    count++;
                }
            }

            jobState = JobState.IDLE;
            jobIntervalCounter = 0;
            initialJobDelayCounter = 0;

            return count;
        }
    }
}
