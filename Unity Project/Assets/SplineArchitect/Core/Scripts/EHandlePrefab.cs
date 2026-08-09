// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandlePrefab.cs
//
// Author: Mikael Danielsson
// Date Created: 25-05-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect
{
    internal class EHandlePrefab
    {
        internal static bool prefabStageOpen { get; private set; }
        internal static bool prefabStageClosedLastFrame { get; private set; }
        internal static bool prefabStageOpenedLastFrame { get; private set; }

        private static List<Spline> splinesContainer = new List<Spline>();
        private static HashSet<Spline> splinesUpdated = new HashSet<Spline>();

        internal static void OnPrefabUpdate(GameObject go)
        {
            UpdatedPrefabDeformations(go, false);

            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (source == null)
                return;

            SplineObject so = source.GetComponent<SplineObject>();

            if (so == null || so.MeshContainerCount == 0)
                return;

            EActionDelayed.Add(() =>
            {
                if (EHandleEvents.undoActive)
                    return;

                Debug.LogError($"[Spline Architect] You should not apply changes to the prefab '{source.name}' when it is deformed along a spline that is not part of the same prefab. Please undo these changes.\n" +
                               $"If you want to modify this prefab '{source.name}', open it in Prefab Mode and make the changes there.");

            }, 0, 0, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE , 33354);
        }

        internal static void OnPrefabRevert(GameObject go)
        {
            UpdatedPrefabDeformations(go, false);

            Spline spline = SplineUtility.TryFindSpline(go.transform);
            SplineObject so = go.GetComponent<SplineObject>();

            if(spline != null && so == null)
            {
                Transform[] childs = go.GetComponentsInChildren<Transform>();
                foreach (Transform child in childs)
                {
                    ESplineObjectUtility.TryAttacheOnTransformEditor(spline, child.transform, true);
                }
            }
        }

        internal static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            //Closing a prefab stage while discarding changes will trigger an OnPrefabStageOpened for some reason. We need to handle that case and skip deformations.
            if (!prefabStageOpen)
            {
                UpdatedPrefabDeformations(prefabStage.openedFromInstanceRoot, false);
            }
            prefabStageOpen = true;
            prefabStageOpenedLastFrame = true;
        }

        internal static void OnPrefabStageClosing(PrefabStage prefabStage)
        {
            UpdatedPrefabDeformations(prefabStage.openedFromInstanceRoot, true);
            prefabStageOpen = false;
            prefabStageClosedLastFrame = true;
        }

        internal static void UpdateGlobal()
        {
            splinesUpdated.Clear();
            prefabStageClosedLastFrame = false;
            prefabStageOpenedLastFrame = false;
        }

        internal static bool IsPartOfAnyPrefab(GameObject go)
        {
            return PrefabUtility.IsPartOfAnyPrefab(go);
        }

        internal static bool IsInPrefabHierarchy(GameObject go)
        {
            Transform transform = go.transform;

            for (int i = 0; i < 25; i++)
            {
                if(transform == null)
                    break;

                if (IsPartOfAnyPrefab(transform.gameObject))
                    return true;

                transform = transform.parent;
            }

            return false;
        }

        internal static bool IsPrefabRoot(GameObject go)
        {
            return PrefabUtility.IsOutermostPrefabInstanceRoot(go);
        }

        internal static PrefabStage GetCurrentPrefabStage()
        {
            return PrefabStageUtility.GetCurrentPrefabStage();
        }

        internal static GameObject GetOutermostPrefabRoot(GameObject go)
        {
            return PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        }

        internal static bool IsPrefabStageActive()
        {
            return PrefabStageUtility.GetCurrentPrefabStage() != null;
        }

        internal static PrefabAssetType GetPrefabAssetType(GameObject go)
        {
            return PrefabUtility.GetPrefabAssetType(go);
        }

        internal static bool IsPartOfActivePrefabStage(GameObject go)
        {
            if (PrefabStageUtility.GetPrefabStage(go) != null)
                return true;

            //Will be true when creating new spline:s inside prefabs.
            if (go.transform != null && PrefabStageUtility.GetPrefabStage(go.transform.gameObject) && IsPrefabStageActive())
                return true;

            //Will be true when creating new spline:s inside prefabs.
            if (go.transform.parent != null && PrefabStageUtility.GetPrefabStage(go.transform.parent.gameObject) && IsPrefabStageActive())
                return true;

            //prefabStageClosing only runs during one frame. The frame after PrefabStage.prefabStageClosing += OnPrefabStageClosing.
            if (prefabStageClosedLastFrame)
                return true;

            return false;
        }

        private static void UpdatedPrefabDeformations(GameObject go, bool closing)
        {
            foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
            {
                if (spline == null)
                    continue;

                if (splinesUpdated.Contains(spline))
                    continue;

                EHandleEvents.MarkForInfoUpdate(spline);

                for (int i = 0; i < spline.AllSplineObjectCount; i++)
                {
                    SplineObject so = spline.GetSplineObjectAtIndex(i);

                    if (so == null || so.transform == null)
                        continue;

                    if (closing && IsPartOfActivePrefabStage(so.gameObject))
                        continue;

                    TryAttachNewChildrenSplineObject(so);
                }

                spline.MarkEditorCacheDirty();
                splinesUpdated.Add(spline);
            }

            if (go == null)
                return;

            splinesContainer.Clear();
            go.GetComponentsInChildren(splinesContainer);

            foreach (Spline spline in splinesContainer)
            {
                for (int i2 = 0; i2 < spline.AllSplineObjectCount; i2++)
                {
                    SplineObject so = spline.GetSplineObjectAtIndex(i2);

                    if (so == null || so.transform == null)
                        continue;

                    //We dont want to deform deformations wihtin the prefab stage after its closed. We will get errors.
                    if (closing && IsPartOfActivePrefabStage(so.gameObject))
                        continue;

                    if (so.Type == SplineObjectType.DEFORMATION && so.MeshContainerCount > 0)
                    {
                        so.SyncInstanceMeshesFromCache();
                    }
                    else if (so.Type == SplineObjectType.FOLLOWER)
                    {
                        //Need to sync MeshContainers becouse the follower can have an instaceMesh without a MeshContainer.
                        so.SyncMeshContainers();

                        for(int i = 0; i < so.MeshContainerCount; i++)
                        {
                            MeshContainer mc = so.GetMeshContainerAtIndex(i);
                            Mesh mesh = mc.GetInstanceMesh();

                            if (mesh == null)
                                continue;

                            string assetPath = GeneralUtility.GetAssetPathOnlyEditor(mesh);

                            if (assetPath == "")
                            {
                                Mesh originMesh = ESplineObjectUtility.GetOriginMeshFromMeshNameId(mesh);

                                if (originMesh == null)
                                    continue;

                                mc.SetOriginMesh(originMesh);
                                mc.SetInstanceMeshToOriginMesh();
                            }
                        }
                    }
                }
            }

            void TryAttachNewChildrenSplineObject(SplineObject so)
            {
                if (so.Type != SplineObjectType.DEFORMATION)
                    return;

                //Need to delay one frame for some reason else the editor can crash after doing Undos 1 or 2 times.
                EActionDelayed.Add(() =>
                {
                    if (so == null)
                        return;

                    Transform[] childs = so.GetComponentsInChildren<Transform>();
                    foreach (Transform child in childs)
                    {
                        SplineObject soChild = child.GetComponent<SplineObject>();

                        if (soChild != null)
                            continue;

                        ESplineObjectUtility.TryAttacheOnTransformEditor(so.SplineParent, child, true);
                    }
                }, 0, 0, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE);
            }
        }
    }
}

#endif
