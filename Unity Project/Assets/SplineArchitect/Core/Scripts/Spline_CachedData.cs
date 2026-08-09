// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Spline_CachedData.cs
//
// Author: Mikael Danielsson
// Date Created: 25-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

using SplineArchitect.Utility;
using SplineArchitect.Jobs;
using SplineArchitect.Workers;

namespace SplineArchitect
{
    public partial class Spline : MonoBehaviour
    {
        // Public, runtime
        /// <summary>
        /// Indicates whether the spline has an invalid shape. 
        /// A spline is considered invalid if it has only one control point,
        /// or if the first anchor and its Tangent B share the same position,
        /// or if the last anchor and its Tangent A share the same position.
        /// An invalid spline cannot be deformed, as this would otherwise 
        /// cause visual artifacts when deforming meshes along the spline.
        /// </summary>
        public bool isInvalidShape { get; private set; }
        public Bounds controlPointsBounds { get; private set; }
        internal Bounds bounds { get; private set; }
        private NativeList<float> distanceMap;
        private NativeList<float3> normalsLocal;
        private NativeList<int> normalsMapLocal;
        private NativeList<float3> positionMapLocal;
        private NativeList<float3> positionMap;
        private NativeArray<NativeSegment> nativeSegmentsLocal;
        private NativeArray<NoiseLayer> nativeNoises;
        private NativeArray<HermiteSegment> scaleProfileSegments;
        private NativeList<Vector3> nativeLinePoints;

        public NativeList<float> DistanceMap => distanceMap;
        public NativeList<int> NormalsMapLocal => normalsMapLocal;
        public NativeList<float3> NormalsLocal => normalsLocal;
        public NativeList<float3> PositionMapLocal => positionMapLocal;
        public NativeArray<NativeSegment> NativeSegmentsLocal => nativeSegmentsLocal;
        public NativeArray<NoiseLayer> NativeNoises => nativeNoises;
        public NativeArray<HermiteSegment> ScaleProfileSegments => scaleProfileSegments;
        internal NativeList<float3> PositionMap => positionMap;

        // Private, stored
        [HideInInspector, SerializeField] private float resolutionSplineData = 500;
        [HideInInspector, SerializeField] private float samplingStepsSplineData = 10;
        [HideInInspector, SerializeField] private float resolutionNormal = 1000;

        // Private, runtime
        [NonSerialized] private bool useCachedPositions = true;
        [NonSerialized] private int oldNoisesCount;
        [NonSerialized] private int oldScaleProfileSegmentsCount;
        [NonSerialized] private int oldSegmentsLocalCount;
        [NonSerialized] private int cacheVersion;
        [NonSerialized] private int oldCacheVersion;

        // Properties
        public bool UseCachedPositions
        {
            get => useCachedPositions;
            set
            {
                if (value == useCachedPositions)
                    return;

                useCachedPositions = value;
                MarkCacheDirty();
            }
        }

        internal void DisposeCache()
        {
            foreach (BaseWorker bw in allWorkers)
                bw.CompleteWithoutAssignData();

            foreach (BaseWorker bw in allWorkers)
                bw.DisposeNativeData();

            foreach (KeyValuePair<ulong, InstanceBatch> item in instanceBatches)
                item.Value.Dispose();

            if (distanceMap.IsCreated)
                distanceMap.Dispose();

            if (normalsLocal.IsCreated)
                normalsLocal.Dispose();

            if(normalsMapLocal.IsCreated)
                normalsMapLocal.Dispose();

            if (positionMapLocal.IsCreated)
                positionMapLocal.Dispose();

#if UNITY_EDITOR
            if (positionMap.IsCreated)
                positionMap.Dispose();
#endif

            if (nativeSegmentsLocal.IsCreated)
                nativeSegmentsLocal.Dispose();

            if (nativeNoises.IsCreated)
                nativeNoises.Dispose();

            if (scaleProfileSegments.IsCreated)
                scaleProfileSegments.Dispose();

            if (nativeLinePoints.IsCreated)
                nativeLinePoints.Dispose();
        }

