// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarToggleBase.cs
//
// Author: Mikael Danielsson
// Date Created: 05-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

using SplineArchitect.Libraries;
using SplineArchitect.Utility;

namespace SplineArchitect.Ui
{
    public abstract class ToolbarToggleBase : EditorToolbarToggle
    {
        public static List<ToolbarToggleBase> instances = new List<ToolbarToggleBase>();

        private int framesWithoutSceneViewChange = 0;
        private Rect oldSceneViewRect;
        private Vector2 oldWindowPos;
        public SceneView currentSceneView { get; private set; }
        protected WindowBase windowBase;
        public bool pointerHovering;

        public ToolbarToggleBase()
        {
            this.RegisterValueChangedCallback(OnValueChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<PointerEnterEvent>(OnPointerEntered);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

            EActionDelayed.Add(() =>
            {
                SyncCurrentSceneView();
            }, 0, 5, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE);
        }

        private void SyncCurrentSceneView()
        {
            foreach (SceneView sv in SceneView.sceneViews)
            {
                if (sv.rootVisualElement.panel == panel)
                {
                    currentSceneView = sv;
                    break;
                }
            }
        }

        private void OnPointerEntered(PointerEnterEvent evt)
        {
            pointerHovering = true;
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            pointerHovering = false;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            StyleSettings(parent.resolvedStyle.flexDirection == FlexDirection.Row);
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            if (currentSceneView == null)
                return;

            CloseOrOpenWindow(evt.newValue);
        }

        protected virtual void OnAttach(AttachToPanelEvent evt)
        {
            CloseOrOpenWindow(value);

            if (instances.Contains(this))
                return;

            instances.Add(this);
        }

        protected virtual void OnDetach(DetachFromPanelEvent evt)
        {
            CloseOrOpenWindow(false);
            instances.Remove(this);
        }

        protected void ShowWindow<T>(bool focus = false) where T : WindowBase
        {
            if (!EHandleUi.initialized)
            {
                SetValueWithoutNotify(false);
                return;
            }

            SyncCurrentSceneView();

            Vector2 windowPos = EUiUtility.GetWindowAnchorPosition(currentSceneView, worldBound, parent.resolvedStyle.flexDirection == FlexDirection.Row);
            oldWindowPos = windowPos;
            WindowBase.CloseAll();

            windowBase = ScriptableObject.CreateInstance<T>();
            windowBase.OpenWindow(windowPos, focus);
            windowBase.toolbarToggleBase = this;
        }

        protected void CloseWindow()
        {
            if (windowBase != null)
            {
                if(windowBase.extendedWindow != null)
                {
                    windowBase.extendedWindow.CloseWindow();
                    windowBase.extendedWindow = null;
                }

                windowBase.toolbarToggleBase = null;
                windowBase.CloseWindow();
                windowBase = null;
            }
        }

        public abstract void CloseOrOpenWindow(bool open, bool focus = false);

        protected virtual void StyleSettings(bool isHorizontal)
        {
            //The icon
            Image img = this.Q<UnityEngine.UIElements.Image>();

            if (img != null)
            {
                img.style.width = 24;
                img.style.height = 17;
                img.scaleMode = ScaleMode.StretchToFill;
            }
        }

        //Runs in the editor update loop
        public void SyncWindowPosition()
        {
            Vector2 windowPos = EUiUtility.GetWindowAnchorPosition(currentSceneView, worldBound, parent.resolvedStyle.flexDirection == FlexDirection.Row);

            //While resizing the sceneView unity has a bug that will give us the wrong position for the sceneView. We can set the window position when that happens.
            //We also nees to wait a comple of frames after that happens.
            if (!GeneralUtility.IsEqual(oldSceneViewRect, currentSceneView.position))
            {
                oldSceneViewRect = currentSceneView.position;
                framesWithoutSceneViewChange = 0;
                return;
            }

            framesWithoutSceneViewChange++;

            //Seems to work okey with 4 (3 dont work at all) but we have 10 for good measures.
            if (framesWithoutSceneViewChange < 10)
                return;

            if (!GeneralUtility.IsEqual(windowPos, oldWindowPos))
            {
                oldWindowPos = windowPos;

                windowBase.cachedRect.position = windowPos;
                windowBase.Repaint();
            }
        }
    }
}
