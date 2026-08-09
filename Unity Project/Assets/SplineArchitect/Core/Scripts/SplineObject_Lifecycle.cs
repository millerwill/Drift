// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObject_Lifecycle.cs
//
// Author: Mikael Danielsson
// Date Created: 28-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System;

using UnityEngine;
using Unity.Mathematics;

using SplineArchitect.Utility;
using SplineArchitect.Monitor;

namespace SplineArchitect
{
    public partial class SplineObject : MonoBehaviour
    {
        // Runtime data
        [NonSerialized] private bool hasOnEnableRun;
        [NonSerialized] private bool initalized;
        [NonSerialized] private bool initializedMeshes;

        private void OnEnable()
        {
            hasOnEnableRun = true;
            Initalize();
        }

        private void OnDisable()
        {
            if (splineParent == null)
                return;
#if UNITY_EDITOR
            // Need to wait for the first editor update loop, else meshes that should be deformed
            // in the editor will deform as if they where for runtime use.
            if (EHandleEvents.waitForEditor)
            {
                EActionDelayed.Add(() => 
                {
                    splineParent.RemoveSplineObject(this);
                }, 0, 0, EActionDelayed.ActionFlag.FRAMES);
                return;
            }
#endif
            splineParent.RemoveSplineObject(this);
        }

        private void Start()
        {
#if !UNITY_EDITOR
            if (splineParent != null)
                return;

            enabled = false;
#endif
        }

        private void OnDestroy()
        {
            if (splineParent == null)
                return;

            splineParent.RemoveSplineObject(this);
        }

        private void OnTransformParentChanged()
        {
            //During copying this will run before OnEnable and the monitor will be null. So we can't run the code below.
            //Note: The above text does not seem to apply any more after some testing (2026-02-25). Added hasOnEnableRun in case.
            if (!hasOnEnableRun)
                return;

            SplineObject oldSoParent = soParent;
            Spline oldSplineParent = splineParent;

            //Updates for old parent
            splineParent?.RemoveSplineObject(this);

            //Update parent data
            SyncParentData();

            //Detach from spline
            if (oldSplineParent != null && soParent == null && splineParent == null)
            {
                int detachType = 1;
#if UNITY_EDITOR
                if (UnityEditor.Undo.isProcessing) detachType = 0;
#endif
                oldSplineParent.detachList.Add((this, detachType));

                //Need to reassign soParent data becouse its needed during detach. We need to get the localspace from the soParent.
                soParent = oldSoParent;
            }
            //Detach from spline
            else if (oldSplineParent != null && oldSoParent != null && splineParent == null)
            {
                int detachType = 2;
                oldSplineParent.detachList.Add((this, detachType));
            }
            //Attach or change parent
            else if (splineParent != null)
            {
                //Updates for new parent
                splineParent.AddSplineObject(this);

                //Reorder for parent hierarchy
                if (soParent != null)
                {
                    splineParent.RemoveSplineObject(this);

                    for (int i = 0; i < splineParent.AllSplineObjectCount; i++)
                    {
                        SplineObject so = splineParent.GetSplineObjectAtIndex(i);

                        if (so != soParent)
                            continue;

                        //Always add child directly after parent in list
                        if (i + 1 >= splineParent.AllSplineObjectCount)
                            splineParent.AddSplineObject(this);
                        else
                            splineParent.AddSplineObject(this, i + 1);
                        break;
                    }
                }

                //Change parent
                if (transform.parent != null && oldSplineParent != null)
                {
                    float4x4 combinedMatrixOld = SplineObjectUtility.GetCombinedParentMatrixs(oldSoParent);
                    float4x4 combinedMatrix = SplineObjectUtility.GetCombinedParentMatrixs(soParent);
                    localSplinePosition = math.transform(combinedMatrixOld, localSplinePosition);
                    localSplinePosition = math.transform(math.inverse(combinedMatrix), localSplinePosition);
                    Quaternion combinedRotations = Quaternion.Inverse(SplineObjectUtility.GetCombinedParentRotations(soParent)) * 
                                                   SplineObjectUtility.GetCombinedParentRotations(oldSoParent);
                    localSplineRotation = Quaternion.Inverse(combinedRotations) * localSplineRotation;
#if UNITY_EDITOR
                    activationPosition = localSplinePosition;
                    EHandleEvents.InvokeAfterSplineObjectParentChanged(this);
#endif
                    oldSplineParent.RemoveFromActiveWorkers(this);
                }
                else
                {
                    splineParent.attachList.Add(this);
                }

                MarkVersionDirty();
            }
        }

        internal void Initalize()
        {
#if UNITY_EDITOR
            if (meshMode != MeshMode.SAVE_IN_BUILD && EHandleEvents.buildRunning)
                return;

            if (EHandleEvents.dragActive)
            {
                EHandleEvents.InitalizeAfterDrag(this);
                return;
            }

            if (undoState == null)
            {
                undoState = ScriptableObject.CreateInstance<ScriptableObjects.SplineObjectUndoState>();
                undoState.hideFlags = HideFlags.HideAndDontSave;
            }
#endif

            if (initalized)
            {
                splineParent?.AddSplineObject(this);
                return;
            }

            if (splineParent == null)
            {
                SyncParentData();

                if(type == SplineObjectType.NOT_SET)
                    type = defaultType;
            }

            if (monitor == null)
                monitor = new MonitorSplineObject(this);

            if (meshContainers == null)
                meshContainers = new List<MeshContainer>();

            if (splineParent == null)
            {
#if UNITY_EDITOR
                EHandleEvents.ForceUpdateSelection();
#endif
                return;
            }

            initalized = true;
            SyncMeshContainers();
            splineParent.AddSplineObject(this);

#if UNITY_EDITOR
            //Turn of static when playing in the editor for all deformations.
            //Else the static batached mesh will be offseted (only in editor playmode).
            if (type == SplineObjectType.DEFORMATION && Application.isPlaying && gameObject.isStatic)
                gameObject.isStatic = false;

            InitalizeEditor();
#endif
        }

        internal void EnsureMeshesInitialized()
        {
            if (initializedMeshes)
                return;

            initializedMeshes = true;

            if (type != SplineObjectType.DEFORMATION)
                return;

            CacheUntrackedInstanceMeshes();
            SyncMeshContainers();
            if (!Application.isPlaying || meshMode != MeshMode.DO_NOTHING)
                SyncInstanceMeshesFromCache();
        }

        internal void SyncParentData()
        {
            splineParent = TryFindSplineParent();

            soParent = transform.parent?.GetComponent<SplineObject>();
            if (soParent != null && soParent.Type == SplineObjectType.FOLLOWER)
            {
                soParent = null;
                splineParent = null;
            }
        }
    }
}