        /// <summary>
        /// Rebuilds and updates all cached and native data 
        /// used by the spline for deformations, bounds,
        /// and runtime evaluation. Call this if you need
        /// the spline to be updated with new data
        /// before performing custom logic that depends on the latest spline state.
        /// </summary>
        public void RebuildCache()
        {
            oldCacheVersion = cacheVersion;
#if UNITY_EDITOR
            undoState.oldCacheVersion = undoState.cacheVersion;
            undoState.oldEditorCacheVersion = undoState.editorCacheVersion;
#endif

            int completedWorkers = CompleteOngoingWorkers();
#if UNITY_EDITOR
            if (completedWorkers > 0)
            {
                Debug.LogWarning($"[SplineArchitect] Force completed {completedWorkers} ongoing worker(s) while rebuilding spline cache. " +
                                 $"This can be expected and is valid, but if it occurs frequently within a frame it may impact performance.");
            }
#endif

            InvokeBeforeRebuildCache();

            PrepareForCaching();
            ValidateShape();
            CacheSegmentsLocal();
            CacheNoises();
            CacheScaleCurveSegments();

            if (!distanceMap.IsCreated) distanceMap = new NativeList<float>(0, Allocator.Persistent);
            if (!normalsLocal.IsCreated) normalsLocal = new NativeList<float3>(0, Allocator.Persistent);
            if (!normalsMapLocal.IsCreated) normalsMapLocal = new NativeList<int>(0, Allocator.Persistent);
            if (!positionMapLocal.IsCreated) positionMapLocal = new NativeList<float3>(0, Allocator.Persistent);

            if (segments.Count > 1)
            {
                CalculateSplineData();
                CalculateCachedNormals();
                RebuildSplineLine(true);

#if UNITY_EDITOR
                if (!positionMap.IsCreated) positionMap = new NativeList<float3>(0, Allocator.Persistent);

                CalculatePositionMap(Space.World);
                CalculateSplineBounds();
#endif
            }

            if (segments.Count > 0) CalculateControlpointBounds();

            InvokeAfterRebuildCache();

            #region Functions
            void CacheSegmentsLocal()
            {
                if (!nativeSegmentsLocal.IsCreated || oldSegmentsLocalCount != segments.Count)
                {
                    oldSegmentsLocalCount = segments.Count;
                    if(nativeSegmentsLocal.IsCreated) nativeSegmentsLocal.Dispose();
                    nativeSegmentsLocal = SplineUtilityNative.CreateNativeArray(segments, Space.Self, Allocator.Persistent);
                }
                else
                {
                    SplineUtilityNative.CopyToNativeArray(segments, nativeSegmentsLocal, Space.Self);
                }
            }

            void CacheNoises()
            {
                if (!nativeNoises.IsCreated || oldNoisesCount != noises.Count)
                {
                    oldNoisesCount = noises.Count;
                    if (nativeNoises.IsCreated) nativeNoises.Dispose();
                    nativeNoises = new NativeArray<NoiseLayer>(noises.Count, Allocator.Persistent);
                }

                for (int i = 0; i < noises.Count; i++)
                {
                    if (noises[i].enabled && noises[i].group == noiseGroup)
                        nativeNoises[i] = noises[i];
                    else
                        nativeNoises[i] = new NoiseLayer();
                }
            }

            void CacheScaleCurveSegments()
            {
                int segmentCount = scaleProfileCurve == null ? 0 : Mathf.Max(0, scaleProfileCurve.length - 1);

                if (!scaleProfileSegments.IsCreated || oldScaleProfileSegmentsCount != segmentCount)
                {
                    oldScaleProfileSegmentsCount = segmentCount;

                    if (scaleProfileSegments.IsCreated)
                        scaleProfileSegments.Dispose();

                    scaleProfileSegments = new NativeArray<HermiteSegment>(segmentCount, Allocator.Persistent);
                }

                if (scaleProfileCurve == null || segmentCount == 0)
                    return;

                for (int i = 0; i < segmentCount; i++)
                {
                    Keyframe k0 = scaleProfileCurve[i];
                    Keyframe k1 = scaleProfileCurve[i + 1];

                    HermiteSegment hs = new HermiteSegment()
                    {
                        timeStart = k0.time,
                        valueStart = k0.value,
                        tangentStart = k0.outTangent,

                        timeEnd = k1.time,
                        valueEnd = k1.value,
                        tangentEnd = k1.inTangent
                    };

                    scaleProfileSegments[i] = hs;
                }
            }

            void ValidateShape()
            {
                isInvalidShape = false;
                if (segments.Count <= 1) isInvalidShape = true;
                else
                {
                    Vector3 anchor = segments[0].GetPosition(ControlHandle.ANCHOR, Space.Self);
                    Vector3 tangentB = segments[0].GetPosition(ControlHandle.TANGENT_B, Space.Self);

                    if (GeneralUtility.IsEqual(anchor, tangentB))
                        isInvalidShape = true;

                    anchor = segments[segments.Count - 1].GetPosition(ControlHandle.ANCHOR, Space.Self);
                    Vector3 tangentA = segments[segments.Count - 1].GetPosition(ControlHandle.TANGENT_A, Space.Self);

                    if (GeneralUtility.IsEqual(anchor, tangentA))
                        isInvalidShape = true;
                }

            }

            void PrepareForCaching()
            {
                for (int i = 0; i < segments.Count; i++)
                {
                    Segment s = segments[i];
                    s.indexInSpline = i;
                    s.splineParent = this;
                    s.localSpace = transform;

                    if(segments.Count > 1)
                        s.UpdateLineOrientation();
                }
            }
            #endregion
        }

