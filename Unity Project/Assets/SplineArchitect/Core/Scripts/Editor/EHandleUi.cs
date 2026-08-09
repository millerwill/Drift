// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleUi.cs
//
// Author: Mikael Danielsson
// Date Created: 16-02-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEditor.ShortcutManagement;
using UnityEditor;
using UnityEngine;

using SplineArchitect.CustomTools;
using SplineArchitect.Libraries;
using SplineArchitect.Ui;
using SplineArchitect.Utility;
using SplineArchitect.ScriptableObjects;

namespace SplineArchitect
{
    internal class EHandleUi
    {
        internal static bool initialized { get; private set; }
        private static int frameCounter;

        //Options
        internal static string[] optionsEasing;
        internal static string[] optionsNoiseType;
        internal static string[] optionsNoiseGroups = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        internal static string[] optionsNoiseGroupsAndNone = new string[] { "None", "A", "B", "C", "D", "E", "F", "G", "H" };
        internal static string[] optionsSpace = new string[] { "World", "Local" };

        private static MenuUndoState menuUndoState;

        internal static string ActiveSplineMenu
        {
            get
            {
                if (menuUndoState == null) return "";
                return menuUndoState.activeSplineMenu;
            }
        }

        internal static string ActiveAnchorMenu
        {
            get
            {
                if (menuUndoState == null) return "";
                return menuUndoState.activeAnchorMenu;
            }
        }

        internal static string ActiveSplineObjectMenu
        {
            get
            {
                if (menuUndoState == null) return "";
                return menuUndoState.activeSplineObjectMenu;
            }
        }

        internal static bool SchedulingMinimized
        {
            get
            {
                if (menuUndoState == null) return true;
                return menuUndoState.schedulingMinimized;
            }
        }

        internal static bool RenderingMinimized
        {
            get
            {
                if (menuUndoState == null) return true;
                return menuUndoState.renderingMinimized;
            }
        }

        internal static bool SnapSettingsMinimized
        {
            get
            {
                if (menuUndoState == null) return true;
                return menuUndoState.snapSettingsMinimized;
            }
        }

        internal static bool MeshSettingsMinimized
        {
            get
            {
                if (menuUndoState == null) return true;
                return menuUndoState.meshSettingsMinimized;
            }
        }

        internal static bool AdvancedSettingsMinimized
        {
            get
            {
                if (menuUndoState == null) return true;
                return menuUndoState.advancedSettingsMinimized;
            }
        }
        internal static bool LinksMinimized
        {
            get
            {
                if (menuUndoState == null) return true;
                return menuUndoState.linksMinimized;
            }
        }
        internal static int SelectedNoiseLayerIndex
        {
            get
            {
                if (menuUndoState == null) return 0;
                return menuUndoState.selectedNoiseLayerIndex;
            }
        }

        internal static void Init()
        {
            if (!initialized)
            {
                initialized = true;

                LibraryTexture.Init();
                LibraryGUIContent.Init();

                EActionToSceneGUI.Add(() => 
                {
                    LibraryGUIStyle.Init();
                    CreateEasingList();
                    CreateNoiseTypeList();
                }, EActionToSceneGUI.Type.LATE, EventType.Layout);

                int preloadedTextures = LibraryTexture.GetPreloadedTextureCount();
                if (preloadedTextures > 0)
                    Debug.LogWarning($"[Spline Architect] {preloadedTextures} textures have been preloaded. This should only happen during the first time you import Spline Architect into a project.");
            }
        }

        internal static void AfterAssemblyReload()
        {
            menuUndoState = ScriptableObject.CreateInstance<MenuUndoState>();
            menuUndoState.hideFlags = HideFlags.HideAndDontSave;
        }

        internal static void UpdateGlobal()
        {
            if (EGlobalSettings.GetIsWindowsFloating())
                return;

            for (int i = WindowBase.instances.Count - 1; i >= 0; i--)
            {
                WindowBase item = WindowBase.instances[i];
                if (item.toolbarToggleBase != null)
                    item.toolbarToggleBase.SyncWindowPosition();
            }

            UpdateInfoWindow();
        }

