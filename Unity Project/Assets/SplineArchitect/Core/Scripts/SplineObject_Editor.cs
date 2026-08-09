// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObject_Editor.cs
//
// Author: Mikael Danielsson
// Date Created: 28-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect
{
    public partial class SplineObject : MonoBehaviour
    {
#if UNITY_EDITOR

        // =====================
        // Editor-only (ignored at runtime)
        // =====================

        // General stored data
        [HideInInspector] public bool editorAutoType;

        // General runtime data
        [NonSerialized] internal Vector3 activationPosition;
        [NonSerialized] public bool editorDisableOnChildrenChanged;
        [NonSerialized] private bool hasFirstUpdateRun;
        [NonSerialized] private bool readWriteWarningTriggered;
        [NonSerialized] private bool componentModeWarningTriggered;
        [NonSerialized] private List<Mesh> meshCountContainer = new List<Mesh>();
        [NonSerialized] internal ScriptableObjects.SplineObjectUndoState undoState;
        internal int deformedVertecies { get; private set; }
        internal int deformations { get; private set; }

        private void Update()
        {
            hasFirstUpdateRun = true;
        }

        private void OnTransformChildrenChanged()
        {
            if (editorDisableOnChildrenChanged)
                return;

            if (this == null || transform == null)
                return;

            if (splineParent == null)
                return;

            monitor.EditorChildCountChange(out int dif);

            if (type != SplineObjectType.DEFORMATION)
                return;

            if (dif > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);

                    //Should not parent splines to deformations. However you can do that to followers.
                    if(type == SplineObjectType.DEFORMATION)
                    {
                        Spline childSpline = child.GetComponent<Spline>();

                        if(childSpline != null)
                        {
                            Debug.LogWarning($"[Spline Architect] Can't parent spline to SplineObject with type Deformation.");
                            child.parent = null;
                            continue;
                        }
                    }

                    Attach(child, this, false);
                }

                bool Attach(Transform childTransform, SplineObject soParent, bool skipTransforming)
                {
                    //Check if its parented to a follower, if so abort.
                    if (childTransform.parent != null)
                    {
                        SplineObject p = childTransform.parent.GetComponent<SplineObject>();

                        if (p != null && p.type != SplineObjectType.DEFORMATION)
                            return false;
                    }

                    bool attached = ESplineObjectUtility.TryAttacheOnTransformEditor(splineParent, childTransform, skipTransforming);
                    SplineObject childSo = childTransform.GetComponent<SplineObject>();

                    if (childSo == null)
                        return attached;

                    for (int i = 0; i < childTransform.childCount; i++)
                    {
                        Transform childTransform2 = childTransform.GetChild(i);

                        if (childTransform2 == childTransform)
                            continue;

                        bool attachedChild = Attach(childTransform2, childSo, true);

                        if (!Application.isPlaying && attached != attachedChild)
                            Debug.LogWarning("[Spline Architect] All GameObjects parented to a spline or deformation must either have a SplineObject component, or none at all. " +
                                             "Failing to do so may result in incorrect positions but will otherwise work fine.");
                    }

                    return attached;
                }
            }
        }

        internal void InitalizeEditor()
        {
            undoState.oldVersion = undoState.version;

            //Validate if mesh can be generated
            if (Application.isPlaying && type == SplineObjectType.DEFORMATION && meshMode == MeshMode.GENERATE)
            {
                if(componentMode == ComponentMode.REMOVE_FROM_BUILD)
                {
                    Debug.LogError($"[Spline Architect] Can't generate mesh on {name} becouse the component setting is set to 'Remove from build'.");
                    DestroyAllInstanceMeshes();
                    Destroy(this);
                    return;
                }
                else if (splineParent.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                {
                    Debug.LogError($"[Spline Architect] Can't generate mesh on {name} becouse the component setting on {splineParent.name} is set to 'Remove from build'.");
                    DestroyAllInstanceMeshes();
                    Destroy(this);
                    return;
                }
                else
                {
                    foreach (MeshContainer mc in meshContainers)
                    {
                        Mesh instanceMesh = mc.GetInstanceMesh();

                        if (instanceMesh != null && !instanceMesh.isReadable)
                        {
                            Debug.LogError($"[Spline Architect] Can't generate mesh during runtime becouse read/write access is disabled on '{name}' ({splineParent.name}).");
                            DestroyAllInstanceMeshes();
                            Destroy(this);
                            return;
                        }
                    }
                }
            }

            //Deform mesh during build and store it in the built application.
            if (type == SplineObjectType.DEFORMATION && meshMode == MeshMode.SAVE_IN_BUILD && EHandleEvents.buildRunning && MeshContainerCount > 0)
            {
                splineParent.Initalize();
                splineParent.directSystemWorker.Deform(this, true);
            }

            activationPosition = localSplinePosition;

            if(type == SplineObjectType.DEFORMATION)
            {
                //Initalizes meschContainers.
                foreach (MeshContainer mc in meshContainers)
                {
                    //Gets the correct time stamp. Is used for detecting asset modifications.
                    mc.TryUpdateTimestamp();
                    //mc.UpdateKeys();
                }
            }

            //If part of hierarchy this data needs to be the same for all spline objects in the hierarchy.
            if (soParent != null)
            {
                alignToEnd = soParent.AlignToEnd;
                componentMode = soParent.componentMode;
            }
        }

        internal bool ValidForRuntimeDeformation()
        {
            bool xZero = GeneralUtility.IsZero(transform.lossyScale.x);
            bool yZero = GeneralUtility.IsZero(transform.lossyScale.y);
            bool zZero = GeneralUtility.IsZero(transform.lossyScale.z);

            if (Type == SplineObjectType.DEFORMATION && (xZero || yZero || zZero))
                return false;

            if (hasFirstUpdateRun)
            {
                if(componentMode != ComponentMode.ACTIVE)
                {
                    if(!componentModeWarningTriggered)
                    {
                        Debug.LogError($"[Spline Architect] The component setting is not set to 'Active' on '{name}' ({splineParent.name})! Animating this object will not work.");
                        componentModeWarningTriggered = true;
                    }
                    return false;
                }
                else if (splineParent.componentMode != ComponentMode.ACTIVE)
                {
                    if(!componentModeWarningTriggered)
                    {
                        Debug.LogError($"[Spline Architect] The component setting is not set to 'Active' on '{splineParent.name}'! Deforming meshes along this spline will not work.");
                        componentModeWarningTriggered = true;
                    }
                    return false;
                }
            }

            if(type == SplineObjectType.DEFORMATION && meshContainers != null)
            {
                foreach (MeshContainer mc in meshContainers)
                {
                    Mesh instanceMesh = mc.GetInstanceMesh();
                    Mesh originMesh = mc.GetOriginMesh();

                    if (instanceMesh == null || originMesh == null)
                    {
                        return false;
                    }

                    if (originMesh == instanceMesh)
                    {
                        return false;
                    }

                    if (!instanceMesh.isReadable)
                    {
                        if (!readWriteWarningTriggered && hasFirstUpdateRun)
                        {
                            readWriteWarningTriggered = true;
                            Debug.LogError($"[Spline Architect] Can't deformation mesh during runtime becouse no read/write access on '{name}' ({splineParent.name}).");
                        }
                        return false;
                    }
                }
            }

            return true;
        }

        internal void UpdateInfo()
        {
            meshCountContainer.Clear();
            deformedVertecies = 0;
            deformations = 0;

            if (type != SplineObjectType.DEFORMATION)
                return;

            if (meshContainers == null)
                return;

            foreach (MeshContainer mc in meshContainers)
            {
                Mesh mesh = mc.GetOriginMesh();

                if (mc.Resolution > 0)
                    mesh = HandleCachedResources.FetchModifiedOriginalMesh(mc);

                if (mc != null &&
                    mc.GetMeshComponent() != null &&
                    mesh != null &&
                    !meshCountContainer.Contains(mesh))
                {
                    deformations++;
                    deformedVertecies += mesh.vertexCount;
                    meshCountContainer.Add(mesh);
                }
            }
        }
#endif
    }
}