        /// <summary>
        /// Marks the spline as dirty, causing the cache to be rebuilt 
        /// and all spline objects to update on the next job cycle.
        /// In most cases this is not needed, as the system automatically
        /// detects changes to the spline and its spline objects.
        /// </summary>
        public void MarkCacheDirty()
        {
            cacheVersion++;
#if UNITY_EDITOR
            undoState.cacheVersion++;
#endif
        }

        internal bool IsCacheDirty()
        {
            bool value = oldCacheVersion != cacheVersion;

#if UNITY_EDITOR
            if (!value)
            {
                value = undoState.oldCacheVersion != undoState.cacheVersion;
            }
#endif
            return value;
        }

        internal void CalculateControlpointBounds()
        {
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            foreach (Segment s in segments)
            {
                min.x = Mathf.Min(min.x, s.GetPosition(ControlHandle.ANCHOR).x);
                min.x = Mathf.Min(min.x, s.GetPosition(ControlHandle.TANGENT_A).x);
                min.x = Mathf.Min(min.x, s.GetPosition(ControlHandle.TANGENT_B).x);

                min.y = Mathf.Min(min.y, s.GetPosition(ControlHandle.ANCHOR).y);
                min.y = Mathf.Min(min.y, s.GetPosition(ControlHandle.TANGENT_A).y);
                min.y = Mathf.Min(min.y, s.GetPosition(ControlHandle.TANGENT_B).y);

                min.z = Mathf.Min(min.z, s.GetPosition(ControlHandle.ANCHOR).z);
                min.z = Mathf.Min(min.z, s.GetPosition(ControlHandle.TANGENT_A).z);
                min.z = Mathf.Min(min.z, s.GetPosition(ControlHandle.TANGENT_B).z);

                max.x = Mathf.Max(max.x, s.GetPosition(ControlHandle.ANCHOR).x);
                max.x = Mathf.Max(max.x, s.GetPosition(ControlHandle.TANGENT_A).x);
                max.x = Mathf.Max(max.x, s.GetPosition(ControlHandle.TANGENT_B).x);

                max.y = Mathf.Max(max.y, s.GetPosition(ControlHandle.ANCHOR).y);
                max.y = Mathf.Max(max.y, s.GetPosition(ControlHandle.TANGENT_A).y);
                max.y = Mathf.Max(max.y, s.GetPosition(ControlHandle.TANGENT_B).y);

                max.z = Mathf.Max(max.z, s.GetPosition(ControlHandle.ANCHOR).z);
                max.z = Mathf.Max(max.z, s.GetPosition(ControlHandle.TANGENT_A).z);
                max.z = Mathf.Max(max.z, s.GetPosition(ControlHandle.TANGENT_B).z);
            }

            Vector3 offset = new Vector3(2, 2, 2);
            var b = new Bounds();
            b.SetMinMax(min - offset, max + offset);
            controlPointsBounds = b;
        }

        internal void CalculateSplineBounds()
        {
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            float precision = 1 / (25 * (length / 100));

            for (float time = 0; time < 1; time += precision)
            {
                UpdateMinMax(time);
            }

            //Make sure to get end.
            UpdateMinMax(1);

            Vector3 offset = new Vector3(2, 2, 2);
            Bounds b = new Bounds();
            b.SetMinMax(min - offset, max + offset);
            bounds = b;

            void UpdateMinMax(float time)
            {
                Vector3 position = GetPosition(time);
                min.x = Mathf.Min(min.x, position.x);
                min.y = Mathf.Min(min.y, position.y);
                min.z = Mathf.Min(min.z, position.z);

                max.x = Mathf.Max(max.x, position.x);
                max.y = Mathf.Max(max.y, position.y);
                max.z = Mathf.Max(max.z, position.z);
            }
        }

