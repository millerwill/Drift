// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarButtonGrid.cs
//
// Author: Mikael Danielsson
// Date Created: 17-08-2025
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
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class ToolbarDropdownTogglePlane : EditorToolbarDropdownToggle
    {
        public const string ID = "SplineArchitect_toolbarButtonPlane";
        public static List<ToolbarDropdownTogglePlane> instances = new List<ToolbarDropdownTogglePlane>();

        private WindowBase windowBase;
        public bool pointerHovering;

        public ToolbarDropdownTogglePlane()
        {
            PlaneType planeType = EGlobalSettings.GetPlaneType();
            SetIcons(planeType);

            dropdownClicked += ToggleMenu;
            this.RegisterValueChangedCallback(OnValueChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            SetValueWithoutNotify(EGlobalSettings.GetPlaneVisibility());
            RegisterCallback<PointerEnterEvent>(OnPointerEntered);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

            Image img = this.Q<UnityEngine.UIElements.Image>();
            if (img != null)
            {
                img.style.width = 13;
                img.style.height = 13;
                img.scaleMode = ScaleMode.StretchToFill;
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

        private void OnAttach(AttachToPanelEvent evt)
        {
            if (!instances.Contains(this))
                instances.Add(this);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            CloseOrOpenWindow(false);
            instances.Remove(this);
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            EGlobalSettings.SetPlaneVisibility(evt.newValue);

            foreach(ToolbarDropdownTogglePlane tdtg in instances)
            {
                tdtg.SetValueWithoutNotify(EGlobalSettings.GetPlaneVisibility());
            }

            CloseWindow();
            WindowBase.RepaintAll();
        }

        private void ToggleMenu()
        {
            CloseOrOpenWindow(windowBase == null);
        }

        private void CloseOrOpenWindow(bool open)
        {
            if (open) ShowWindow<WindowPlane>();
            else CloseWindow();
        }

        private void ShowWindow<T>() where T : WindowBase
        {

            if (!EHandleUi.initialized)
            {
                SetValueWithoutNotify(false);
                return;
            }

            for(int i = WindowBase.instances.Count - 1; i >= 0; i--)
            {
                WindowPlane wg = WindowBase.instances[i] as WindowPlane;
                if (wg != null)
                {
                    wg.CloseWindow();
                }
            }

            Vector2 windowPos = EUiUtility.GetWindowAnchorPosition(SceneView.lastActiveSceneView, worldBound, parent.resolvedStyle.flexDirection == FlexDirection.Row);

            windowBase = ScriptableObject.CreateInstance<T>();
            windowBase.OpenWindow(windowPos, true);
            windowBase.toolbarDropdownTogglePlane = this;
        }

        private void CloseWindow()
        {
            if (windowBase != null)
            {
                windowBase.toolbarDropdownTogglePlane = null;
                windowBase.CloseWindow();
                windowBase = null;
            }
        }

        internal void SetIcons(PlaneType planeType)
        {
            if(planeType == PlaneType.GRID)
            {
                tooltip = "Toggle grid";
                icon = EditorGUIUtility.isProSkin ? LibraryTexture.iconGrid : LibraryTexture.iconGridLight;
            }
            else
            {
                tooltip = "Toggle plane";
                icon = EditorGUIUtility.isProSkin ? LibraryTexture.iconPlane : LibraryTexture.iconPlaneLight;
            }
        }
    }
}
