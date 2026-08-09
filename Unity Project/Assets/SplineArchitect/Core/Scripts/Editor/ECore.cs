// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ECore.cs
//
// Author: Mikael Danielsson
// Date Created: 28-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEditor.SceneManagement;

using SplineArchitect.PostProcessing;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public static class ECore
    {
        public static bool initalized;
        private static PlayModeStateChange playModeStateChange;
        private static EditorWindow lastFocusedEditorWindow;

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AfterAssemblyReload()
        {
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;

            EditorApplication.update += Update;
#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUI;
#else
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
#endif
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.wantsToQuit += OnEditorWantsToQuit;

            ObjectChangeEvents.changesPublished += ChangesPublished;

            SceneView.duringSceneGui += OnSceneGUI;
            SceneView.beforeSceneGui += BeforeSceneGUI;

            Undo.undoRedoEvent += OnRedoEvents;
            Selection.selectionChanged += OnSelectionChange;

            Tools.pivotRotationChanged += OnPivotRotationChanged;

            ShortcutManager.instance.shortcutBindingChanged += OnShortcutBindingChanged;

            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            PrefabUtility.prefabInstanceUpdated += OnPrefabUpdate;
            PrefabUtility.prefabInstanceReverted += OnPrefabRevert;

            EHandleUi.AfterAssemblyReload();
            EHandleSelection.AfterAssemblyReload();
        }

        // Runs for example when adding components to a GameObject.
        private static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            EHandleMeshContainer.ChangesPublished(ref stream);
        }

        private static void OnPrefabUpdate(GameObject go)
        {
            EHandlePrefab.OnPrefabUpdate(go);
        }

        private static void OnPrefabRevert(GameObject go)
        {
            EHandlePrefab.OnPrefabRevert(go);
        }

        private static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            EHandlePrefab.OnPrefabStageOpened(prefabStage);
        }

        private static void OnPrefabStageClosing(PrefabStage prefabStage)
        {
            EHandlePrefab.OnPrefabStageClosing(prefabStage);
        }

        private static void OnShortcutBindingChanged(ShortcutBindingChangedEventArgs args)
        {
            EHandleUi.OnShortcutBindingChanged();
        }

        private static void OnWindowFocusChanged()
        {
            EHandleUi.OnWindowFocusChanged();
        }

        private static bool OnEditorWantsToQuit()
        {
            EHandleUi.OnEditorWantsToQuit();
            return true;
        }

        private static void BeforeAssemblyReload()
        {
            EHandleUi.BeforeAssemblyReload();
            HandleRegistry.DisposeNativeDataOnSplines();
        }

        private static void OnPivotRotationChanged()
        {
            EHandleTool.OnPivotRotationChanged();
        }

#if UNITY_6000_4_OR_NEWER
        private static void OnHierarchyGUI(EntityId id, Rect selectionRect)
        {
            EHandleSelection.OnHierarchyGUI(id, selectionRect);
        }
#else
        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            EHandleSelection.OnHierarchyGUI(instanceID, selectionRect);
        }
#endif

        private static void OnSelectionChange()
        {
            EHandleSelection.OnSelectionChange();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            EHandleEvents.waitForEditor = true;
            playModeStateChange = state;
            EHandleEvents.playModeStateChange = state;

#if UNITY_6000_6_OR_NEWER
            EnterPlayModeOptions options = EditorSettings.enterPlayModeOptions;
            if (!options.HasFlag(EnterPlayModeOptions.DisableDomainReload) || !options.HasFlag(EnterPlayModeOptions.DisableSceneReload))
            {
                foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
                {
                    if (state == PlayModeStateChange.ExitingEditMode)
                        spline.DisposeCache();
                }
            }
#else
            foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    spline.DisposeCache();
            }
