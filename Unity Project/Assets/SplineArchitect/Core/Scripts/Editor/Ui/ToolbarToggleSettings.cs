// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarToggleSettings.cs
//
// Author: Mikael Danielsson
// Date Created: 17-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEditor.Toolbars;

using SplineArchitect.Libraries;

namespace SplineArchitect.Ui
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class ToolbarToggleSettings : ToolbarToggleBase
    {
        public const string ID = "SplineArchitect_toolbarButtonSettings";

        public ToolbarToggleSettings()
        {
            bool isDark = EditorGUIUtility.isProSkin;
            icon = isDark ? LibraryTexture.iconMenuSettings : LibraryTexture.iconMenuSettingsLight;
            tooltip = "Settings";
        }

        public override void CloseOrOpenWindow(bool open, bool focus = false)
        {
            if (open) ShowWindow<WindowSettings>(focus);
            else CloseWindow();
        }

        protected override void StyleSettings(bool isHorizontal)
        {
            base.StyleSettings(isHorizontal);

            if (isHorizontal)
            {
                style.marginRight = 0;
                style.marginLeft = 0;
                style.borderTopRightRadius = 0;
                style.borderBottomRightRadius = 0;
                style.borderTopLeftRadius = 0;
                style.borderBottomLeftRadius = 0;
            }
            else
            {
                style.marginBottom = 0;
                style.marginTop = 0;
                style.borderTopRightRadius = 0;
                style.borderBottomRightRadius = 0;
                style.borderTopLeftRadius = 0;
                style.borderBottomLeftRadius = 0;
            }
        }
    }
}
