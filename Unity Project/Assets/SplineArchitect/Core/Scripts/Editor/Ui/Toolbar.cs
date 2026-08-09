// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Toolbar.cs
//
// Author: Mikael Danielsson
// Date Created: 27-07-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

using SplineArchitect.Libraries;

namespace SplineArchitect.Ui
{
    [Overlay(typeof(SceneView), "Spline Architect", defaultDockZone = DockZone.RightToolbar, defaultDockPosition = DockPosition.Top)]
    public class Toolbar : ToolbarOverlay
    {
        public Toolbar() : base(ToolbarToggleControlPanel.ID,
                                ToolbarToggleSettings.ID,
                                ToolbarToggleInfo.ID,
                                ToolbarSpacer.ID,
                                ToolbarDropdownTogglePlane.ID,
                                ToolbarSpacer.ID,
                                ToolbarDropdownHandleType.ID,
                                ToolbarSpacer.ID,
                                ToolbarButtonHideSplines.ID,
                                ToolbarButtonNormals.ID) 
        {
            //Inits
            EHandleUi.Init();
            EditorApplication.delayCall += EHandleUi.SecureCloseAllWindows;
            collapsedIcon = EditorGUIUtility.isProSkin ? LibraryTexture.iconSpline : LibraryTexture.iconSplineLight;
        }
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    public class ToolbarSpacer : VisualElement
    {
        public const string ID = "SplineArchitect_toolbarSpacer";

        public ToolbarSpacer()
        {
            style.width = 0;
        }
    }
}
