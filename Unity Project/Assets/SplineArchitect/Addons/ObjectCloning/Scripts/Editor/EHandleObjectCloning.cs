// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleObjectCloning.cs
//
// Author: Mikael Danielsson
// Date Created: 21-04-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using SplineArchitect.CustomTools;
using SplineArchitect.Libraries;
using SplineArchitect.Ui;
using SplineArchitect.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SplineArchitect
{
    public class EHandleObjectCloning
    {
        const string name = "Object Cloning";
        const string version = "1.3.6";

        public static Texture2D textureClone { get; private set; }
        public static Texture2D textureCloneActive { get; private set; }
        public static GUIContent iconClone { get; private set; }
        public static GUIContent iconCloneActive { get; private set; }
        public static GUIContent sOPositionFieldWarningText { get; private set; }

        private static List<SplineObject> splineObjectContainer = new List<SplineObject>();
        private static List<SplineObject> splineObjectContainer2 = new List<SplineObject>();
        private static List<SplineObject> splineObjectContainer3 = new List<SplineObject>();
        private static List<Spline> splineContainer = new List<Spline>();

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AftertAssemblyReload()
        {
            //Events
            EHandleEvents.beforeFirstUpdate += OnFirstUpdate;
            EHandleEvents.beforeAssetImport += BeforeAssetImport;
            EHandleEvents.beforeWindowExtendedGUI += BeforeWindowExtendedGUI;
            EHandleEvents.beforeSegmentRemoved += OnSegmentDeleted;
            EHandleEvents.afterSplineSplit += OnSplineSplit;
            EHandleEvents.afterSplineJoin += OnSplineChange;
            EHandleEvents.afterSplineReverse += OnSplineChange;
            EHandleEvents.afterSplineLoop += OnSplineChange;
            EHandleEvents.afterSegmentLinked += OnSegmentLinked;
            EHandleEvents.afterSplineObjectActivatePositionTool += AfterSplineObjectActivatePositionTool;
            EHandleEvents.afterSplineObjectParentChanged += OnSplineObjectParentChanged;
            EHandleEvents.afterSplineOnbjectSetPositionInUi += AfterSplineOnbjectSetPositionInUi;
            EHandleEvents.beforeWindowExtendedGUI += BeforeWindowExtendedGUI;
            SceneView.beforeSceneGui += BeforeSceneGUI;

            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            //Addon display name
            WindowInfo.DisplayAddonName($"{name} {version}");
        }

        private static void OnFirstUpdate()
        {
            string mainFolderPath = EHandleFolder.GetMainFolderPath();

            //Deform terrain
            textureClone = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}/Addons/ObjectCloning/Textures/cloneIcon.png");
            textureCloneActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}/Addons/ObjectCloning/Textures/cloneIcon_active.png");
            iconClone = new GUIContent(textureClone, "Object Cloning");
            iconCloneActive = new GUIContent(textureCloneActive, "Object Cloning");
            sOPositionFieldWarningText = new GUIContent(LibraryTexture.iconWarningMsg, "Position locked because this SplineObject is a clone. Disconnect it in the Object Cloning submenu to unlock the position fields.");
            WindowExtended.AddSubMenuSplineObject("objectCloning", iconClone, iconCloneActive, ObjectCloningUi.DrawSplineObjectWindow, ObjectCloningUi.CalcSplineObjectWindowSize);
        }

        static void BeforeWindowExtendedGUI(Event e, Spline spline, SplineObject so)
        {
            if (so == null)
                return;

            if (so.SplineParent == null)
                return;

            SplineObject cloneParent = GetCloneParent(so);

            if (cloneParent != null)
            {
                if (!IsOriginClone(so, cloneParent))
                {
                    WindowExtended.disableSOPosition = true;
                    WindowExtended.sOPositionFieldWarningText = sOPositionFieldWarningText;
                }

                WindowExtended.disableSOAutoType = true;
                WindowExtended.disableSOType = true;
            }

            if (so.IsCloneVersionDirty())
            {
                UpdateCloneAmount(so);
            }
            foreach (SplineObject so2 in EHandleSelection.selectedSplineObjects)
            {
                if (so2.IsCloneVersionDirty())
                    UpdateCloneAmount(so2);
            }
        }

        private static void BeforeAssetImport()
        {
            // Needs to be here else the scene will become dirty again after saving. 
            if (EHandleScene.isSaving)
                return;

            TryUpdateSelection();
        }

        private static void BeforeSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                TryUpdateSelection();
            }
        }

        private static void TryUpdateSelection()
        {
            Spline spline = EHandleSelection.selectedSpline;

            if (spline == null)
            {
                GameObject selectedGo = Selection.activeGameObject;
                if (selectedGo != null)
                    spline = selectedGo.GetComponent<Spline>();
            }

            if (spline != null)
            {
                for (int i = spline.AllSplineObjectCount - 1; i >= 0; i--)
                {
                    if (i >= spline.AllSplineObjectCount)
                        continue;

                    UpdateClones(spline.GetSplineObjectAtIndex(i));
                }

                // Update clones for links
                foreach (Segment s in spline.segments)
                {
                    if (s.LinkCount > 0)
                    {
                        for (int i3 = 0; i3 < s.LinkCount; i3++)
                        {
                            Segment link = s.GetLinkAtIndex(i3);

                            if (link == s)
                                continue;

                            if (link.SplineParent == null)
                            {
                                Debug.LogError("[Spline Architect] Spline parent is null!");
                                continue;
                            }

                            for (int i2 = link.SplineParent.AllSplineObjectCount - 1; i2 >= 0; i2--)
                            {
                                UpdateClones(link.SplineParent.GetSplineObjectAtIndex(i2));
                            }
                        }
                    }
                }

                // Update all childed splines
                splineContainer.Clear();
                spline.GetComponentsInChildren(splineContainer);
                foreach (Spline spline2 in splineContainer)
                {
                    if (spline2 == spline)
                        continue;

                    for (int i2 = spline2.AllSplineObjectCount - 1; i2 >= 0; i2--)
                    {
                        if (i2 >= spline2.AllSplineObjectCount)
                            continue;

                        UpdateClones(spline2.GetSplineObjectAtIndex(i2));
                    }
                }
            }
            else
            {
                if (Selection.activeGameObject == null)
                    return;

                SplineConnector[] connectors = Selection.activeGameObject.GetComponentsInChildren<SplineConnector>();

                splineContainer.Clear();
                foreach (SplineConnector connector in connectors)
                {
                    if (connector == null)
                        continue;

                    for (int i = 0; i < connector.ConnectionCount; i++)
                    {
                        Segment s = connector.GetConnectionAtIndex(i);

                        if (s == null)
                            continue;

                        if (s.linkTarget == LinkTarget.NONE)
                            continue;

                        if (s.SplineParent == null)
                        {
                            Debug.LogError($"[Spline Architect] Could not find spline parent for segment! {connectors.Length} {connector.name}");
                            continue;
                        }

                        if (!splineContainer.Contains(s.SplineParent))
                            splineContainer.Add(s.SplineParent);
                    }
                }

                foreach (Spline spline2 in splineContainer)
                {
                    for (int i = spline2.AllSplineObjectCount - 1; i >= 0; i--)
                    {
                        if (i >= spline2.AllSplineObjectCount)
                            continue;

                        UpdateClones(spline2.GetSplineObjectAtIndex(i));
                    }

                    EHandleDeformation.ProcessSplineObjects(spline2);
                }
            }

            void UpdateClones(SplineObject so)
            {
                if (so == null)
                    return;

                if (so.Type != SplineObjectType.DEFORMATION)
                    return;

                if (!so.cloningEnabled)
                    return;

                UpdateCloneAmount(so);
            }
        }

        private static void OnSplineSplit(Spline spline, Spline newSpline)
        {
            if (spline == null)
                return;

            for(int i = 0; i < spline.AllSplineObjectCount; i++)
            {
                SplineObject so = spline.GetSplineObjectAtIndex(i);

                if (so == null)
                    continue;

                if (so.Type != SplineObjectType.DEFORMATION)
                    continue;

                if (!so.cloningEnabled)
                    continue;

                EActionDelayed.Add(() => { 
                    UpdateCloneAmount(so);
                }, 0, 0, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE);
            }
        }

        private static void AfterSplineOnbjectSetPositionInUi(SplineObject so)
        {
            if (so == null)
                return;

            if (so.Type != SplineObjectType.DEFORMATION)
                return;

            if (!so.cloningEnabled)
                return;

            UpdateCloneAmount(so);
        }

        private static void OnSegmentLinked(Segment segment)
        {
            OnSplineChange(segment.splineParent);
        }

        private static void OnSplineChange(Spline spline)
        {
            if (spline == null)
                return;

            for (int i = 0; i < spline.AllSplineObjectCount; i++)
            {
                SplineObject so = spline.GetSplineObjectAtIndex(i);

                if (so == null)
                    continue;

                if (so.Type != SplineObjectType.DEFORMATION)
                    continue;

                if (!so.cloningEnabled)
                    continue;

                EActionDelayed.Add(() => {
                    UpdateCloneAmount(so);

                    foreach (SplineObject so2 in so.originClones)
                    {
                        if (so2 == null)
                            continue;

                        so2.MarkVersionDirty();
                    }

                    foreach (SplineObject so2 in so.clones)
                    {
                        if (so2 == null)
                            continue;

                        so2.MarkVersionDirty();
                    }

                    EHandleDeformation.ProcessSplineObjects(so.SplineParent);
                }, 0, 0, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE);
            }
        }

        private static void OnSegmentDeleted(Segment segment)
        {
            if (segment == null)
                return;

            Spline spline = segment.SplineParent;

            if (spline == null)
                return;

            for (int i = spline.AllSplineObjectCount - 1; i >= 0; i--)
            {
                if (i >= spline.AllSplineObjectCount)
                    continue;

                SplineObject so = spline.GetSplineObjectAtIndex(i);

                if (so == null)
                    continue;

                if (so.Type != SplineObjectType.DEFORMATION)
                    continue;

                if (!so.cloningEnabled)
                    continue;

                //We need to delay UpdateCloneAmount to the next update. Else the spline have not updated its data.
                EActionDelayed.Add(() => 
                {
                    UpdateCloneAmount(so);
                }, 0, 0, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE);
            }
        }

        private static void OnUndoRedoPerformed()
        {
            Spline selectedSpline = EHandleSelection.selectedSpline;

            if (selectedSpline != null)
                UpdateSpline(selectedSpline);

            SplineObject selectedSo = EHandleSelection.selectedSplineObject;

            if (selectedSo != null && selectedSo.SplineParent != null)
                UpdateSpline(selectedSo.SplineParent);

            void UpdateSpline(Spline spline)
            {
                for (int i = 0; i < spline.AllSplineObjectCount; i++)
                {
                    SplineObject so = spline.GetSplineObjectAtIndex(i);

                    if (so.clones == null || so.clones.Count == 0)
                        continue;

                    if (so.originClones == null || so.originClones.Count == 0)
                        continue;

                    if (so.IsCloneVersionDirty())
                        UpdateCloneAmount(so);

                    if (so.SplineParent == null)
                        continue;

                    so.SplineParent.directSystemWorker.Add(so);
                    foreach (SplineObject so2 in so.clones) ForceUpdate(so2);
                    foreach (SplineObject so2 in so.originClones) ForceUpdate(so2);
                }

                if (spline.directSystemWorker.HasWork())
                    spline.directSystemWorker.Complete();
            }

            void ForceUpdate(SplineObject so)
            {
                if (so == null)
                    return;

                SplineObject[] soChilds = so.transform.GetComponentsInChildren<SplineObject>();
                foreach (SplineObject soChild in soChilds)
                {
                    if (soChild.Type == SplineObjectType.DEFORMATION)
                    {
                        // Ugly fix, but during redo the mesh can become invisible.
                        soChild.SyncMeshContainers();
                        soChild.SyncInstanceMeshesFromCache();
                        soChild.RenderMeshInternal(true);
                    }

                    soChild.SplineParent.directSystemWorker.Add(soChild);
                }
            }
        }

        private static void OnSplineObjectParentChanged(SplineObject so)
        {
            if (so == null)
                return;

            if (so.Type != SplineObjectType.DEFORMATION)
                return;

            if (!so.cloningEnabled)
                return;

            EActionDelayed.Add(() => {
                UpdateCloneAmount(so);
                EHandleDeformation.ProcessSplineObjects(so.SplineParent);
            }, 0, 0, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE);
        }

        private static void AfterSplineObjectActivatePositionTool(SplineObject so)
        {
            //Lock position tool when a clone is selected
            SplineObject selectedSo = EHandleSelection.selectedSplineObject;
            if (selectedSo != null)
            {
                SplineObject cloneParent = GetCloneParent(selectedSo);
                bool isOriginClone = IsOriginClone(selectedSo, cloneParent);
                if (cloneParent != null && !isOriginClone)
                {
                    PositionTool.lockedWarningMsg = "[Spline Architect] Can't move cloned GameObject!";
                    PositionTool.locked = true;
                }
            }
        }

        private static int GetCurrentCloneSections(SplineObject cloneParent)
        {
            return cloneParent.clones.Count / cloneParent.originClones.Count + 1;
        }

        private static void CreateClones(SplineObject cloneParent, int amount, float sectionLength)
        {
            //Amount check menu
            if (amount * cloneParent.originClones.Count > 999)
            {
                bool clicksOk = EditorUtility.DisplayDialog("Spline Architect Warning",
                                                         $"You are about to clone {amount * cloneParent.originClones.Count} GameObject(s). Do you want to continue?",
                                                         "Yes",
                                                         "No"
                );

                if (!clicksOk)
                {
                    amount = 0;
                    DeleteClones(cloneParent, GetCurrentCloneSections(cloneParent) - 1);
                    EHandleUndo.RecordNow(cloneParent);
                    cloneParent.cloningEnabled = false;
                }
            }

            int totalSections = GetCurrentCloneSections(cloneParent);

            for (int i = totalSections; i < totalSections + amount; i++)
            {
                foreach (SplineObject originClone in cloneParent.originClones)
                {
                    if (originClone.MeshContainerCount > 0 && originClone.GetMeshContainerAtIndex(0).GetOriginMesh() == null)
                    {
                        Debug.LogError($"[Spline Architect] Origin clone {originClone.name} has an invalid origin mesh! Could not create new clones.");
                        return;
                    }

                    //Create clone
                    cloneParent.editorDisableOnChildrenChanged = true;
                    GameObject goClone = Object.Instantiate(originClone.gameObject, cloneParent.transform);
                    cloneParent.editorDisableOnChildrenChanged = false;

                    GameObject prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(originClone.gameObject);
                    if (prefab != null && PrefabUtility.IsAnyPrefabInstanceRoot(originClone.gameObject))
                    {
                        ConvertToPrefabInstanceSettings settings = new();
                        settings.componentsNotMatchedBecomesOverride = true;
                        settings.objectMatchMode = ObjectMatchMode.ByHierarchy;
                        settings.recordPropertyOverridesOfMatches = true;
                        settings.logInfo = false;
                        settings.changeRootNameToAssetName = false;
                        PrefabUtility.ConvertToPrefabInstance(goClone, prefab, settings, InteractionMode.AutomatedAction);
                    }

                    EHandleUndo.RegisterCreatedObject(goClone, "Created clones");
                    SplineObject soClone = goClone.GetComponent<SplineObject>();
                    EHandleUndo.RecordNow(soClone);
                    if (cloneParent.cloneDirection == CloneDirection.BACKWARD)
                    {
                        soClone.localSplinePosition -= new Vector3(0, 0, sectionLength) * i;
                    }
                    else
                    {
                        soClone.localSplinePosition += new Vector3(0, 0, sectionLength) * i;
                    }
                    soClone.transform.localScale = originClone.transform.localScale;
                    EHandleUndo.RecordNow(cloneParent);
                    cloneParent.clones.Add(soClone);

                    // Need to do this else children to the cloned SplineObject can get the wrong name on its instance mesh, so it will not match the Mesh data key.
                    // It will have the component isntance id from the original clone.
                    splineObjectContainer3.Clear();
                    goClone.transform.GetComponentsInChildren(splineObjectContainer3);
                    foreach (SplineObject soChild in splineObjectContainer3)
                    {
                        if (soChild.Type != SplineObjectType.DEFORMATION)
                            continue;

                        soChild.SyncMeshContainers();
                        soChild.SyncInstanceMeshesFromCache();
                    }
                }
            }

            EHandleUndo.RecordNow(cloneParent);
            cloneParent.cloneAmount = GetCurrentCloneSections(cloneParent);
        }

        public static void DeleteClones(SplineObject cloneParent, int amount)
        {
            if (cloneParent.clones == null)
                return;

            int stop = cloneParent.clones.Count - (amount * cloneParent.originClones.Count);
            if (stop < 0)
                stop = 0;

            EHandleUndo.RecordNow(cloneParent, "Deleted clones", EHandleUndo.RecordType.REGISTER_COMPLETE_OBJECT);
            for (int i = cloneParent.clones.Count - 1; i >= stop; i--)
            {
                SplineObject soChild = cloneParent.clones[i];
                cloneParent.clones.RemoveAt(i);

                if (soChild != null)
                {
                    EHandleUndo.DestroyObjectImmediate(soChild.gameObject);
                }
            }

            if (!cloneParent.cloneUseFixedAmount)
            {
                if (cloneParent.clones.Count == 0 || cloneParent.originClones.Count == 0)
                    cloneParent.cloneAmount = 0;
                else
                    cloneParent.cloneAmount = GetCurrentCloneSections(cloneParent);
            }
        }

        private static (int, float) GetAmountAndSectionLength(SplineObject cloneParent)
        {
            float start = 99999;
            float end = -99999;
            Bounds endBounds = new Bounds();
            Bounds startBounds = new Bounds();

            foreach (SplineObject originClone in cloneParent.originClones)
            {
                if(originClone == null ||originClone.transform == null)
                    continue;

                Bounds transformedBounds = GetTransformedBounds(originClone);
                float l = transformedBounds.center.z - transformedBounds.extents.z;
                float h = transformedBounds.center.z + transformedBounds.extents.z;
                if (l < start)
                {
                    start = l;
                    startBounds = transformedBounds;
                }
                if (h > end)
                {
                    end = h;
                    endBounds = transformedBounds;
                }
            }

            //Section length
            float sectionLength = end - start + cloneParent.cloneOffset.z;
            //Offset adjustment for origin clones
            float adjustment = (cloneParent.splinePosition.z - start) + (sectionLength / 2);
            if(cloneParent.cloneSnapEnd) adjustment = (cloneParent.splinePosition.z - start) + endBounds.extents.z;
            //Calculate amount of clones
            int amount = Mathf.FloorToInt((cloneParent.SplineParent.Length - cloneParent.splinePosition.z + adjustment) / sectionLength);
            if (cloneParent.cloneUseFixedAmount)
                amount = cloneParent.cloneAmount;
            else if (cloneParent.cloneDirection == CloneDirection.BACKWARD)
            {
                adjustment = (end - cloneParent.splinePosition.z) + (sectionLength / 2);
                if (cloneParent.cloneSnapEnd) adjustment = (end - cloneParent.splinePosition.z) + startBounds.extents.z;
                amount = Mathf.FloorToInt((cloneParent.splinePosition.z + adjustment) / sectionLength);
            }
            amount = Mathf.FloorToInt(amount / cloneParent.transform.localScale.z);
            if(amount < 0) amount = 0;
            
            return (amount, sectionLength);
        }

        private static Bounds GetTransformedBounds(SplineObject splineObject)
        {
            Bounds transformedBounds = GetBounds(splineObject);

            splineObjectContainer2.Clear();
            splineObject.gameObject.GetComponentsInChildren(splineObjectContainer2);

            foreach(SplineObject soChild in splineObjectContainer2)
            {
                if (soChild == null)
                    continue;

                transformedBounds.Encapsulate(GetBounds(soChild));
            }

            return transformedBounds;

            Bounds GetBounds(SplineObject so)
            {
                Bounds bounds = new Bounds();
                bounds.size = Vector3.one;
                bounds.center = splineObject.splinePosition;

                if (so.Type == SplineObjectType.DEFORMATION)
                {
                    if (so.MeshContainerCount > 0)
                    {
                        Mesh originMesh = so.GetMeshContainerAtIndex(0).GetOriginMesh();
                        if (originMesh != null)
                            bounds = GeneralUtility.TransformBounds(originMesh.bounds, SplineObjectUtility.GetCombinedParentMatrixs(so, true));
                    }
                }
                else
                {
                    MeshFilter meshFilter = so.GetComponent<MeshFilter>();
                    if (meshFilter == null)
                    {
                        MeshCollider meshCollider = so.GetComponent<MeshCollider>();
                        if (meshCollider != null && meshCollider.sharedMesh != null)
                            bounds = GeneralUtility.TransformBounds(meshCollider.sharedMesh.bounds, SplineObjectUtility.GetCombinedParentMatrixs(so, true));
                    }
                    else
                    {
                        if (meshFilter.sharedMesh != null)
                            bounds = GeneralUtility.TransformBounds(meshFilter.sharedMesh.bounds, SplineObjectUtility.GetCombinedParentMatrixs(so, true));
                    }
                }

                return bounds;
            }
        }

        public static void UpdateCloneAmount(SplineObject cloneParent)
        {
            if (!cloneParent.cloningEnabled)
                return;

            foreach(SplineObject originClone in cloneParent.originClones)
            {
                if(originClone == null)
                {
                    Debug.LogError("[Spline Architect] Origin clone was null! Can't update clones. Have you deleted a origin clone?");
                    return;
                }
            }

            int currentSections = GetCurrentCloneSections(cloneParent);
            (int, float) amountAndSectionLength = GetAmountAndSectionLength(cloneParent);
            int newAmount = amountAndSectionLength.Item1;
            float sectionLength = amountAndSectionLength.Item2;
            int dif = newAmount - currentSections;

            UpdateCloneOffset(cloneParent, sectionLength);

            if (dif > 0)
            {
                CreateClones(cloneParent, dif, sectionLength);
            }
            else if (dif < 0)
            {
                DeleteClones(cloneParent, Mathf.Abs(dif));
            }

            UpdateCloneFitting(cloneParent);
        }

        public static void UpdateCloneFitting(SplineObject cloneParent)
        {
            foreach (SplineObject so in cloneParent.clones)
            {
                if (so == null || so.transform == null)
                    continue;

                splineObjectContainer.Clear();
                so.gameObject.GetComponentsInChildren(splineObjectContainer);
                foreach (SplineObject soChild in splineObjectContainer) ResetSnapData(soChild);
            }

            if (!cloneParent.cloneSnapEnd || cloneParent.cloneUseFixedAmount)
                return;

            int totalOrigins = cloneParent.originClones.Count;
            int totalClones = cloneParent.clones.Count;

            splineObjectContainer.Clear();
            float endDistance = -99999;
            float startDistance = 99999;

            for (int i = 0; i < totalClones; i++)
            {
                SplineObject clone = cloneParent.clones[i];

                if (clone == null)
                    continue;

                Bounds transformedBounds = GetTransformedBounds(clone);
                float end = transformedBounds.center.z + transformedBounds.extents.z;
                float start = transformedBounds.center.z - transformedBounds.extents.z;

                if ((cloneParent.cloneDirection == CloneDirection.FORWARD && GeneralUtility.IsEqual(endDistance, end)) ||
                    (cloneParent.cloneDirection == CloneDirection.BACKWARD && GeneralUtility.IsEqual(startDistance, start)))
                {
                    splineObjectContainer.Add(clone);
                }
                else if ((cloneParent.cloneDirection == CloneDirection.FORWARD && endDistance < end) ||
                         (cloneParent.cloneDirection == CloneDirection.BACKWARD && startDistance > start))
                {
                    endDistance = end;
                    startDistance = start;
                    splineObjectContainer.Clear();
                    splineObjectContainer.Add(clone);
                }
            }

            (int, float) amountAndSectionLength = GetAmountAndSectionLength(cloneParent);

            foreach (SplineObject so in splineObjectContainer)
            {
                if (so.Type == SplineObjectType.FOLLOWER)
                    continue;

                splineObjectContainer2.Clear();
                so.gameObject.GetComponentsInChildren(splineObjectContainer2);

                foreach(SplineObject soChild in splineObjectContainer2)
                {
                    if (soChild.MeshContainerCount > 0)
                        SetSnapData(soChild);
                }
            }

            void ResetSnapData(SplineObject x)
            {
                EHandleUndo.RecordNow(x);
                x.snapSettings.snapMode = SnapMode.NONE;
                x.snapSettings.endSnapDistance = 1;
                x.snapSettings.startSnapDistance = 1;
                x.snapSettings.endSnapOffset = 0;
                x.snapSettings.startSnapOffset = 0;
                x.snapSettings.snapTargetPoint = 0;
                x.MarkVersionDirty();
            }

            void SetSnapData(SplineObject x)
            {
                EHandleUndo.RecordNow(x);
                x.snapSettings.snapMode = SnapMode.CONTROL_POINTS;
                if (cloneParent.cloneDirection == CloneDirection.FORWARD)
                {
                    x.snapSettings.endSnapDistance = amountAndSectionLength.Item2 * 1.01f;
                    x.snapSettings.startSnapDistance = 0;
                    x.snapSettings.endSnapOffset = cloneParent.cloneSnapEndOffset;
                }
                else
                {
                    x.snapSettings.startSnapDistance = amountAndSectionLength.Item2 * 1.01f;
                    x.snapSettings.endSnapDistance = 0;
                    x.snapSettings.startSnapOffset = cloneParent.cloneSnapEndOffset;
                }
                x.MarkVersionDirty();
            }
        }

        public static void ToggleCloneDirection(SplineObject cloneParent)
        {
            if (!cloneParent.cloningEnabled)
                return;

            if (cloneParent.clones != null)
            {
                foreach (SplineObject clone in cloneParent.clones)
                {
                    if (clone == null)
                        continue;

                    EHandleUndo.RecordNow(clone, "Updated clone direction");
                    clone.localSplinePosition.z = clone.localSplinePosition.z * -1;
                }

                foreach (SplineObject originClone in cloneParent.originClones)
                {
                    if (originClone == null)
                        continue;

                    EHandleUndo.RecordNow(originClone, "Updated clone direction");
                    originClone.localSplinePosition.z = originClone.localSplinePosition.z * -1;
                }
            }

            UpdateCloneAmount(cloneParent);
        }

        private static void UpdateCloneOffset(SplineObject cloneParent, float sectionLength)
        {
            if (cloneParent.cloneDirection == CloneDirection.BACKWARD) 
                sectionLength *= -1;

            if (cloneParent.clones != null && cloneParent.clones.Count > 0)
            {
                for (int i = 0; i < cloneParent.clones.Count; i++)
                {
                    SplineObject clone = cloneParent.clones[i];
                    SplineObject originClone = cloneParent.originClones[i % cloneParent.originClones.Count];

                    if (originClone.MeshContainerCount > 0 && originClone.GetMeshContainerAtIndex(0).GetOriginMesh() == null)
                    {
                        Debug.LogError($"[Spline Architect] Origin clone {originClone.name} as an invalid origin mesh! Could not update offsets for clones.");
                        return;
                    }

                    if (clone == null || originClone == null)
                        continue;

                    int column = 1;
                    if(i > 0) column += Mathf.FloorToInt(i / cloneParent.originClones.Count);
                    Vector3 p = new Vector3(cloneParent.cloneOffset.x * column + originClone.localSplinePosition.x,
                                            cloneParent.cloneOffset.y * column + originClone.localSplinePosition.y,
                                            sectionLength * column + originClone.localSplinePosition.z);

                    if (GeneralUtility.IsEqual(p, clone.localSplinePosition, 0.001f))
                        continue;

                    EHandleUndo.RecordNow(clone);
                    clone.localSplinePosition = p;
                }
            }
        }

        internal static bool IsOriginClone(SplineObject so, SplineObject cloneParent = null)
        {
            if (cloneParent == null)
                cloneParent = GetCloneParent(so);

            if (cloneParent == null)
                return false;

            if (cloneParent.originClones == null || cloneParent.originClones.Count == 0)
                return false;                

            return cloneParent.originClones.Contains(so);
        }

        internal static SplineObject GetCloneParent(SplineObject so)
        {
            SplineObject parent = so.SoParent;

            for (int i = 0; i < 25; i++)
            {
                if (parent == null)
                    break;

                if (parent.cloningEnabled && parent.clones != null && (parent.clones.Contains(so) || parent.originClones.Contains(so)))
                    return parent;

                parent = parent.SoParent;
            }

            return null;
        }

        public static void EnableUsingChildren(Spline spline, SplineObject cloneParent)
        {
            if (cloneParent.clones == null)
                cloneParent.clones = new List<SplineObject>();

            if (cloneParent.originClones == null)
                cloneParent.originClones = new List<SplineObject>();

            cloneParent.clones.Clear();
            cloneParent.originClones.Clear();

            int childCount = cloneParent.transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = cloneParent.transform.GetChild(i);

                if (childTransform == null)
                    continue;

                SplineObject soOriginClone = childTransform.GetComponent<SplineObject>();

                if (soOriginClone == null)
                    continue;

                cloneParent.originClones.Add(soOriginClone);
            }

            EHandleUndo.RecordNow(cloneParent);
            cloneParent.cloningEnabled = true;
            if(!GeneralUtility.IsEqual(cloneParent.transform.localScale, Vector3.one))
            {
                Debug.LogWarning($"[Spline Architect] Clone parent '{cloneParent.name}' did not have a scale of (1,1,1). Scale has been reset to (1,1,1).");
                cloneParent.transform.localScale = Vector3.one;
            }

            (int, float) amountAndSectionLength = GetAmountAndSectionLength(cloneParent);
            int amount = amountAndSectionLength.Item1;
            float sectionLength = amountAndSectionLength.Item2;

            CreateClones(cloneParent, amount - 1, sectionLength);
            UpdateCloneFitting(cloneParent);
        }

        public static void EnableUsingSelf(Spline spline, SplineObject origin)
        {
            Transform originParent = origin.transform.parent;
            SplineObject originParentSo = originParent.GetComponent<SplineObject>();

            GameObject cloneParentGo = new GameObject();
            EHandleUndo.RegisterCreatedObject(cloneParentGo);
            cloneParentGo.name = $"{origin.name} cloneParent";

            EHandleUndo.RecordNow(cloneParentGo.transform, "Cloned");

            //Set parent for clone head
            if (originParentSo != null) originParentSo.editorDisableOnChildrenChanged = true;
            spline.editorDisableOnChildrenChanged = true;
            EHandleUndo.SetTransformParent(cloneParentGo.transform, originParent);
            spline.editorDisableOnChildrenChanged = false;
            if (originParentSo != null) originParentSo.editorDisableOnChildrenChanged = false;

            SplineObject cloneParent = cloneParentGo.GetComponent<SplineObject>();
            if (cloneParent == null) cloneParent = EHandleUndo.AddComponent<SplineObject>(cloneParentGo);

            EHandleUndo.RecordNow(cloneParent, "Cloned");
            cloneParent.Type = SplineObjectType.DEFORMATION;
            cloneParent.splinePosition = origin.splinePosition;
            cloneParent.splineRotation = Quaternion.identity;
            cloneParent.cloneAmount = origin.cloneAmount;
            cloneParent.cloneDirection = origin.cloneDirection;
            cloneParent.cloneOffset = origin.cloneOffset;
            cloneParent.cloneUseFixedAmount = origin.cloneUseFixedAmount;
            cloneParent.cloneSnapEnd = origin.cloneSnapEnd;

            EHandleUndo.RecordNow(origin, "Cloned");
            EHandleUndo.SetTransformParent(origin.transform, cloneParent.transform);

            EHandleUndo.RecordNow(cloneParent, "Cloned");
            cloneParent.MarkVersionDirty();

            // Need to be dealyed, else the ui will update with a missmatch from layout to repaint.
            EActionDelayed.Add(() => 
            {
                Selection.activeTransform = cloneParent.transform;
                EHandleSelection.ForceUpdate();
            }, 0, 0, EActionDelayed.ActionFlag.FRAMES);

            EnableUsingChildren(spline, cloneParent);
        }

        public static void Disable(SplineObject cloneParent)
        {
            DeleteClones(cloneParent, GetCurrentCloneSections(cloneParent) - 1);
            EHandleUndo.RecordNow(cloneParent);
            cloneParent.cloningEnabled = false;
        }

        public static void DisconnectClonesAndDisable(SplineObject cloneParent)
        {
            EHandleUndo.RecordNow(cloneParent);
            if(cloneParent.clones != null)
                cloneParent.clones.Clear();

            if (cloneParent.originClones != null)
                cloneParent.originClones.Clear();

            cloneParent.cloningEnabled = false;
        }
    }
}
