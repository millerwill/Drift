// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineInstance.cs
//
// Author: Mikael Danielsson
// Date Created: 12-06-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;

using SplineArchitect.Utility;

namespace SplineArchitect
{
    public partial class SplineInstance
    {
        // General
        internal Spline splineParent;

        // Mesh
        private Mesh mesh;
        internal List<Material> materials = new List<Material>();
        internal int batchIndex = -1;
        internal InstanceBatch batch = null;
        private bool renderMesh = true;

        internal NativeSplineInstance cachedNsi;
        internal Matrix4x4 cachedMatrix;

        public Spline SplineParent => splineParent;
        public NativeSplineInstance NativeSplineInstance
        {
            get
            {
                if (splineParent != null)
                {
                    return batch.nativeSplineInstances[batchIndex];
                }

                return cachedNsi;
            }
            set 
            {
                if (splineParent == null)
                {
                    cachedNsi = value;
                    return;
                }

                value.version++;
                batch.nativeSplineInstances[batchIndex] = value;
            }
        }
        public float3 SplinePosition
        {
            get
            {
                if (splineParent != null)
                {
                    return batch.nativeSplineInstances[batchIndex].SplinePosition;
                }

                return cachedNsi.SplinePosition;
            }
            set
            {
                if (splineParent == null)
                {
                    cachedNsi.SplinePosition = value;
                    return;
                }

                NativeSplineInstance nsi = batch.nativeSplineInstances[batchIndex];
                nsi.SplinePosition = value;
                batch.nativeSplineInstances[batchIndex] = nsi;
            }
        }
        public float3 Scale
        {
            get
            {
                if (splineParent != null)
                {
                    return batch.nativeSplineInstances[batchIndex].Scale;
                }

                return cachedNsi.Scale;
            }
            set
            {
                if (splineParent == null)
                {
                    cachedNsi.Scale = value;
                    return;
                }

                NativeSplineInstance nsi = batch.nativeSplineInstances[batchIndex];
                nsi.Scale = value;
                batch.nativeSplineInstances[batchIndex] = nsi;
            }
        }
        public quaternion SplineRotation
        {
            get
            {
                if (splineParent != null)
                {
                    return batch.nativeSplineInstances[batchIndex].SplineRotation;
                }

                return cachedNsi.SplineRotation;
            }
            set
            {
                if (splineParent == null)
                {
                    cachedNsi.SplineRotation = value;
                    return;
                }

                NativeSplineInstance nsi = batch.nativeSplineInstances[batchIndex];
                nsi.SplineRotation = value;
                batch.nativeSplineInstances[batchIndex] = nsi;
            }
        }
        public RotationMode RotationMode
        {
            get
            {
                if (splineParent != null)
                {
                    return batch.nativeSplineInstances[batchIndex].RotationMode;
                }

                return cachedNsi.RotationMode;
            }
            set
            {
                if (splineParent == null)
                {
                    cachedNsi.RotationMode = value;
                    return;
                }

                NativeSplineInstance nsi = batch.nativeSplineInstances[batchIndex];
                nsi.RotationMode = value;
                batch.nativeSplineInstances[batchIndex] = nsi;
            }
        }
        public bool AlignToEnd
        {
            get
            {
                if (splineParent != null)
                {
                    return batch.nativeSplineInstances[batchIndex].AlignToEnd;
                }

                return cachedNsi.AlignToEnd;
            }
            set
            {
                if (splineParent == null)
                {
                    cachedNsi.AlignToEnd = value;
                    return;
                }

                NativeSplineInstance nsi = batch.nativeSplineInstances[batchIndex];
                nsi.AlignToEnd = value;
                batch.nativeSplineInstances[batchIndex] = nsi;
            }
        }

        public SplineInstance(Mesh mesh, List<Material> materials) 
        {
            cachedNsi = new NativeSplineInstance(float3.zero, new float3(1, 1, 1), quaternion.identity, false, RotationMode.FOLLOW_SPLINE);
            cachedMatrix = Matrix4x4.identity;
            SetMeshAndMaterials(mesh, materials);
        }

