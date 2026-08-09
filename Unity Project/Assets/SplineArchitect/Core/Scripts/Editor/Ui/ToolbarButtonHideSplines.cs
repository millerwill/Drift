// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarButtonHideSplines.cs
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
    public class ToolbarButtonHideSplines : ToolbarButtonBase
    {
        public const string ID = "SplineArchitect_toolbarButtonHideSplines";

        public ToolbarButtonHideSplines()
        {
            icon = LibraryTexture.iconHide;
            tooltip = "Hide unselected splines";

            SplineHideMode splineHideMode = EGlobalSettings.GetSplineHideMode();
            SyncVisualData(splineHideMode);
        }

        protected override void ToggleMenu()
        {
            SplineHideMode splineHideMode = EGlobalSettings.GetSplineHideMode();

            if (splineHideMode == SplineHideMode.NONE)
            {
                EGlobalSettings.SetSplineHideMode(SplineHideMode.SELECTED);
                splineHideMode = SplineHideMode.SELECTED;
            }
            else if (splineHideMode == SplineHideMode.SELECTED)
            {
                EGlobalSettings.SetSplineHideMode(SplineHideMode.SELECTED_OCCLUDED);
                splineHideMode = SplineHideMode.SELECTED_OCCLUDED;
            }
            else
            {
                EGlobalSettings.SetSplineHideMode(SplineHideMode.NONE);
                splineHideMode = SplineHideMode.NONE;
            }

            foreach (ToolbarButtonBase tbb in instances)
            {
                ToolbarButtonHideSplines tbhs = tbb as ToolbarButtonHideSplines;

                if (tbhs == null)
                    continue;

                tbhs.SyncVisualData(splineHideMode);
            }
        }

        protected override void OnGeometryChangedExtended(bool isHorizontal)
        {
            base.OnGeometryChangedExtended(isHorizontal);

            if (isHorizontal)
            {
                style.marginRight = 0;
                style.borderTopRightRadius = 0;
                style.borderBottomRightRadius = 0;
            }
            else
            {
                style.marginBottom = 0;
                style.borderBottomLeftRadius = 0;
                style.borderBottomRightRadius = 0;
            }
        }

        protected override void OnPointerLeaveEnter()
        {
            SyncVisualData(EGlobalSettings.GetSplineHideMode());
        }

        private void SyncVisualData(SplineHideMode splineHideMode)
        {
            bool isDark = EditorGUIUtility.isProSkin;

            //Tooltip
            if (splineHideMode == SplineHideMode.SELECTED_OCCLUDED) tooltip = "Only selected (occluded when blocked)";
            else if (splineHideMode == SplineHideMode.SELECTED)     tooltip = "Only selected";
            else                                                    tooltip = "All visible";

            //Icon
            icon = isDark ? LibraryTexture.iconHide : LibraryTexture.iconHideLight;

            //Background color
            if (splineHideMode == SplineHideMode.NONE && !pointerHovering && isDark)        style.backgroundColor = new StyleColor(new Color32(88, 88, 88, 255));
            else if (splineHideMode == SplineHideMode.NONE && !pointerHovering && !isDark)  style.backgroundColor = new StyleColor(new Color32(228, 228, 228, 255));
            else if (splineHideMode == SplineHideMode.NONE && pointerHovering && !isDark)   style.backgroundColor = new StyleColor(new Color32(236, 236, 236, 255));
            else if (splineHideMode == SplineHideMode.NONE && pointerHovering && isDark)    style.backgroundColor = new StyleColor(new Color32(103, 103, 103, 255));
            else if (splineHideMode == SplineHideMode.SELECTED && isDark)                   style.backgroundColor = new StyleColor(new Color32(70, 96, 124, 255));
            else if (splineHideMode == SplineHideMode.SELECTED && !isDark)                  style.backgroundColor = new StyleColor(new Color32(150, 195, 251, 255));
            else if (splineHideMode == SplineHideMode.SELECTED_OCCLUDED && isDark)          style.backgroundColor = new StyleColor(new Color32(156, 141, 76, 255));
            else if (splineHideMode == SplineHideMode.SELECTED_OCCLUDED && !isDark)         style.backgroundColor = new StyleColor(new Color32(251, 212, 150, 255));
        }
    }
}
