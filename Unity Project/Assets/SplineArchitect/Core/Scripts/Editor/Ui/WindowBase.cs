// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: WindowBase.cs
//
// Author: Mikael Danielsson
// Date Created: 05-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;


using UnityEditor;
using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Ui
{
    public abstract class WindowBase : EditorWindow
    {
        public const float headerHeight = 19;
        public const float toolbarHeight = 24;
        public const float itemHeight = 22;
        public const float sectionHeight = 16;
        public const float bottomHeight = 20;

        public static List<WindowBase> instances = new List<WindowBase>();

        [NonSerialized] public ToolbarToggleBase toolbarToggleBase;
        [NonSerialized] public ToolbarDropdownTogglePlane toolbarDropdownTogglePlane;
        public WindowBase extendedWindow;
        public Rect cachedRect = new Rect(0, 0, 0, 0);
        public Vector2 startDragPos;

        private bool externalClose = true;
        private bool skipFirstOnGUI = true;
        private bool shouldRemoveFocus = true;

        private void OnEnable()
        {
            instances.Add(this);
            minSize = new Vector2(22, 22);

            System.Type hostViewType = typeof(Editor).Assembly.GetType("UnityEditor.HostView");
            if (hostViewType == null)
            {
                Debug.LogWarning("[Spline Architect] UnityEditor.HostView not found. Internal Unity API may have changed.");
                return;
            }

            FieldInfo fieldInfo = hostViewType.GetField("k_DockedMinSize", BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo == null)
            {
                Debug.LogWarning("[Spline Architect] UnityEditor.HostView.k_DockedMinSize not found. Unity may have changed internal APIs.");
                return;
            }
            fieldInfo.SetValue(null, new Vector2(22, 22));
        }

        private void OnDestroy()
        {
            WindowExtended we = this as WindowExtended;

            if (we != null && externalClose)
            {
                Spline spline = EHandleSelection.selectedSpline;
                if (spline != null)
                {
                    EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
                    {
                        undoState.selectedControlPoints.Clear();
                        undoState.selectedControlPoint = 0;
                    }, "Selected spline");

                    Selection.objects = null;
                    Selection.activeTransform = spline.transform;
                }
            }
            else
            {
                instances.Remove(this);
                if (extendedWindow != null) extendedWindow.CloseWindow();
                if (toolbarToggleBase != null && we == null) toolbarToggleBase.SetValueWithoutNotify(false);
            }

            externalClose = true;
        }

        private void OnFocus()
        {
            shouldRemoveFocus = true;
            Repaint();
        }

        private void OnLostFocus()
        {
            shouldRemoveFocus = true;
        }

        private void OnGUI()
        {
            if (!EHandleUi.initialized)
                return;

            Initalize();
            ProcessFocusControl();

            bool floatingWindow = EGlobalSettings.GetIsWindowsFloating();

#if !UNITY_EDITOR_WINDOWS
            ProcessWindowSize();
#endif

            GUILayout.BeginHorizontal();

            EUiUtility.CreateVerticalGreyLine80();

            GUILayout.BeginVertical();
            EUiUtility.CreateHorizontalGreyLine80();
            OnGUIExtended();
            EUiUtility.CreateHorizontalGreyLine80();
            GUILayout.EndVertical();

            EUiUtility.CreateVerticalGreyLine80();

            GUILayout.EndHorizontal();

#if UNITY_EDITOR_WINDOWS
            ProcessWindowSize();
#endif

            void Initalize()
            {
                Event e = Event.current;
                if (e.type == EventType.Repaint)
                {
                    //Need to skip first SceneGUI and set the size of the window else during the first frame the window will look weird.
                    if (skipFirstOnGUI)
                    {
                        skipFirstOnGUI = false;

                        UpdateWindowSize();

                        if (EGlobalSettings.GetIsWindowsFloating())
                            position = new Rect(position.x, position.y, cachedRect.width, cachedRect.height);
                        else
                            position = cachedRect;

                        HandleExtendedWindow();
                        return;
                    }
                }
            }

            void ProcessFocusControl()
            {
                if (shouldRemoveFocus)
                {
                    GUI.FocusControl(null);
                    shouldRemoveFocus = false;
                }
            }

            void ProcessWindowSize()
            {
                if (!GeneralUtility.IsEqual(position, cachedRect))
                {
                    if (EGlobalSettings.GetIsWindowsFloating())
                        position = new Rect(position.x, position.y, cachedRect.width, cachedRect.height);
                    else
                        position = cachedRect;

                    Repaint();

                    if (extendedWindow != null)
                    {
#if UNITY_EDITOR_WINDOWS
                        if (!EGlobalSettings.GetIsWindowsFloating()) extendedWindow.position = extendedWindow.cachedRect;
#endif
                        extendedWindow.Repaint();
                    }
                }

                UpdateWindowSize();
                HandleExtendedWindow();
            }
        }

        protected abstract void OnGUIExtended();

        protected abstract void UpdateWindowSize();

        protected virtual void HandleExtendedWindow() { }

        public void SetToolbarButtonValue(bool value)
        {
            if (toolbarToggleBase != null)
                toolbarToggleBase.SetValueWithoutNotify(value);
        }

        public static void RepaintAll()
        {
            foreach (WindowBase item in instances)
            {
                if (item != null)
                {
                    item.Repaint();
                }

                EActionDelayed.Add(() =>
                {
                    foreach (WindowBase wb in WindowBase.instances)
                    {
                        WindowSpline sp = wb as WindowSpline;

                        if (sp != null)
                            sp.Repaint();
                    }
                }, 0, 0, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE);
            }
        }

        public void UpdateChacedPosition(Vector2 screenPos)
        {
            if (EGlobalSettings.GetIsWindowsFloating())
                return;

            cachedRect.x = screenPos.x;
            cachedRect.y = screenPos.y;
        }

        public void OpenWindow(Vector2 position, bool focused)
        {
            OpenWindow(focused);
            UpdateChacedPosition(position);
        }

        public void OpenWindow(bool focused)
        {
            EditorWindow lastFocus = EditorWindow.focusedWindow;

            if (EGlobalSettings.GetIsWindowsFloating()) ShowUtility();
            else ShowPopup();

            if (lastFocus != null && !focused)
            {
                lastFocus.Focus();
            }
            else if (focused)
            {
                Focus();
            }

        }

        public void CloseWindow()
        {
            externalClose = false;
            Close();
        }

        public static void CloseAll()
        {
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                WindowBase item = instances[i];

                if (item == null)
                    continue;

                item.SetToolbarButtonValue(false);
                item.CloseWindow();
            }
        }
    }
}
