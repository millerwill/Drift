// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: NativeSplineInstance.cs
//
// Author: Mikael Danielsson
// Date Created: 15-06-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

namespace SplineArchitect
{
    public struct NativeSplineInstance
    {
        // Transform
        private float3 splinePosition;
        private float3 scale;
        private quaternion splineRotation;
        private bool alignToEnd;
        private RotationMode rotationMode;

        internal int version;
        internal int processedVersion;

        internal int splineInstanceKey;
        internal ulong instanceBatcheKey;

        // Link
        public float closestSegmentZPosition { get; internal set; }
        public int closestSegmentIndex { get; internal set; }
        public float3 SplinePosition
        {
            get => splinePosition;
            set
            {
                version++;
                splinePosition = value;
            }
        }
        public quaternion SplineRotation
        {
            get => splineRotation;
            set
            {
                version++;
                splineRotation = value;
            }
        }
        public float3 Scale
        {
            get => scale;
            set
            {
                version++;
                scale = value;
            }
        }
        public bool AlignToEnd
        {
            get => alignToEnd;
            set
            {
                version++;
                alignToEnd = value;
            }
        }
        public RotationMode RotationMode
        {
            get => rotationMode;
            set
            {
                version++;
                rotationMode = value;
            }
        }

        public NativeSplineInstance(float3 splinePosition, float3 scale,  quaternion splineRotation, bool alignToEnd, RotationMode rotationMode)
        {
            this.splinePosition = splinePosition;
            this.scale = scale;
            this.splineRotation = splineRotation;
            this.alignToEnd = alignToEnd;
            this.rotationMode = rotationMode;

            closestSegmentZPosition = 0;
            closestSegmentIndex = -1;

            version = 0;
            processedVersion = 0;

            splineInstanceKey = 0;
            instanceBatcheKey = 0;
        }

        public bool DidCrossSegment(float3 prevSplinePosition, float3 splinePosition)
        {
            if (closestSegmentIndex < 0)
                return false;

            float previousZ = prevSplinePosition.z;
            float currentZ = splinePosition.z;
            float linkZ = closestSegmentZPosition;

            if ((linkZ - previousZ) * (linkZ - currentZ) < 0)
            {
                return true;
            }

            return false;
        }

        public SplineInstance GetSplineInstance(Spline spline)
        {
            if (spline.GetInstanceBatchesUnsafe().TryGetValue(instanceBatcheKey, out InstanceBatch ib))
            {
                if (ib.splineInstances.TryGetValue(splineInstanceKey, out SplineInstance si))
                {
                    return si;
                }
            }

            return null;
        }
    }
}
