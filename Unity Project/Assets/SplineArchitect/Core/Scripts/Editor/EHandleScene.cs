// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleScene.cs
//
// Author: Mikael Danielsson
// Date Created: 17-01-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

using SplineArchitect.Utility;

namespace SplineArchitect
{
    public class EHandleScene
    {
        public static bool sceneIsClosing { get; private set; }
        public static bool editorIsQuitting { get; private set; }
        public static bool isSaving { get; private set; }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AfterAssemblyReload()
        {
            //Callbacks
            //EditorSceneManager.sceneDirtied += OnSceneDirtied;
            //EditorSceneManager.sceneOpening += OnSceneOpening;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EditorApplication.quitting += OnEditorIsQuitting;
        }

        private static void OnEditorIsQuitting()
        {
            editorIsQuitting = true;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ECore.initalized = false;
            EHandleEvents.waitForEditor = true;
        }

        private static void OnSceneClosed(Scene scene)
        {
            if(EHandleEvents.playModeStateChange != PlayModeStateChange.ExitingPlayMode &&
               EHandleEvents.playModeStateChange != PlayModeStateChange.ExitingEditMode)
                HandleCachedResources.ClearScene(scene);
            EHandleEvents.sceneIsClosing = false;
            sceneIsClosing = false;
        }

        private static void OnSceneClosing(Scene scene, bool removingScene)
        {
            EHandleEvents.sceneIsClosing = true;
            sceneIsClosing = true;
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            isSaving = true;

            foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
            {
                if (spline == null)
                    continue;

                if (spline.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                    spline.hideFlags = HideFlags.DontSaveInBuild;
                else
                    spline.hideFlags = HideFlags.None;

                //SplineObject HideFlags
                for (int i2 = 0; i2 < spline.AllSplineObjectCount; i2++)
                {
                    SplineObject so = spline.GetSplineObjectAtIndex(i2);

                    if (so == null)
                        continue;

                    //Meshes HideFlags
                    if (so.Type == SplineObjectType.DEFORMATION)
                    {
                        if (so.meshMode == MeshMode.SAVE_IN_SCENE)
                        {
                            for (int i = 0; i < so.MeshContainerCount; i++)
                            {
                                UpdateHideFlagsMeshContainer(so.GetMeshContainerAtIndex(i), HideFlags.None);
                            }
                        }
                        else if (so.meshMode == MeshMode.SAVE_IN_BUILD)
                        {
                            for (int i = 0; i < so.MeshContainerCount; i++)
                            {
                                UpdateHideFlagsMeshContainer(so.GetMeshContainerAtIndex(i), HideFlags.DontSaveInEditor);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < so.MeshContainerCount; i++)
                            {
                                UpdateHideFlagsMeshContainer(so.GetMeshContainerAtIndex(i), HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor);
                            }
                        }
                    }

                    so.hideFlags = HideFlags.DontSaveInBuild;

                    if (so.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                        continue;

                    so.hideFlags = HideFlags.None;
                }
            }

            //SplineConnectors HideFlags
            foreach (SplineConnector sc in HandleRegistry.GetSplineConnectorsUnsafe())
            {
                if (sc == null) continue;

                sc.hideFlags = HideFlags.DontSaveInBuild;

                for (int i = 0; i < sc.ConnectionCount; i++)
                {
                    Segment s = sc.GetConnectionAtIndex(i);

                    if (s.SplineParent.componentMode == ComponentMode.ACTIVE)
                    {
                        sc.hideFlags = HideFlags.None;
                        break;
                    }
                }
            }
        }

        private static void OnSceneSaved(Scene scene)
        {
            EActionDelayed.Add(() =>
            {
                isSaving = false;
            }, 0, 8, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE);
        }

        private static void UpdateHideFlagsMeshContainer(MeshContainer mc, HideFlags hideFlags)
        {
            Mesh instanceMesh = mc.GetInstanceMesh();
            Mesh originMesh = mc.GetOriginMesh();

            if (originMesh == null || instanceMesh == null)
                return;

            if (originMesh == instanceMesh)
                return;

            instanceMesh.hideFlags = hideFlags;
        }

        ////Playmode.

        private static void OnSceneUnloaded(Scene scene)
        {

        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            ECore.initalized = false;
            EHandleEvents.waitForEditor = true;
        }
    }
}
