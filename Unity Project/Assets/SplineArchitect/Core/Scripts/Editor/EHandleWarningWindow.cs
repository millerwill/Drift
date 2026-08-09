// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleWarningWindow.cs
//
// Author: Mikael Danielsson
// Date Created: 15-11-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;

using SplineArchitect.Utility;
using SplineArchitect.Ui;

namespace SplineArchitect
{
    internal class EHandleWarningWindow
    {
        private static bool debug = false;

        internal static void UpdateGlobal()
        {
            if(!EGlobalSettings.GetFirstPackageImport() || debug)
            {
                debug = false;
                SceneView sceneView = EHandleSceneView.GetCurrent();

                if (sceneView == null)
                    return;

                WarningToolbarNotDisplayed(sceneView);
                WarningGizmosIsDisabled(sceneView);
                WarningUndockedSceneView(sceneView);
                EGlobalSettings.SetFirstPackageImport(true);
            }
        }

        private static void WarningToolbarNotDisplayed(SceneView sceneView)
        {
            OverlayCanvas canvas = sceneView.overlayCanvas;
            sceneView.TryGetOverlay("Spline Architect", out Overlay overlay);
            {
                if (!overlay.displayed)
                {
                    bool option = EditorUtility.DisplayDialog(
                        "Spline Architect Warning",
                        $"Spline Architect is disabled in the Overlay menu, the Toolbar will not be visible. Do you want to enable it?",
                        "Yes",
                        "No");

                    if (option)
                    {
                        overlay.displayed = true;
                    }
                }
            }
        }

        private static void WarningGizmosIsDisabled(SceneView sceneView)
        {
            if(!sceneView.drawGizmos)
            {
                bool option = EditorUtility.DisplayDialog(
                    "Spline Architect Warning",
                    $"Gizmos are disabled, so splines will not be visible in the Scene view. Would you like to enable them?",
                    "Yes",
                    "No");

                if (option)
                {
                    sceneView.drawGizmos = true;
                }
            }
        }

        private static void WarningUndockedSceneView(SceneView sceneView)
        {
            if (!sceneView.docked)
            {
                bool option = EditorUtility.DisplayDialog(
                    "Spline Architect Warning",
                    $"The active Scene View window is undocked. Spline Architect menus may appear behind it, making them unavailable." +
                    $"You can fix this by docking the toolbar to the right or bottom of the Scene View, or by enabling floating menus." +
                    $"\n\nWould you like to enable floating menus?",
                    "Yes",
                    "No");

                if (option)
                {
                    EGlobalSettings.SetIsWindowsFloating(true);
                    WindowBase.CloseAll();

                    EActionDelayed.Add(() =>
                    {
                        foreach (ToolbarToggleBase ttb in ToolbarToggleBase.instances)
                        {
                            ToolbarToggleSettings tts = ttb as ToolbarToggleSettings;
                            if (tts != null)
                            {
                                tts.CloseOrOpenWindow(true, true);
                                tts.SetValueWithoutNotify(true);
                                break;
                            }
                        }
                    }, 0, 5, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE);
                }
            }
        }
    }
}