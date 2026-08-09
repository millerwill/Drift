// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: WindowPlane.cs
//
// Author: Mikael Danielsson
// Date Created: 15-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Libraries;
using SplineArchitect.Utility;

namespace SplineArchitect.Ui
{
    public class WindowPlane : WindowBase
    {
        private static string[] optionsPlaneTypes = new string[] { "Grid", "Plane" };

        protected override void OnGUIExtended()
        {
            PlaneType planeType = EGlobalSettings.GetPlaneType();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
            EUiUtility.CreateLabelField("<b>Plane settings</b>", LibraryGUIStyle.textHeaderBlack, true);

            if (!EGlobalSettings.GetIsWindowsFloating())
            {
                //Close
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconClose, 19, 14, () =>
                {
                    EActionToSceneGUI.Add(() =>
                    {
                        CloseWindow();
                    }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                    EHandleSceneView.RepaintCurrent();
                });
            }

            GUILayout.EndHorizontal();

            EUiUtility.CreatePopupField("Type:", 60, (int)planeType, optionsPlaneTypes, (newValue) =>
            {
                EGlobalSettings.SetPlaneType((PlaneType)newValue);
                toolbarDropdownTogglePlane.SetIcons((PlaneType)newValue);
                EHandleSceneView.RepaintCurrent();
            });

            if (planeType == PlaneType.GRID)
            {
                EUiUtility.CreateFloatFieldWithLabel("Size:", EGlobalSettings.GetGridSize(), (newValue) =>
                {
                    EGlobalSettings.SetGridSize(newValue);
                    EHandleSceneView.RepaintCurrent();
                }, 60, 54);
            }

            EUiUtility.CreateColorField("Color:", EGlobalSettings.GetPlaneColor(), (Color newColor) =>
            {
                EGlobalSettings.SetPlaneColor(newColor);
                EHandleSceneView.RepaintCurrent();
            }, 60);

            EUiUtility.CreateToggleField("Occluded:", EGlobalSettings.GetPlaneOccluded(), (newValue) =>
            {
                EGlobalSettings.SetPlaneOccluded(newValue);
                EHandleSceneView.RepaintCurrent();
            });

            EUiUtility.CreateToggleField("Distance labels:", EGlobalSettings.GetPlaneDistanceLabels(), (newValue) =>
            {
                EGlobalSettings.SetPlaneDistanceLabels(newValue);
                EHandleSceneView.RepaintCurrent();
            });
        }

        protected override void UpdateWindowSize()
        {
            PlaneType planeType = EGlobalSettings.GetPlaneType();
            Vector2 size = new Vector2(125, itemHeight * 4 + 20);

            if(planeType == PlaneType.GRID)
                size.y += itemHeight;

            cachedRect.size = size;
        }
    }
}
