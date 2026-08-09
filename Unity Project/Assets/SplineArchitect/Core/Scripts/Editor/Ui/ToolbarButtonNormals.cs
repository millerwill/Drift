// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarButtonNormals.cs
//
// Author: Mikael Danielsson
// Date Created: 17-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

using SplineArchitect.Libraries;

namespace SplineArchitect.Ui
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class ToolbarButtonNormals : ToolbarButtonBase
    {
        public const string ID = "SplineArchitect_toolbarButtonNormals";

        public ToolbarButtonNormals()
        {
            tooltip = "Toggle normals";

            SyncVisualData(EGlobalSettings.GetShowNormals());
        }

        protected override void ToggleMenu()
        {
            bool value = !EGlobalSettings.GetShowNormals();
            EGlobalSettings.SetShowNormals(value);

            foreach (ToolbarButtonBase tbb in instances)
            {
                ToolbarButtonNormals tbn = tbb as ToolbarButtonNormals;

                if (tbn == null)
                    continue;

                tbn.SyncVisualData(value);
            }
        }

        protected override void OnGeometryChangedExtended(bool isHorizontal)
        {
            base.OnGeometryChangedExtended(isHorizontal);

            if (isHorizontal)
            {
                style.marginLeft = 1;
                style.borderTopLeftRadius = 0;
                style.borderBottomLeftRadius = 0;
            }
            else
            {
                style.marginTop = 1;
                style.borderTopLeftRadius = 0;
                style.borderTopRightRadius = 0;
            }
        }

        protected override void OnPointerLeaveEnter()
        {
            SyncVisualData(EGlobalSettings.GetShowNormals());
        }

        private void SyncVisualData(bool isOn)
        {
            bool isDark = EditorGUIUtility.isProSkin;

            //Icon
            icon = isDark ? LibraryTexture.iconNormals : LibraryTexture.iconNormalsLight;

            //Background color
            if (isOn && isDark)         style.backgroundColor = new StyleColor(new Color32(70, 96, 124, 255));
            else if(isOn && !isDark)    style.backgroundColor = new StyleColor(new Color32(150, 195, 251, 255));
            else if (!isOn && pointerHovering && isDark) style.backgroundColor = new StyleColor(new Color32(103, 103, 103, 255));
            else if (!isOn && pointerHovering && !isDark) style.backgroundColor = new StyleColor(new Color32(236, 236, 236, 255));
            else if (!isOn && !pointerHovering && isDark)   style.backgroundColor = new StyleColor(new Color32(88, 88, 88, 255));
            else if (!isOn && !pointerHovering && !isDark)  style.backgroundColor = new StyleColor(new Color32(228, 228, 228, 255));
        }
    }
}
