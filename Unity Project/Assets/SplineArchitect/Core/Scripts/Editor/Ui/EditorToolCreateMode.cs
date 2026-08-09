// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarButtonCreateMode.cs
//
// Author: Mikael Danielsson
// Date Created: 17-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

using SplineArchitect.Libraries;
using SplineArchitect.Utility;

namespace SplineArchitect.Ui
{
    [EditorTool("Spline Architect - Create Spline or Control Point on selected spline")]
    public class EditorToolCreateMode : EditorTool
    {
        public override GUIContent toolbarIcon => icon;
        public GUIContent icon;

        void OnEnable()
        {
            EHandleUi.Init();
            icon = EditorGUIUtility.isProSkin ? LibraryGUIContent.iconCreateSpline : LibraryGUIContent.iconCreateSplineLight;
        }

        public override void OnActivated()
        {
            EHandleSpline.controlPointCreationActive = true;
            EHandleEvents.controlPointCreationActive = true;
        }

        public override void OnWillBeDeactivated()
        {
            EHandleSpline.controlPointCreationActive = false;
            EHandleEvents.controlPointCreationActive = false;
            EHandleTool.ActivatePositionToolForControlPoint(EHandleSelection.selectedSpline);
        }
    }
}
