// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarDropdownHandleType.cs
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

namespace SplineArchitect.Ui
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class ToolbarDropdownHandleType : EditorToolbarDropdown
    {
        public const string ID = "SplineArchitect_toolbarDropdownHandleType";
        public static List<ToolbarDropdownHandleType> instances = new List<ToolbarDropdownHandleType>();

        public ToolbarDropdownHandleType()
        {
            ControlHandleType handleType = EGlobalSettings.GetHandleType();
            SyncVisualData(handleType);
            clicked += ShowMenu;

            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            if (!instances.Contains(this))
                instances.Add(this);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            instances.Remove(this);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (parent.resolvedStyle.flexDirection == FlexDirection.Row)
                style.width = 48;
            else
                style.width = 36;
        }

        void ShowMenu()
        {
            ControlHandleType handleType = EGlobalSettings.GetHandleType();

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Mirrored"), handleType == ControlHandleType.MIRRORED, () =>
            {
                EGlobalSettings.SetHandleType(ControlHandleType.MIRRORED);
                foreach (ToolbarDropdownHandleType tdht in instances) tdht.SyncVisualData(ControlHandleType.MIRRORED);
            });
            menu.AddItem(new GUIContent("Continuous"), handleType == ControlHandleType.CONTINUOUS, () =>
            {
                EGlobalSettings.SetHandleType(ControlHandleType.CONTINUOUS);
                foreach (ToolbarDropdownHandleType tdht in instances) tdht.SyncVisualData(ControlHandleType.CONTINUOUS);
            });
            menu.AddItem(new GUIContent("Broken"), handleType == ControlHandleType.BROKEN, () =>
            {
                EGlobalSettings.SetHandleType(ControlHandleType.BROKEN);
                foreach (ToolbarDropdownHandleType tdht in instances) tdht.SyncVisualData(ControlHandleType.BROKEN);
            });
            menu.AddItem(new GUIContent("Auto"), handleType == ControlHandleType.AUTO, () =>
            {
                EGlobalSettings.SetHandleType(ControlHandleType.AUTO);
                foreach (ToolbarDropdownHandleType tdht in instances) tdht.SyncVisualData(ControlHandleType.AUTO);
            });

            Rect r = worldBound;
            Vector2 screenPos = GUIUtility.GUIToScreenPoint(new Vector2(worldBound.x, worldBound.y));
            menu.DropDown(new Rect(worldBound.position, worldBound.size));
        }

        private void SyncVisualData(ControlHandleType handleType)
        {
            bool isDark = EditorGUIUtility.isProSkin;

            //Tooltip
            if (handleType == ControlHandleType.MIRRORED)          tooltip = "Handle type - Mirrored";
            else if (handleType == ControlHandleType.CONTINUOUS)   tooltip = "Handle type - Continuous";
            else if (handleType == ControlHandleType.BROKEN)       tooltip = "Handle type - Broken";
            else if (handleType == ControlHandleType.AUTO)         tooltip = "Handle type - Auto";

            //Icon
            if (handleType == ControlHandleType.MIRRORED && isDark)         icon = LibraryTexture.iconHandleMirrored;
            else if (handleType == ControlHandleType.MIRRORED && !isDark)   icon = LibraryTexture.iconHandleMirroredLight;
            else if (handleType == ControlHandleType.CONTINUOUS && isDark)  icon = LibraryTexture.iconHandleContinuous;
            else if (handleType == ControlHandleType.CONTINUOUS && !isDark) icon = LibraryTexture.iconHandleContinuousLight;
            else if (handleType == ControlHandleType.BROKEN && isDark)      icon = LibraryTexture.iconHandleBroken;
            else if (handleType == ControlHandleType.BROKEN && !isDark)     icon = LibraryTexture.iconHandleBrokenLight;
            else if (handleType == ControlHandleType.AUTO && isDark)        icon = LibraryTexture.iconHandleAuto;
            else if (handleType == ControlHandleType.AUTO && !isDark)       icon = LibraryTexture.iconHandleAutoLight;


            Image img = this.Q<UnityEngine.UIElements.Image>();

            if (img != null)
            {
                img.style.width = 28;
                img.style.height = 14;
                img.scaleMode = ScaleMode.StretchToFill;
            }
        }
    }
}