#endif
            EHandleTool.OnPlayModeStateChanged(state);
        }

        private static void BeforeSceneGUI(SceneView sceneView)
        {
            if (!EHandleSceneView.IsValid(sceneView))
                return;

            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode || playModeStateChange == PlayModeStateChange.ExitingEditMode)
                return;

            EHandleTool.BeforeSceneGUIGlobal(sceneView, Event.current);
            EHandleSpline.BeforeSceneGUIGlobal(sceneView, Event.current);
            EHandleSceneView.BeforeSceneGUIGlobal(sceneView, Event.current);
            EHandleSelection.BeforeSceneGUIGlobal(sceneView, Event.current);
            EHandleUi.BeforeSceneGUIGlobal(sceneView, Event.current);
            EHandleSegment.BeforeSceneGUI(sceneView, Event.current);
            EHandleEvents.InitAfterDrag(sceneView);
            EActionToSceneGUI.BeforeOnSceneGUI(Event.current);
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!EHandleSceneView.IsValid(sceneView))
                return;

            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode || playModeStateChange == PlayModeStateChange.ExitingEditMode)
                return;

            //Needs to be first
            EActionToSceneGUI.EArlyOnSceneGUI(Event.current);

            foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
            {
                if (spline == null)
                {
                    EHandleUndo.MarkSplineForDestroy(spline);
                    continue;
                }

                if (spline.IsEnabled() == false) continue;
                if (!spline.editorInitialized) continue;

                if (!EHandleSelection.IsPrimiarySelection(spline)) continue;

                EHandleSpline.OnSceneGUI(spline, Event.current);
                EHandleTool.OnSceneGUI(spline, Event.current, sceneView);
                EHandleSplineObject.OnSceneGUI(spline, Event.current);
                EHandlePlane.OnSceneGUI(spline, Event.current);
            }

            EHandleUi.OnSceneGUIGlobal(sceneView);
            EHandleUndo.DestroyMarkedSplines();
            EHandleDrawing.OnSceneGUIGlobal(HandleRegistry.GetSplinesUnsafe(), Event.current);

            //Needs to be last
            EActionToSceneGUI.LateOnSceneGUI(Event.current);
        }

        private static void Update()
        {
            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode || playModeStateChange == PlayModeStateChange.ExitingEditMode)
              return;

            if (!initalized)
            {
                EHandleEvents.InvokeBeforeFirstUpdate();
            }

            if(lastFocusedEditorWindow != EditorWindow.focusedWindow)
            {
                lastFocusedEditorWindow = EditorWindow.focusedWindow;
                OnWindowFocusChanged();
            }

            EHandleEvents.InvokeBeforeUpdate();
            EActionDelayed.UpdateGlobalEarly();

            //Needs to be before all splines update. Spline connectors might change segment positions and if its after "EHandleDeformation.ProcessSplineObjects(spline)"
            //changes will not be registred at all.
            EHandleSplineConnector.UpdateGlobal();

            foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
            {
                if (spline == null) continue;

                EHandleSpline.InitalizeEditor(spline, initalized);

                if (spline.IsEnabled() == false) continue;

                EHandleSegment.HandleLinking(spline);

                bool isPrimiarySelection = EHandleSelection.IsPrimiarySelection(spline);
                bool IsSecondarySelection = EHandleSelection.IsSecondarySelection(spline);
                bool forceUpdate = EHandleEvents.waitForEditor || spline.IsEditorCacheDirty() || (spline.Monitor.TransformChange() !&& Application.isPlaying) || EHandleSelection.IsConnectedToSelection(spline) || EHandleSelection.IsChildOfSelected(spline);

                if (isPrimiarySelection || IsSecondarySelection || forceUpdate)
                {
                    EHandleSpline.UpdateLinksOnTransformChange(spline);
                    EHandleDeformation.ProcessSplineObjects(spline);
                }

                if (isPrimiarySelection || IsSecondarySelection || forceUpdate || spline.jobState == JobState.EDITOR)
                {
                    EHandleDeformation.ProcessJobs(spline, true);
                }

                if (isPrimiarySelection)
                {
                    EHandleSplineObject.Update(spline);
                }
            }

            EHandlePrefab.UpdateGlobal();
            EHandleMeshContainer.RefreshAfterAssetImport(HandleRegistry.GetSplinesUnsafe());
            BuildProcessReport.UpdateGlobal();
            EHandleSpline.ProcessMarkedForInfoUpdates();
            EHandleUi.UpdateGlobal();
            HandleRegistry.UpdateGlobal();
            EHandleWarningWindow.UpdateGlobal();
            AssetModificationDetection.UpdateGlobal();
            EHandleUi.Init();

            //Second last
            EActionDelayed.UpdateGlobalLate();
            
            //Last
            foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
            {
                if (spline == null) continue;
                if (spline.IsEnabled() == false) continue;
                spline.Monitor.EditorUpdateTransform();
            }

            initalized = true;
            EHandleEvents.waitForEditor = false;
            EHandleEvents.undoActive = false;
        }

        private static void OnRedoEvents(in UndoRedoInfo undo)
        {
            EHandleEvents.undoActive = true;
            EHandleEvents.undoWasRedo = undo.isRedo;

            EHandleSelection.OnUndo();

            foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
            {
                if (spline == null) continue;

                spline.Monitor.EditorUpdateChildCount();

                for (int i = 0; i > spline.segments.Count; i++)
                {
                    Segment s = spline.segments[i];
                    s.indexInSpline = i;
                    s.splineParent = spline;
                    s.localSpace = spline.transform;
                }

                if (spline.IsEnabled() == false) continue;
                if (spline.segments.Count < 2) continue;

                bool isPrimiarySelection = EHandleSelection.IsPrimiarySelection(spline);
                bool IsSecondarySelection = EHandleSelection.IsSecondarySelection(spline);

                if (!IsSecondarySelection && !isPrimiarySelection) continue;

                EHandleSpline.MarkForInfoUpdate(spline);
            }

            EHandleTool.OnUndoGlobal();

            //Needs to be last
            EHandleUndo.UpdateUndoTriggerTime();
        }

        public static PlayModeStateChange GetLastPlayMode()
        {
            return playModeStateChange;
        }
    }
}
