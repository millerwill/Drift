// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleMeshContainer.cs
//
// Author: Mikael Danielsson
// Date Created: 18-02-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

using SplineArchitect.Utility;
using SplineArchitect.Ui;

namespace SplineArchitect
{
    internal static class EHandleMeshContainer
    {
        private static bool refresh;
        private static List<Component> componentContainer = new List<Component>();
        private static HashSet<MeshContainer> hasRunOriginMeshWarning = new HashSet<MeshContainer>();

        internal static void Refresh()
        {
            refresh = true;
        }

        public static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; i++)
            {
                Object changedObject = null;

                if (stream.GetEventType(i) == ObjectChangeKind.ChangeGameObjectOrComponentProperties)
                {
                    stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out ChangeGameObjectOrComponentPropertiesEventArgs args);
#if UNITY_6000_4_OR_NEWER
                    changedObject = EditorUtility.EntityIdToObject(args.entityId);
#elif UNITY_6000_3_OR_NEWER
                    changedObject = EditorUtility.EntityIdToObject(args.instanceId);
#else
                    changedObject = EditorUtility.InstanceIDToObject(args.instanceId);
#endif
                }
                else if (stream.GetEventType(i) == ObjectChangeKind.ChangeGameObjectStructure)
                {
                    stream.GetChangeGameObjectStructureEvent(i, out ChangeGameObjectStructureEventArgs args);
#if UNITY_6000_4_OR_NEWER
                    changedObject = EditorUtility.EntityIdToObject(args.entityId);
#elif UNITY_6000_3_OR_NEWER
                    changedObject = EditorUtility.EntityIdToObject(args.instanceId);
#else
                    changedObject = EditorUtility.InstanceIDToObject(args.instanceId);
#endif
                }

                if (changedObject == null)
                    continue;

                GameObject changedGameObject = changedObject as GameObject;

                if (changedGameObject != null)
                {
                    SplineObject so = changedGameObject.GetComponent<SplineObject>();

                    if (so == null)
                        continue;

                    if (so.Type != SplineObjectType.DEFORMATION)
                        continue;

                    if (so.SplineParent == null)
                        continue;

                    so.SyncMeshContainers();
                    so.SyncInstanceMeshesFromCache();
                    continue;
                }

                if (changedObject is Component component)
                {
                    changedGameObject = component.gameObject;
                }

                if (changedGameObject == null)
                    continue;