        internal void CalculatePositionMap(Space space)
        {
            PositionMapJob positionMapJob = new PositionMapJob()
            {
                positionMap = space == Space.Self ? positionMapLocal : positionMap,
                nativeSegments = space == Space.Self ? nativeSegmentsLocal : SplineUtilityNative.CreateNativeArray(segments, Space.World, Allocator.TempJob),
                resolution = GetSplineResolution()
            };

            JobHandle jobHandle = positionMapJob.Schedule();
            jobHandle.Complete();

            if (space == Space.World)
                positionMapJob.nativeSegments.Dispose();
        }

        internal void CalculateSplineData()
        {
            SplineDataJob splineDataJob = new SplineDataJob()
            {
                distanceMap = distanceMap,
                positionMapLocal = positionMapLocal,
                splineLength = new NativeArray<float>(1, Allocator.TempJob),
                segmentZPositions = new NativeArray<float>(segments.Count, Allocator.TempJob),
                segmentLengths = new NativeArray<float>(segments.Count, Allocator.TempJob),
                nativeSegments = nativeSegmentsLocal,
                resolution = GetSplineResolution(),
                samplingStep = GetSamplingStepDistanceMap(),
                calculateLocalPositions = useCachedPositions
            };

            JobHandle jobHandle = splineDataJob.Schedule();
            jobHandle.Complete();

            for (int i = 0; i < segments.Count; i++)
            {
                Segment s = segments[i];
                s.zPosition = splineDataJob.segmentZPositions[i];
                s.length = splineDataJob.segmentLengths[i];

                NativeSegment ns = nativeSegmentsLocal[i];
                ns.length = splineDataJob.segmentLengths[i];
                nativeSegmentsLocal[i] = ns;
            }

            length = splineDataJob.splineLength[0];
            splineDataJob.splineLength.Dispose();
            splineDataJob.segmentZPositions.Dispose();
            splineDataJob.segmentLengths.Dispose();

            if(!useCachedPositions)
            {
                positionMapLocal.Clear();
                positionMapLocal.TrimExcess();
            }
        }

        internal void CalculateCachedNormals()
        {
            if (splineType != SplineType.DYNAMIC)
            {
                NormalsJob normalsJob = new NormalsJob()
                {
                    normals = normalsLocal,
                    normalMap = normalsMapLocal,
                    nativeSegments = nativeSegmentsLocal,
                    resolution = GetNormalResolution()
                };

                JobHandle jobHandle = normalsJob.Schedule();
                jobHandle.Complete();
            }
            else
            {
                LegacyNormalsJob legacyNormalsJob = new LegacyNormalsJob()
                {
                    normals = normalsLocal,
                    normalMap = normalsMapLocal,
                    nativeSegments = nativeSegmentsLocal,
                    resolution = GetNormalResolution()
                };

                JobHandle jobHandle = legacyNormalsJob.Schedule();
                jobHandle.Complete();
            }
        }

        public float GetSplineResolution(bool rawValue = false)
        {
            if (rawValue) return resolutionSplineData;

            float value = 1 / (resolutionSplineData - 1);
            return Mathf.Clamp(value, 0.00001f, 0.1f);
        }

        public float GetNormalResolution()
        {
            return resolutionNormal;
        }

        public float GetSamplingStepDistanceMap(bool rawValue = false)
        {
            if (rawValue) return samplingStepsSplineData;

            return GetSplineResolution() / samplingStepsSplineData;
        }

        public void SetSplineResolution(float value)
        {
            if (GeneralUtility.IsEqual(value, resolutionSplineData))
                return;

            resolutionSplineData = value;
            MarkCacheDirty();
        }

        public void SetResolutionNormal(float value)
        {
            if (GeneralUtility.IsEqual(value, resolutionNormal))
                return;

            resolutionNormal = value;
            MarkCacheDirty();
        }

        public void SetSamplingStepDistanceMap(float value)
        {
            if (GeneralUtility.IsEqual(value, samplingStepsSplineData))
                return;

            samplingStepsSplineData = Mathf.Clamp(value, 3, 1000);
            MarkCacheDirty();
        }
    }
}
