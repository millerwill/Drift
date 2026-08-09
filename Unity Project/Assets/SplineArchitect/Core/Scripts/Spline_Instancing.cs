// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Spline_Instancing.cs
//
// Author: Mikael Danielsson
// Date Created: 09-06-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using SplineArchitect.Utility;

namespace SplineArchitect
{
    public partial class Spline : MonoBehaviour
    {
        private Dictionary<ulong, InstanceBatch> instanceBatches = new Dictionary<ulong, InstanceBatch>();
        internal HashSet<SplineInstance> switchBatch = new HashSet<SplineInstance>();
        public ShadowCastingMode InstancingCastShadows { get; set; } = ShadowCastingMode.On;
        public bool InstancingReceiveShadows { get; set; } = true;
        public SplineOptimizationFlags instancingOptimizationFlags { get; set; }

        public int InstanceCount
        {
            get
            {
                int count = 0;

                foreach (KeyValuePair<ulong, InstanceBatch> item in instanceBatches) 
                    count += item.Value.nativeSplineInstances.Length;

                return count;
            }
        }

        public int NoneRenderedInstanceCount
        {
            get
            {
                if (instanceBatches.TryGetValue(0, out InstanceBatch ib))
                {
                    return ib.matrices.Length;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets a dictionary of all instance batches.
        /// Do not add or remove any instance batches,
        /// as doing so may break the internal system.
        /// The key is generated from the instance IDs of the materials and the mesh.
        /// All instances that are set to not render will be placed in the batch with key 0.
        /// </summary>
        public Dictionary<ulong, InstanceBatch> GetInstanceBatchesUnsafe()
        {
            return instanceBatches;
        }

        internal void AddSplineInstance(SplineInstance si)
        {
            Mesh mesh = si.Mesh;
            ulong key = GetKey(si);

            if (!si.IsMeshRendered()) 
                key = 0;

            if (instanceBatches.ContainsKey(key))
            {
                InstanceBatch batch = instanceBatches[key];
                batch.Add(si, key);
            }
            else
            {
                InstanceBatch newBatch;
                if(key == 0) newBatch = new InstanceBatch(null, null);
                else newBatch = new InstanceBatch(mesh, si.materials);

                newBatch.Add(si, key);
                instanceBatches.Add(key, newBatch);
            }
        }

        internal void RemoveSplineInstance(SplineInstance si)
        {
            si.batch.Remove(si);
        }

        private void DrawInstances()
        {
            foreach (InstanceBatch batch in instanceBatches.Values)
            {
                // Prevents rendering instances that has rendedrMesh = false.
                if (batch.mesh == null)
                    continue;

                if (batch.matrices.Length == 0)
                    continue;

                for (int i = 0; i < batch.mesh.subMeshCount; i++) 
                {
                    RenderParams renderParams = new RenderParams(batch.materials[i]);
                    if(!GeneralUtility.IsZero(batch.Bounds.size, 0.001f))
                        renderParams.worldBounds = batch.Bounds;

                    renderParams.shadowCastingMode = InstancingCastShadows;
                    renderParams.receiveShadows = InstancingReceiveShadows;

                    Graphics.RenderMeshInstanced(renderParams, batch.mesh, i, batch.matrices.AsArray(), batch.matrices.Length);
                }
            }
        }

        private ulong GetKey(SplineInstance si)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            ulong hash = offset;

            hash ^= 2UL;
            hash *= prime;

#if UNITY_6000_4_OR_NEWER
            hash ^= EntityId.ToULong(si.Mesh.GetEntityId());
#else
            hash ^= (ulong)si.Mesh.GetInstanceID();
#endif
            hash *= prime;

            for (int i = 0; i < si.MaterialCount; i++)
            {
                Material mat = si.GetMaterialAtIndex(i);

#if UNITY_6000_4_OR_NEWER
                hash ^= EntityId.ToULong(mat.GetEntityId());
#else
                hash ^= (ulong)mat.GetInstanceID();
#endif
                hash *= prime;
            }

            return hash;
        }
    }
}
