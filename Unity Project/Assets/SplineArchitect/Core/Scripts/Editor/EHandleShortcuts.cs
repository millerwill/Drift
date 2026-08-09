// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleShortcuts.cs
//
// Author: Mikael Danielsson
// Date Created: 23-03-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor.ShortcutManagement;
using UnityEditor;

using SplineArchitect.ScriptableObjects;
using SplineArchitect.Utility;
using SplineArchitect.Ui;

namespace SplineArchitect
{
    internal class EHandleShortcuts
    {
        //Ids
        internal const string hideUiId = "Spline Architect/Hide ui";
        internal const string toggleGridVisibilityId = "Spline Architect/Toggle grid visibility";
        internal const string toggleNormalsId = "Spline Architect/Toggle normals";

        [Shortcut(hideUiId, KeyCode.H, ShortcutModifiers.Alt)]
        private static void HideUi()
        {
            SceneView sceneView = EHandleSceneView.GetCurrent();

            foreach (ToolbarToggleBase ttb in ToolbarToggleBase.instances)
            {
                ToolbarToggleControlPanel ttcp = ttb as ToolbarToggleControlPanel;

                if (ttcp == null)
                    continue;

                if (ttcp.currentSceneView == null)
                    continue;

                if(ttcp.currentSceneView == sceneView)
                    ttcp.ToggleWindow();
            }
        }

        [Shortcut(toggleGridVisibilityId)]
        private static void ToggleGridVisibility()
        {
            EGlobalSettings.SetPlaneVisibility(!EGlobalSettings.GetPlaneVisibility());
        }

        [Shortcut(toggleNormalsId)]
        private static void ToggleNormals()
        {
            EGlobalSettings.SetShowNormals(!EGlobalSettings.GetShowNormals());
        }

        [Shortcut("Spline Architect/Toggle splines")]
        private static void ToggleSplines()
        {
            int value = (int)EGlobalSettings.GetSplineHideMode() + 1;
            if (value > 2) value = 0;

            EGlobalSettings.SetSplineHideMode((SplineHideMode)value);
        }

        [Shortcut("Spline Architect/Toggle creation mode")]
        private static void ToggleCreationMode()
        {
            EHandleSpline.controlPointCreationActive = !EHandleSpline.controlPointCreationActive;
        }

        [Shortcut("Spline Architect/Select all anchors")]
        private static void SelectAllAnchors()
        {
            Spline spline = EHandleSelection.selectedSpline;
            if (spline == null)
                return;

            EHandleSelection.SelectAllAnchors(spline);

            EActionToSceneGUI.Add(() => {
                EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
            }, EActionToSceneGUI.Type.LATE, EventType.Layout);
        }

        [Shortcut("Spline Architect/Next control point", KeyCode.Period, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        private static void NextControlPoint()
        {
            Spline spline = EHandleSelection.selectedSpline;
            if (spline == null)
                return;

            if (EHandleSelection.selectedSplineObject != null)
                return;

            EHandleSelection.UpdateSelectedControlPointsRecordUndo((unddoState) =>
            {
                unddoState.selectedControlPoint = EHandleSpline.GetNextControlPoint(spline);
            }, "Next control point");

            EActionToSceneGUI.Add(() => {
                EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
            }, EActionToSceneGUI.Type.LATE, EventType.Layout);
        }
         
        [Shortcut("Spline Architect/Prev control point", KeyCode.Comma, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        private static void PrevControlPoint()
        {
            Spline spline = EHandleSelection.selectedSpline;
            if (spline == null)
                return;

            if (EHandleSelection.selectedSplineObject != null)
                return;

            EHandleSelection.UpdateSelectedControlPointsRecordUndo((unddoState) =>
            {
                unddoState.selectedControlPoint = EHandleSpline.GetNextControlPoint(spline, true);
            }, "Prev control point");

            EActionToSceneGUI.Add(() => {
                EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
            }, EActionToSceneGUI.Type.LATE, EventType.Layout);
        }

        [Shortcut("Spline Architect/Flatten control points")]
        private static void FlattenControlPoints()
        {
            Spline spline = EHandleSelection.selectedSpline;
            if (spline == null)
                return;

            if(EHandleSelection.SelectedControlPoint != 0)
            {
                EHandleSelection.UpdateSelectedSegmentsRecordUndo(spline, (selected) =>
                {
                    EHandleSpline.MoveControlPointsToPlane(spline);
                }, "Flatten control points");

                EActionToSceneGUI.Add(() => {
                    EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
                }, EActionToSceneGUI.Type.LATE, EventType.Layout);
            }
            else
            {
                EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                {
                    EHandleSpline.MoveControlPointsToPlane(selected);
                }, "Flatten control points");

                EActionToSceneGUI.Add(() => {
                    EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
                }, EActionToSceneGUI.Type.LATE, EventType.Layout);
            }
        }
    }
}