        internal static void OnSceneGUIGlobal(SceneView sceneView)
        {
            Event e = Event.current;

            //Update when using position tool
            if (PositionTool.activePart != PositionTool.ActivePart.NONE)
                WindowBase.RepaintAll();

            if (EHandleSpline.controlPointCreationActive && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                Tools.current = Tool.Move;
                e.Use();
            }
        }

        internal static void BeforeSceneGUIGlobal(SceneView sceneView, Event e)
        {
            if (e.type == EventType.MouseUp)
            {
                WindowBase.RepaintAll();
            }
        }

        internal static void BeforeAssemblyReload()
        {
            LibraryTexture.DestroyPreloadedTextures();
        }

        internal static void OnEditorWantsToQuit()
        {
            SecureCloseAllWindows();
            LibraryTexture.DestroyPreloadedTextures();
        }

        internal static void OnShortcutBindingChanged()
        {
            foreach (ToolbarToggleBase tcp in ToolbarToggleBase.instances)
            {
                ToolbarToggleControlPanel ttcp = tcp as ToolbarToggleControlPanel;

                if (ttcp == null)
                    continue;

                tcp.tooltip = $"Control panel ({ShortcutManager.instance.GetShortcutBinding(EHandleShortcuts.hideUiId)})";
            }
        }

        internal static void OnWindowFocusChanged()
        {
            if (EditorWindow.focusedWindow == null)
                return;

            if (EGlobalSettings.GetIsWindowsFloating())
                return;

            for (int i = WindowBase.instances.Count - 1; i >= 0; i--)
            {
                WindowBase item = WindowBase.instances[i];

                if (item == null)
                    continue;

                if (EditorWindow.focusedWindow == item)
                    continue;

                if (EditorWindow.focusedWindow.titleContent.text == "Color")
                    continue;

                if (item.toolbarDropdownTogglePlane != null)
                {
                    if (item.toolbarDropdownTogglePlane.pointerHovering)
                        continue;

                    EActionDelayed.Add(() =>
                    {
                        item.CloseWindow();
                    }, 0, 5, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE);
                }
            }
        }

        internal static void SecureCloseAllWindows()
        {
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            if (windows.Length > 0)
            {
                for (int i = 0; i < windows.Length; i++)
                {
                    if (windows[i] == null)
                        continue;

                    WindowBase windowBase = windows[i] as WindowBase;
                    if (windowBase != null)
                    {
                        windowBase.CloseWindow();
                    }
                }
            }
        }

        internal static void UpdateMenuStateRecordUndo(Action<MenuUndoState> onUpdate, string recordName = null, EHandleUndo.RecordType recordType = EHandleUndo.RecordType.RECORD_OBJECT)
        {
            EHandleUndo.RecordNow(menuUndoState, recordName, recordType);
            onUpdate.Invoke(menuUndoState);
        }

        internal static void UpdateMenuState(Action<MenuUndoState> onUpdate)
        {
            onUpdate.Invoke(menuUndoState);
        }

        private static void UpdateInfoWindow()
        {
            frameCounter++;

            if (frameCounter < 8)
                return;

            frameCounter = 0;

            if (WindowBase.instances == null || WindowBase.instances.Count != 1)
                return;

            WindowInfo windowInfo = WindowBase.instances[0] as WindowInfo;

            if (windowInfo == null)
                return;

            windowInfo.Repaint();
        }

        private static void CreateEasingList()
        {
            optionsEasing = Enum.GetNames(typeof(Easing));

            for (int y = 0; y < optionsEasing.Length; y++)
            {
                optionsEasing[y] = EConversionUtility.CapitalizeString(optionsEasing[y]);
            }
        }

        private static void CreateNoiseTypeList()
        {
            optionsNoiseType = Enum.GetNames(typeof(NoiseType));

            for (int y = 0; y < optionsNoiseType.Length; y++)
            {
                optionsNoiseType[y] = EConversionUtility.CapitalizeString(optionsNoiseType[y]);
            }
        }
    }
}
