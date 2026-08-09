// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: WindowSettings.cs
//
// Author: Mikael Danielsson
// Date Created: 05-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

using SplineArchitect.Libraries;
using SplineArchitect.Utility;

namespace SplineArchitect.Ui
{
    public class WindowSettings : WindowBase
    {
        protected override void OnGUIExtended()
        {
            EUiUtility.ResetGetBackgroundStyleId();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
            EUiUtility.CreateLabelField("<b>Settings</b>", LibraryGUIStyle.textHeaderBlack, true);

            if(!EGlobalSettings.GetIsWindowsFloating())
            {
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconClose, 19, 14, () =>
                {
                    EActionToSceneGUI.Add(() =>
                    {
                        toolbarToggleBase.SetValueWithoutNotify(false);
                        CloseWindow();
                    }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                    EHandleSceneView.RepaintCurrent();
                });
            }

            GUILayout.EndHorizontal();

            //Spline resolution
            EUiUtility.CreateSliderAndInputField("Spline line resolution:", EGlobalSettings.GetSplineLineResolution(), (newValue, changeBySlider) =>
            {
                EGlobalSettings.SetSplineLineResolution(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 1, 200, 125, 50);

            //Spline view distance
            EUiUtility.CreateSliderAndInputField("Spline view distance:", EGlobalSettings.GetSplineViewDistance(), (newValue, changeBySlider) =>
            {
                EGlobalSettings.SetSplineViewDistance(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 100, 2500, 125, 50);

            //Normals spacing
            EUiUtility.CreateSliderAndInputField("Normals spacing:", EGlobalSettings.GetNormalsSpacing(), (newValue, changeBySlider) =>
            {
                EGlobalSettings.SetNormalsSpacing(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 1, 200, 125, 50);

            //Normals length
            EUiUtility.CreateSliderAndInputField("Normals length:", EGlobalSettings.GetNormalsLength(), (newValue, changeBySlider) =>
            {
                EGlobalSettings.SetNormalsLength(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 0.001f, 2, 125, 50);

            //Control point Size
            EUiUtility.CreateSliderAndInputField("Control point size:", EGlobalSettings.GetControlPointSize(), (newValue, changeBySlider) =>
            {
                EGlobalSettings.SetControlPointSize(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 0.05f, 2.5f, 125, 50);

            //Control point Size
            EUiUtility.CreateSliderAndInputField("Control point scale distance:", EGlobalSettings.GetControlPointScaleDistance(), (newValue, changeBySlider) =>
            {
                EGlobalSettings.SetControlPointScaleDistance(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 10, 1000, 90, 50);

            //Deformation performance
            EUiUtility.CreateSliderAndInputField("Deformation performance:", EGlobalSettings.GetDeformationPerformance(), (newValue, changeBySlider) =>
            {
                EGlobalSettings.SetDeformationPerformance(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 0, 1, 90, 50);

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateToggleField("Link indicators:", EGlobalSettings.GetShowLinkIndicators(), (newValue) =>
            {
                EGlobalSettings.SetShowLinkIndicators(newValue);
                EHandleSceneView.RepaintCurrent();
            }, true, true, 94, 20);
            GUILayout.EndHorizontal();

            EUiUtility.CreateSubSection("MENUS");

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateToggleField("Info icons:", EGlobalSettings.GetInfoIconsVisibility(), (newValue) =>
            {
                EGlobalSettings.SetInfoIconsVisibility(newValue);
            }, true, true, 66, 20);

            GUILayout.FlexibleSpace();
            bool isWindowsFloating = EGlobalSettings.GetIsWindowsFloating();
            EUiUtility.CreateToggleField("Floating menus:", isWindowsFloating, (newValue) =>
            {
                EGlobalSettings.SetIsWindowsFloating(newValue);
                CloseAll();

                foreach(ToolbarToggleBase ttb in ToolbarToggleBase.instances)
                {
                    ToolbarToggleSettings tts = ttb as ToolbarToggleSettings;
                    if(tts != null)
                    {
                        tts.CloseOrOpenWindow(true);
                        tts.SetValueWithoutNotify(true);
                        break;
                    }
                }

            }, true, true, 98, 20);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateToggleField("Horizontal layout:", EGlobalSettings.GetWindowHorizontalOrder(), (newValue) =>
            {
                EGlobalSettings.SetWindowHorizontalOrder(newValue);
            }, !isWindowsFloating, true, 107, 20);

            GUILayout.FlexibleSpace();
            EUiUtility.CreateToggleField("Submenus on top:", EGlobalSettings.GetSubmenusOnTop(), (newValue) =>
            {
                EGlobalSettings.SetSubmenusOnTop(newValue);
            }, true, true, 112, 20);
            GUILayout.EndHorizontal();
        }

        protected override void UpdateWindowSize()
        {
            cachedRect.size = new Vector2(327, 22 * 10 + 36);
        }
    }
}
