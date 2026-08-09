// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarToggleInfo.cs
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
    public class ToolbarToggleInfo : ToolbarToggleBase
    {
        public const string ID = "SplineArchitect_toolbarButtonInfo";

        public ToolbarToggleInfo()
        {
            bool isDark = EditorGUIUtility.isProSkin;
            icon = isDark ? LibraryTexture.iconMenuInfo : LibraryTexture.iconMenuInfoLight;
            tooltip = "Info";
        }

        public override void CloseOrOpenWindow(bool open, bool focus = false)
        {
            if (open)
            {
                EHandleFolder.UpdateTempFolderSize();
                EHandleFolder.UpdateHarddiskSpaceLeft();
                ShowWindow<WindowInfo>();
            }
            else CloseWindow();
        }

        protected override void StyleSettings(bool isHorizontal)
        {
            base.StyleSettings(isHorizontal);

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
    }
}
