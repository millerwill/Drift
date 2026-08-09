// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Population.cs
//
// Author: Mikael Danielsson
// Date Created: 07-02-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;

using SplineArchitect.Utility;

namespace SplineArchitect
{
    public class Population
    {
        // Data that can be changed by the user
        private float startPadding;
        private float endPadding;
        private float xOffset;
        private float yOffset;
        private float spacing;
        private int resolution;
        private bool skipTangents;
        private int maxInstances = 0;
        private Quaternion prefabRotationOffset = Quaternion.identity;
        private Bounds prefabBounds;
        private PopulationFillMode fillMode = PopulationFillMode.SNAP_LAST;

        // Read only data
        private bool updateOverTime;
        private GameObject prefab;
        private bool deform;
        private Transform parent;
        private bool worldPositionStays;

        private int version;
        private int oldVersion;
        private float splineLength;
        private int warmupCounts;
        private int warmupCount;
        private bool invalid;
        private bool hasPrefabParts;

        private List<Component> components = new List<Component>(32);

        internal List<SplineObject> activeSet;
        internal List<SplineObject> deformingSet;

        public event Action<SplineObject, PopulationSpawnSource> afterInstanceSpawned;
        public event Action<SplineObject> afterInstanceDespawned;
        public event Action afterUpdate;

        public float StartPadding
        {
            get => startPadding;
            set
            {
                if (GeneralUtility.IsEqual(value, startPadding))
                    return;

                version++;
                startPadding = value;
            }
        }
        public float EndPadding
        {
            get => endPadding;
            set
            {
                if (GeneralUtility.IsEqual(value, endPadding))
                    return;

                version++;
                endPadding = value;
            }
        }
        public float XOffset
        {
            get => xOffset;
            set
            {
                if (GeneralUtility.IsEqual(value, xOffset))
                    return;

                version++;
                xOffset = value;
            }
        }
        public float YOffset
        {
            get => yOffset;
            set
            {
                if (GeneralUtility.IsEqual(value, yOffset))
                    return;

                version++;
                yOffset = value;
            }
        }
        public float Spacing
        {
            get => spacing;
            set
            {
                if (GeneralUtility.IsEqual(value, spacing))
                    return;

                version++;
                spacing = value;
            }
        }
        public int Resolution
        {
            get => resolution;
            set
            {
                if (GeneralUtility.IsEqual(value, resolution))
                    return;

                version++;
                resolution = value;
            }
        }
        public bool SkipTangents
        {
            get => skipTangents;
            set
            {
                if (value == skipTangents)
                    return;
                version++;
                skipTangents = value;
            }
        }   
        public int MaxInstances
        {
            get => maxInstances;
            set
            {
                if (value == maxInstances)
                    return;

                version++;
                maxInstances = value;
            }
        }
        /// <summary>
        /// Determines how the population fills the spline.
        /// Either the last deformation is snapped to the end of the spline,
        /// or all deformations are scaled to fit the spline length.
        /// </summary>
        public Quaternion PrefabRotationOffset
        {
            get => prefabRotationOffset;
            set
            {
                if (GeneralUtility.IsEqual(value, prefabRotationOffset))
                    return;

                version++;
                prefabRotationOffset = value;
            }
        }
        public GameObject Prefab => prefab;
        public Bounds PrefabBounds
        {
            get => prefabBounds;
            set 
            {
                if (GeneralUtility.IsEqual(value, prefabBounds))
                    return;

                version++;
                prefabBounds = value;
            }
        }
        public PopulationFillMode FillMode
        {
            get => fillMode;
            set
            {
                if (value == fillMode)
                    return;
                version++;
                fillMode = value;
            }
        }
        public bool Deform => deform;
        public Transform Parent => parent;
        public bool UpdateOverTime => updateOverTime;
        public bool WorldPositionStays => worldPositionStays;
        internal bool Invalid => invalid;
        internal bool HasPrefabParts => hasPrefabParts;

        public Population(GameObject prefab, bool deform, bool updateOverTime = true, Transform parent = null, bool worldPositionStays = true)
        {
            Mesh mesh = null;
            components.Clear();
            bool firstBoundsSet = false;
            prefab.GetComponentsInChildren(components);

            foreach (Component c in components)
            {
                if (c == null)
                    continue;

                Mesh sharedMesh = null;

                MeshFilter mf = c as MeshFilter;
                MeshCollider mc = c as MeshCollider;
                if (mf != null && mf.sharedMesh != null)
                    sharedMesh = mf.sharedMesh;
                else if (mc != null && mc.sharedMesh != null)
                    sharedMesh = mc.sharedMesh;

                if (sharedMesh != null)
                {
                    if (!sharedMesh.isReadable && deform)
                    {
#if UNITY_EDITOR
                        Debug.LogError($"[Spline Architect] Population is invalid! The mesh used by '{prefab.name}' does not have Read/Write enabled.");
#endif
                        invalid = true;
                    }

                    float4x4 matrix = float4x4.TRS(float3.zero, quaternion.identity, prefab.transform.localScale);
                    if (c.transform != prefab.transform)
                    {
                        matrix = math.mul(matrix, float4x4.TRS(c.transform.localPosition, c.transform.localRotation, c.transform.localScale));
                        Transform p = c.transform.parent;

                        for (int i = 25; i > 0; i--)
                        {
                            if (p == null || p == prefab.transform)
                                break;

                            matrix = math.mul(matrix, float4x4.TRS(p.transform.localPosition, p.transform.localRotation, c.transform.localScale));
                            p = p.parent;
                        }
                    }

                    Bounds transformedBounds = GeneralUtility.TransformBounds(sharedMesh.bounds, matrix);

                    if (!firstBoundsSet)
                    {
                        prefabBounds = transformedBounds;
                        firstBoundsSet = true;
                    }
                    else
                    {
                        prefabBounds.Encapsulate(transformedBounds);
                    }

                    if (prefab.transform != c.transform)
                        hasPrefabParts = true;

                    mesh = sharedMesh;
                }
            }

            if (mesh == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"[Spline Architect] Population is invalid! Could not find valid mesh for {prefab.name}.");
#endif
                invalid = true;
            }

            activeSet = new List<SplineObject>();
            deformingSet = new List<SplineObject>();

            this.prefab = prefab;
            this.parent = parent;
            this.deform = deform;
            this.updateOverTime = updateOverTime;
            this.worldPositionStays = worldPositionStays;

            warmupCounts = 1;
            if (updateOverTime) warmupCounts = 2;
        }

        internal bool IsVersionDirty(float splineLength)
        {
            bool dirty = version != oldVersion;
            oldVersion = version;

            //Warmup period
            if(warmupCount < warmupCounts) dirty = true;
            warmupCount++;

            if (!GeneralUtility.IsEqual(this.splineLength, splineLength))
            {
                dirty = true;
                this.splineLength = splineLength;
            }

            return dirty;
        }

        internal void Clear()
        {
            activeSet.Clear();
            deformingSet.Clear();
        }

        internal void InvokeAfterInstanceSpawned(SplineObject splineObject, PopulationSpawnSource populationSpawnSource)
        {
            afterInstanceSpawned?.Invoke(splineObject, populationSpawnSource);
        }

        internal void InvokeAfterInstanceDespawned(SplineObject splineObject)
        {
            afterInstanceDespawned?.Invoke(splineObject);
        }

        internal void InvokeAfterUpdate()
        {
            afterUpdate?.Invoke();
        }
    }
}