        public SplineInstance(Mesh mesh, Material material)
        {
            cachedNsi = new NativeSplineInstance(float3.zero, new float3(1, 1, 1), quaternion.identity, false, RotationMode.FOLLOW_SPLINE);
            cachedMatrix = Matrix4x4.identity;
            SetMeshAndMaterial(mesh, material);
        }

        public int MaterialCount
        {
            get
            {
                if (materials == null)
                    return 0;

                return materials.Count;
            }
        }

        public Mesh Mesh
        {
            get => mesh;
            set
            {
                if (mesh == value)
                    return;

                if (value.subMeshCount != materials.Count)
                {
                    Debug.LogError("[Spline Architect] Could not set mesh! Needs to have the same sub mesh count as material count! Use splineInstance.SetMeshAndMaterials() instead.");
                    return;
                }

                mesh = value;

                if (splineParent != null)
                    splineParent.switchBatch.Add(this);
            }
        }

        public Material GetMaterialAtIndex(int index)
        {
            return materials[index];
        }

        public void SetMaterialAtIndex(int index, Material material)
        {
            materials[index] = material;

            if (splineParent != null)
            {
                splineParent.switchBatch.Add(this);
            }
        }

        public void SetMeshAndMaterials(Mesh mesh, List<Material> materials)
        {
            if (mesh.subMeshCount != materials.Count)
            {
                Debug.LogError("[Spline Architect] Sub mesh count is not the same as total materials!");
                return;
            }

            this.mesh = mesh;
            this.materials.Clear();
            this.materials.AddRange(materials);

            if (splineParent != null)
                splineParent.switchBatch.Add(this);
        }

        public void SetMeshAndMaterial(Mesh mesh, Material material)
        {
            if (mesh.subMeshCount != 1)
            {
                Debug.LogError("[Spline Architect] Sub mesh count is not the same as total materials!");
                return;
            }

            this.mesh = mesh;
            this.materials.Clear();
            this.materials.Add(material);

            if (splineParent != null)
                splineParent.switchBatch.Add(this);
        }

        public bool TryFindLinkCrossings(List<Segment> links, Vector3 splinePosition,
                                                              Vector3 previousSplinePosition,
                                                              out float zDif)
        {
            zDif = 0;

            NativeSplineInstance nsi = batch.nativeSplineInstances[batchIndex];

            if (nsi.closestSegmentIndex < 0)
                return false;

            float previousZ = previousSplinePosition.z;
            float currentZ = splinePosition.z;
            float linkZ = nsi.closestSegmentZPosition;

            if ((linkZ - previousZ) * (linkZ - currentZ) < 0)
            {
                Segment closest = splineParent.GetSegmentAtIndex(nsi.closestSegmentIndex);

                if (closest.LinkCount > 0)
                {
                    links.AddRange(closest.links);
                    return true;
                }
            }

            return false;
        }

        public void RenderMesh(bool value)
        {
            if (splineParent == null || batch == null)
                return;

            if (renderMesh == value)
                return;

            renderMesh = value;

            if (!splineParent.switchBatch.Contains(this))
                splineParent.switchBatch.Add(this);
            else
                splineParent.switchBatch.Remove(this);
        }

        public bool IsMeshRendered()
        {
            return renderMesh;
        }

        public void SetParent(Spline newParent)
        {
            if (splineParent == newParent)
                return;

            if (newParent == null)
            {
                cachedNsi = NativeSplineInstance;
            }

            NativeSplineInstance oldNsi = NativeSplineInstance;

            if(splineParent != null)
                splineParent.RemoveSplineInstance(this);

            splineParent = newParent;

            if (newParent != null)
            {
                newParent.AddSplineInstance(this);

                NativeSplineInstance newNsi = NativeSplineInstance;
                oldNsi.splineInstanceKey = newNsi.splineInstanceKey;
                oldNsi.instanceBatcheKey = newNsi.instanceBatcheKey;
                NativeSplineInstance = oldNsi;
            }
        }
    }
}
