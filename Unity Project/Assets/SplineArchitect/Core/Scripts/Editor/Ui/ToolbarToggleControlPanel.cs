// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarToggleControlPanel.cs
//
// Author: Mikael Danielsson
// Date Created: 17-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

using SplineArchitect.Libraries;

namespace SplineArchitect.Ui
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class ToolbarToggleControlPanel : ToolbarToggleBase
    {
        public const string ID = "SplineArchitect_toolbarButtonControlPanel";

        public ToolbarToggleControlPanel()
        {
            icon = EditorGUIUtility.isProSkin ? LibraryTexture.iconMenuControlPanel : LibraryTexture.iconMenuControlPanelLight;
            tooltip = $"Control panel ({ShortcutManager.instance.GetShortcutBinding(EHandleShortcuts.hideUiId)})";
        }

        protected override void StyleSettings(bool isHorizontal)
        {
            base.StyleSettings(isHorizontal);

            if (isHorizontal)
            {
                style.marginRight = 1;
                style.borderTopRightRadius = 0;
                style.borderBottomRightRadius = 0;
            }
            else
            {
                style.marginBottom = 1;
                style.borderBottomLeftRadius = 0;
                style.borderBottomRightRadius = 0;
            }
        }

        public override void CloseOrOpenWindow(bool open, bool focus = false)
        {
            if (open) ShowWindow<WindowSpline>();
            else CloseWindow();
        }

        public void ToggleWindow()
        {
            if (windowBase == null)
            {
                SetValueWithoutNotify(true);
                ShowWindow<WindowSpline>();
            }
            else
            {
                SetValueWithoutNotify(false);
                CloseWindow();
            }
        }
    }
}
