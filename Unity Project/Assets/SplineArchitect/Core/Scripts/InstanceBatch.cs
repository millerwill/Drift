// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: InstanceBatch.cs
//
// Author: Mikael Danielsson
// Date Created: 09-06-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using Unity.Collections;
using UnityEngine;

namespace SplineArchitect
{
    public class InstanceBatch
    {
        internal Mesh mesh;
        internal List<Material> materials;

        internal Dictionary<int, SplineInstance> splineInstances;
        internal NativeList<Matrix4x4> matrices;
        internal NativeList<NativeSplineInstance> nativeSplineInstances;
        private int nextHandlerKey;
        private Bounds bounds;

        public Bounds Bounds
        {
            get => bounds;
            set 
            {
                bounds = value;
            }
        }
        public NativeList<NativeSplineInstance> NativeSplineInstances => nativeSplineInstances;

        public int Count => matrices.Length;

        internal InstanceBatch(Mesh mesh, List<Material> materials)
        {
            this.materials = new List<Material>();
            this.matrices = new NativeList<Matrix4x4>(Allocator.Persistent);
            this.nativeSplineInstances = new NativeList<NativeSplineInstance>(Allocator.Persistent);

            this.mesh = mesh;

            if(materials != null)
                this.materials.AddRange(materials);

            this.splineInstances = new Dictionary<int, SplineInstance>();
        }

        internal void Add(SplineInstance si, ulong key)
        {
            // Set values and add
            splineInstances.Add(nextHandlerKey, si);

            // Create native data
            NativeSplineInstance nsi = si.cachedNsi;
            nsi.splineInstanceKey = nextHandlerKey;
            nsi.instanceBatcheKey = key;
            si.cachedNsi = nsi;

            si.batchIndex = matrices.Length;
            si.batch = this;

            nativeSplineInstances.Add(nsi);
            matrices.Add(si.cachedMatrix);

            // Update handler key for next add
            nextHandlerKey++;
        }

        internal void Remove(SplineInstance si)
        {
            int index = si.batchIndex;
            int lastIndex = matrices.Length - 1;

            // Remove handler
            NativeSplineInstance nsi = nativeSplineInstances[index];
            si.cachedNsi = nsi;
            si.cachedMatrix = matrices[index];
            splineInstances.Remove(nsi.splineInstanceKey);

            // If not last, swap last with the instance thats being removed.
            if (index != lastIndex)
            {
                NativeSplineInstance lastNsi = nativeSplineInstances[lastIndex];

                if (splineInstances.TryGetValue(lastNsi.splineInstanceKey, out SplineInstance lastSi))
                    lastSi.batchIndex = index;
                else
                {
                    Debug.Log("[Spline Architect] Could not remove Spline Instance!");
                    return;
                }
            }

            // Swap with last and remove native data.
            matrices.RemoveAtSwapBack(index);
            nativeSplineInstances.RemoveAtSwapBack(index);

            si.batch = null;
            si.batchIndex = -1;
        }

        internal void Dispose() 
        {
            matrices.Dispose();
            nativeSplineInstances.Dispose();
        }
    }
}