                LookForMeshChanges(changedGameObject);
            }

            void LookForMeshChanges(GameObject gameObject)
            {
                SplineObject so = gameObject.GetComponent<SplineObject>();

                if (so == null)
                    return;

                if (so.Type != SplineObjectType.DEFORMATION)
                    return;

                Spline splineParent = so.TryFindSplineParent();

                if (splineParent == null)
                    return;

                EHandleMeshContainer.CheckForOriginMeshChange(so);
            }
        }

        internal static void RefreshAfterAssetImport(HashSet<Spline> splines)
        {
            if (!refresh)
                return;

            refresh = false;
            foreach (Spline spline in splines)
            {
                for (int i2 = 0; i2 < spline.AllSplineObjectCount; i2++)
                {
                    SplineObject so = spline.GetSplineObjectAtIndex(i2);

                    if (so == null || so.transform == null)
                        continue;

                    if (so.Type != SplineObjectType.DEFORMATION)
                        continue;

                    bool foundModification = false;

                    for (int i = 0; i < so.MeshContainerCount; i++)
                    {
                        MeshContainer mc = so.GetMeshContainerAtIndex(i);
                        if (mc.TryUpdateTimestamp())
                        {
                            foundModification = true;
                            if (!TryUpdateOriginMesh(so, mc))
                                Debug.LogError($"[Spline Architect] Failed to update the origin mesh after the asset modification refresh on SplineObject {so.name}. " +
                                               $"Has the asset been deleted? If so, add the asset back and reload the scene.");
                        }
                        else if (mc.HasReadabilityDif())
                        {
                            foundModification = true;
                            RefreshInstanceMesh(so, mc);
                        }
                    }

                    if (foundModification)
                    {
                        so.MarkVersionDirty();
                        EHandleSpline.MarkForInfoUpdate(spline);
                    }
                }
                
                EHandleDeformation.ProcessSplineObjects(spline);
            }
        }

        internal static void Initialize(SplineObject so)
        {
            for (int i = 0; i < so.gameObject.GetComponentCount(); i++)
            {
                if (so.Type != SplineObjectType.DEFORMATION)
                    continue;

                Component component = so.gameObject.GetComponentAtIndex(i);
                MeshFilter meshFilter = component as MeshFilter;
                MeshCollider meshCollider = component as MeshCollider;

                Mesh sharedMesh = null;

                if (meshFilter != null)
                {
                    sharedMesh = meshFilter.sharedMesh;
                }
                else if (meshCollider != null)
                {
                    sharedMesh = meshCollider.sharedMesh;
                }

                if (sharedMesh == null)
                    continue;

                bool allreadyExists = false;
                for (int i2 = 0; i2 < so.MeshContainerCount; i2++)
                {
                    MeshContainer mc2 = so.GetMeshContainerAtIndex(i2);
                    if (mc2 != null && mc2.ContainsMeshComponent(component))
                    {
                        allreadyExists = true;
                        break;
                    }
                }
                if (allreadyExists) continue;

                Mesh originMesh = ESplineObjectUtility.GetOriginMeshFromMeshNameId(sharedMesh);
                if (originMesh != null && meshFilter != null)
                {
                    meshFilter.sharedMesh = originMesh;
                }
                else if (originMesh != null && meshCollider != null)
                {
                    meshCollider.sharedMesh = originMesh;
                }

                MeshContainer mc = new MeshContainer(component);
                so.AddMeshContainer(mc);
                mc.SetInstanceMesh(HandleCachedResources.FetchInstanceMesh(mc));
            }
        }

        internal static void DeleteUnvalidMeshContainers(SplineObject so)
        {
            for (int i = so.MeshContainerCount - 1; i >= 0; i--)
            {
                MeshContainer mc = so.GetMeshContainerAtIndex(i);

                if (mc == null)
                {
                    so.RemoveMeshContainer(mc);
                    continue;
                }

                if (mc.GetMeshComponent() == null)
                {
                    so.RemoveMeshContainer(mc);
                    continue;
                }

                Mesh instanceMesh = mc.GetInstanceMesh();

                if (instanceMesh == null)
                {
                    so.RemoveMeshContainer(mc);
                }
            }
        }

        internal static void DeleteDuplicates(Spline spline)
        {
            for (int i2 = 0; i2 < spline.AllSplineObjectCount; i2++)
            {
                SplineObject so = spline.GetSplineObjectAtIndex(i2);

                if (so.MeshContainerCount < 2)
                    continue;

                componentContainer.Clear();
                for (int i = so.MeshContainerCount - 1; i >= 0; i--)
                {
                    MeshContainer mc = so.GetMeshContainerAtIndex(i);
                    Component c = mc.GetMeshComponent();

                    if (componentContainer.Contains(c))
                    {
                        so.RemoveMeshContainerAt(i);
                        Debug.Log("[Spline Architect] Found MeshContainer duplicate! It's now removed. " +
                                  "this is for fixing a bug that can happen in older Spline Architect versions (1.2.5 or less).");
                    }
                    else
                    {
                        componentContainer.Add(c);
                    }
                }
            }
        }

        internal static void CheckForOriginMeshChange(SplineObject so)
        {
            if (so.Type != SplineObjectType.DEFORMATION)
                return;

            bool changed = false;

            //Update meshContainers
            for (int i = so.MeshContainerCount - 1; i >= 0; i--)
            {
                MeshContainer mc = so.GetMeshContainerAtIndex(i);

                if (mc.GetMeshComponent() == null)
                    continue;

                Mesh instanceMesh = mc.GetInstanceMesh();

                if (instanceMesh == null)
                    continue;

                if(so.SplineParent.jobState != JobState.IDLE)
                    continue;

                string[] meshNameSplit = instanceMesh.name.Split('*');
                string[] keySplit = mc.GetMeshKey().Split('*');

                if (meshNameSplit.Length != 5 || meshNameSplit.Length != keySplit.Length || 
                    meshNameSplit[0] != keySplit[0] || meshNameSplit[1] != keySplit[1] || meshNameSplit[2] != keySplit[2])
                {
                    changed = true;
                    if(!TryUpdateOriginMesh(so, mc) && !hasRunOriginMeshWarning.Contains(mc))
                    {
                        hasRunOriginMeshWarning.Add(mc);
                        Debug.LogError($"[Spline Architect] Failed to update the origin mesh on SplineObject {so.name} at index {i}. " +
                                       $"Has the asset been deleted? If so, add the asset back and reload the scene.");
                    }
                }
            }

            WindowBase.RepaintAll();

            if (changed)
            {
                EHandleSpline.MarkForInfoUpdate(so.SplineParent);
                EActionDelayed.Add(() =>
                {
                    so.RenderMeshInternal(true);
                }, 0, 0, EActionDelayed.ActionFlag.FRAMES);
            }
        }

        private static bool TryUpdateOriginMesh(SplineObject so, MeshContainer mc)
        {
            Mesh instanceMesh = mc.GetInstanceMesh();
            Mesh oldOriginMesh = mc.GetOriginMesh();

            // First try get mesh from name.
            Mesh newOriginMesh = ESplineObjectUtility.GetOriginMeshFromMeshNameId(instanceMesh);

            // Second, if mesh is the same, skip updating.
            if (oldOriginMesh != null && newOriginMesh == oldOriginMesh)
                return true;

            // Third, retrive the original mesh directly from the mc. Moving assets to other folders will trow errors if this is not here.
            if (newOriginMesh == null)
                newOriginMesh = mc.GetOriginMesh();

            // Fourth, if the instance mesh is null, try to fetch the origin mesh from the container.
            if (instanceMesh == null)
            {
                // Instance mesh was invalidated during reimport; fall through to use originMesh if available.
                if (newOriginMesh != null)
                {
                    Update(newOriginMesh, $"[Spline Architect] Mesh change detected for '{so.name}'. Instance mesh was null, regenerating from origin mesh.");
                    return true;
                }
                return false;
            }

            // Fifth, try get mesh from guid and localId. If a mesh is found this way, this mesh should always be retrived and used.
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instanceMesh, out string guid, out long localId);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid));
            foreach (Object a in assets)
            {
                if (a == null) 
                    continue;

                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out var g, out long id))
                {
                    if (g == guid && id == localId)
                    {
                        newOriginMesh = (Mesh)a;
                        break;
                    }
                }
            }

            if (newOriginMesh != null)
            {
                if (newOriginMesh == oldOriginMesh)
                    return true;

                Update(newOriginMesh, $"[Spline Architect] Mesh change detected for '{so.name}'. New instance mesh was regenerated.");
                return true;
            }

            return false;

            void Update(Mesh originMesh, string msg)
            {
                mc.SetOriginMesh(originMesh);
                mc.UpdateKeys();

                if (msg != null && !HandleCachedResources.IsInstanceMeshCached(mc.GetMeshKey()))
                    Debug.Log(msg);

                mc.SetInstanceMesh(HandleCachedResources.FetchInstanceMesh(mc));
                so.MarkVersionDirty();
                so.RenderMeshInternal(false);
            }
        }

        private static void RefreshInstanceMesh(SplineObject so, MeshContainer mc)
        {
            Mesh orginMesh = mc.GetOriginMesh();
            Mesh newInstanceMesh = Object.Instantiate(orginMesh);
            mc.SetInstanceMesh(newInstanceMesh);
            HandleCachedResources.AddOrUpdateInstanceMesh(mc);
            so.MarkVersionDirty();

            if (orginMesh.isReadable != newInstanceMesh.isReadable)
                Debug.LogError("[Spline Architect] Redable status dif error!");
        }
    }
}
