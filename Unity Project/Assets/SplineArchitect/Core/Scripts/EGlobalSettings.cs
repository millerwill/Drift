// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EGlobalSettings.cs
//
// Author: Mikael Danielsson
// Date Created: 23-04-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect
{
    internal static class EGlobalSettings
    {
        //Cached data
        private static Color planeColor;
        private static bool planeOccluded;
        private static bool planeDistanceLabels;
        private static bool showNormals;
        private static SplineHideMode splineHideMode;
        private static bool controlPanelWindowMinimized;
        private static bool extendedWindowMinimized;
        private static bool windowHorizontalOrder;
        private static float gridSize;
        private static bool gridVisibility;
        private static bool windowsIsUtility;
        private static bool firstPackageImport;
        private static bool showLinkIndicators;
        private static bool infoIconsVisibility;
        private static float controlPointSize;
        private static ControlHandleType handleType;
        private static float normalsSpacing;
        private static float normalsLength;
        private static float splineLineResolution;
        private static float splineViewDistance;
        private static float controlPointScaleDistance;
        private static float deformationPerformance;
        private static bool subMenusOnTop;
        private static float newSegmentTangentDistance;
        private static PlaneType planeType;

        //General
        private static bool planeColorInit;
        private static bool planeOccludedInit;
        private static bool planeDistanceLabelsInit;
        private static bool showNormalsInit;
        private static bool splineHideModeInit;
        private static bool controlPanelWindowMinimizedInit;
        private static bool extendedWindowMinimizedInit;
        private static bool windowHorizontalOrderInit;
        private static bool gridSizeInit;
        private static bool gridVisibilityInit;
        private static bool windowsIsUtilityInit;
        private static bool firstPackageImportInit;
        private static bool showLinkIndicatorsInit;
        private static bool infoIconsVisibilityInit;
        private static bool controlPointSizeInit;
        private static bool handleTypeInit;
        private static bool normalsSpacingInit;
        private static bool normalsLengthInit;
        private static bool splineLineResolutionInit;
        private static bool splineViewDistanceInit;
        private static bool controlPointScaleDistanceInit;
        private static bool deformationPerformanceInit;
        private static bool subMenusOnTopInit;
        private static bool newSegmentTangentDistanceInit;
        private static bool planeTypeInit;

        internal static PlaneType GetPlaneType()
        {
            if (!planeTypeInit)
            {
                planeTypeInit = true;
                planeType = (PlaneType)EditorPrefs.GetInt("SplineArchitect_planeType", 0);
            }

            return planeType;
        }

        internal static void SetPlaneType(PlaneType planeType)
        {
            EditorPrefs.SetInt("SplineArchitect_planeType", (int)planeType);
            EGlobalSettings.planeType = planeType;
        }

        internal static ControlHandleType GetHandleType()
        {
            if (!handleTypeInit)
            {
                handleTypeInit = true;
                handleType = (ControlHandleType)EditorPrefs.GetInt("SplineArchitect_handleType", 0);
            }

            return handleType;
        }

        internal static void SetHandleType(ControlHandleType handleType)
        {
            EditorPrefs.SetInt("SplineArchitect_handleType", (int)handleType);
            EGlobalSettings.handleType = handleType;
        }

        internal static float GetNormalsSpacing()
        {
            if (!normalsSpacingInit)
            {
                normalsSpacingInit = true;
                normalsSpacing = EditorPrefs.GetFloat("SplineArchitect_normalsSpacing", 50);
            }

            return normalsSpacing;
        }

        internal static void SetNormalsSpacing(float value)
        {
            value = Mathf.Round(value);
            EditorPrefs.SetFloat("SplineArchitect_normalsSpacing", value);
            normalsSpacing = value;
        }

        internal static float GetNormalsLength()
        {
            if (!normalsLengthInit)
            {
                normalsLengthInit = true;
                normalsLength = EditorPrefs.GetFloat("SplineArchitect_normalsLength", 1);
            }

            return normalsLength;
        }

        internal static void SetNormalsLength(float value)
        {
            value = Mathf.Round(value * 100) / 100;
            if (value < 0.001f) value = 0.001f;

            EditorPrefs.SetFloat("SplineArchitect_normalsLength", value);
            normalsLength = value;
        }

        internal static float GetSplineLineResolution()
        {
            if (!splineLineResolutionInit)
            {
                splineLineResolutionInit = true;
                splineLineResolution = EditorPrefs.GetFloat("SplineArchitect_splineLineResolution", 100);
            }

            return splineLineResolution;
        }

        internal static void SetSplineLineResolution(float value)
        {
            if (value < 1)
                value = 1;

            value = Mathf.Round(value);

            EditorPrefs.SetFloat("SplineArchitect_splineLineResolution", value);
            splineLineResolution = value;
        }

        internal static float GetNewSegmentTangentDistance()
        {
            if (!newSegmentTangentDistanceInit)
            {
                newSegmentTangentDistanceInit = true;
                newSegmentTangentDistance = EditorPrefs.GetFloat("SplineArchitect_newSegmentTangentDistance", 6);
            }

            return newSegmentTangentDistance;
        }

        internal static void SetNewSegmentTangentDistance(float value)
        {
            value = Mathf.Clamp(value , 1, 32);
            value = Mathf.Round(value * 10) / 10;

            if(!GetPlaneVisibility() || GetPlaneType() != PlaneType.GRID)
                value = GeneralUtility.RoundToClosest(value, 2);
            else
                value = Mathf.Clamp(value, GetGridSize(), 32);

            EditorPrefs.SetFloat("SplineArchitect_newSegmentTangentDistance", value);
            newSegmentTangentDistance = value;
        }

        internal static bool GetControlPanelWindowMinimized()
        {
            if (!controlPanelWindowMinimizedInit)
            {
                controlPanelWindowMinimizedInit = true;
                controlPanelWindowMinimized = EditorPrefs.GetBool("SplineArchitect_controlPanelWindowMinimized", false);
            }

            return controlPanelWindowMinimized;
        }

        internal static void SetControlPanelWindowMinimized(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_controlPanelWindowMinimized", value);
            controlPanelWindowMinimized = value;
        }

        internal static bool GetWindowHorizontalOrder()
        {
            if (!windowHorizontalOrderInit)
            {
                windowHorizontalOrderInit = true;
                windowHorizontalOrder = EditorPrefs.GetBool("SplineArchitect_windowHorizontalOrder", true);
            }

            return windowHorizontalOrder;
        }

        internal static void SetWindowHorizontalOrder(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_windowHorizontalOrder", value);
            windowHorizontalOrder = value;
        }

        internal static bool GetIsWindowsFloating()
        {
            if (!windowsIsUtilityInit)
            {
                windowsIsUtilityInit = true;
                windowsIsUtility = EditorPrefs.GetBool("SplineArchitect_isWindowsFloating", false);
            }

            return windowsIsUtility;
        }

        internal static void SetIsWindowsFloating(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_isWindowsFloating", value);
            windowsIsUtility = value;
        }

        internal static bool GetShowLinkIndicators()
        {
            if (!showLinkIndicatorsInit)
            {
                showLinkIndicatorsInit = true;
                showLinkIndicators = EditorPrefs.GetBool("SplineArchitect_showLinkIndicators", true);
            }

            return showLinkIndicators;
        }

        internal static void SetShowLinkIndicators(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_showLinkIndicators", value);
            showLinkIndicators = value;
        }

        internal static bool GetExtendedWindowMinimized()
        {
            if (!extendedWindowMinimizedInit)
            {
                extendedWindowMinimizedInit = true;
                extendedWindowMinimized = EditorPrefs.GetBool("SplineArchitect_extendedWindowMinimized", false);
            }

            return extendedWindowMinimized;
        }

        internal static void SetExtendedWindowMinimized(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_extendedWindowMinimized", value);
            extendedWindowMinimized = value;
        }

        internal static bool GetFirstPackageImport()
        {
            if (!firstPackageImportInit)
            {
                firstPackageImportInit = true;
                firstPackageImport = EditorPrefs.GetBool("SplineArchitect_firstPackageImport", false);
            }

            return firstPackageImport;
        }

        internal static void SetFirstPackageImport(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_firstPackageImport", value);
            firstPackageImport = value;
        }

        internal static bool GetPlaneDistanceLabels()
        {
            if (!planeDistanceLabelsInit)
            {
                planeDistanceLabelsInit = true;
                planeDistanceLabels = EditorPrefs.GetBool("SplineArchitect_planeDistanceLabels", false);
            }

            return planeDistanceLabels;
        }

        internal static void SetPlaneDistanceLabels(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_planeDistanceLabels", value);
            planeDistanceLabels = value;
        }

        internal static bool GetPlaneOccluded()
        {
            if(!planeOccludedInit)
            {
                planeOccludedInit = true;
                planeOccluded = EditorPrefs.GetBool("SplineArchitect_planeOccluded", true);
            }

            return planeOccluded;
        }

        internal static void SetPlaneOccluded(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_planeOccluded", value);
            planeOccluded = value;
        }

        internal static SplineHideMode GetSplineHideMode()
        {
            if(!splineHideModeInit)
            {
                splineHideModeInit = true;
                splineHideMode = (SplineHideMode)EditorPrefs.GetInt("SplineArchitect_splineHiddenMode", 0);
            }

            return splineHideMode;
        }

        internal static void SetSplineHideMode(SplineHideMode value)
        {
            EditorPrefs.SetInt("SplineArchitect_splineHiddenMode", (int)value);
            splineHideMode = value;
        }

        internal static float GetGridSize()
        {
            if (!gridSizeInit)
            {
                gridSizeInit = true;
                gridSize = EditorPrefs.GetFloat("SplineArchitect_gridSize", 1);
            }

            return gridSize;
        }

        internal static void SetGridSize(float value)
        {
            if (value < 0.25f)
                value = 0.25f;
            if (value > 16)
                value = 16;

            EditorPrefs.SetFloat("SplineArchitect_gridSize", value);
            gridSize = value;
        }

        internal static Color GetPlaneColor()
        {
            if(!planeColorInit)
            {
                planeColorInit = true;
                planeColor = new Color(EditorPrefs.GetFloat("SplineArchitect_planeColorR", 1),
                                      EditorPrefs.GetFloat("SplineArchitect_planeColorG", 1),
                                      EditorPrefs.GetFloat("SplineArchitect_planeColorB", 1),
                                      1);
            }
    
            return planeColor;
        }

        internal static void SetPlaneColor(Color value)
        {
            EditorPrefs.SetFloat("SplineArchitect_planeColorR", value.r);
            EditorPrefs.SetFloat("SplineArchitect_planeColorG", value.g);
            EditorPrefs.SetFloat("SplineArchitect_planeColorB", value.b);
            planeColor = new Color(value.r, value.g, value.b, 1);
        }

        internal static float GetControlPointSize()
        {
            if (!controlPointSizeInit)
            {
                controlPointSizeInit = true;
                controlPointSize = EditorPrefs.GetFloat("SplineArchitect_controlPointSize", 0.75f);
            }

            return controlPointSize;
        }

        internal static void SetControlPointSize(float value)
        {
            if (value < 0.05f)
                value = 0.05f;

            EditorPrefs.SetFloat("SplineArchitect_controlPointSize", value);
            controlPointSize = value;
        }

        internal static float GetControlPointScaleDistance()
        {
            if (!controlPointScaleDistanceInit)
            {
                controlPointScaleDistanceInit = true;
                controlPointScaleDistance = EditorPrefs.GetFloat("SplineArchitect_controlPointScaleDistance", 250f);
            }

            return controlPointScaleDistance;
        }

        internal static void SetControlPointScaleDistance(float value)
        {
            if (value < 10)
                value = 10;

            EditorPrefs.SetFloat("SplineArchitect_controlPointScaleDistance", value);
            controlPointScaleDistance = value;
        }

        internal static float GetDeformationPerformance()
        {
            if (!deformationPerformanceInit)
            {
                deformationPerformanceInit = true;
                deformationPerformance = EditorPrefs.GetFloat("SplineArchitect_deformationPerformance", 0.75f);
            }

            return deformationPerformance;
        }

        internal static void SetDeformationPerformance(float value)
        {
            EditorPrefs.SetFloat("SplineArchitect_deformationPerformance", value);
            deformationPerformance = value;
        }

        internal static bool GetInfoIconsVisibility()
        {
            if (!infoIconsVisibilityInit)
            {
                infoIconsVisibilityInit = true;
                infoIconsVisibility = EditorPrefs.GetBool("SplineArchitect_infoMessages", true);
            }

            return infoIconsVisibility;
        }

        internal static void SetInfoIconsVisibility(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_infoMessages", value);
            infoIconsVisibility = value;
        }

        internal static bool GetPlaneVisibility()
        {
            if (!gridVisibilityInit)
            {
                gridVisibilityInit = true;
                gridVisibility = EditorPrefs.GetBool("SplineArchitect_planeVisibility", false);
            }

            return gridVisibility;
        }

        internal static void SetPlaneVisibility(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_planeVisibility", value);
            gridVisibility = value;
        }

        internal static float GetSplineViewDistance()
        {
            if (!splineViewDistanceInit)
            {
                splineViewDistanceInit = true;
                splineViewDistance = EditorPrefs.GetFloat("SplineArchitect_splineViewDistance", 500);
            }

            return splineViewDistance;
        }

        internal static void SetSplineViewDistance(float value)
        {
            EditorPrefs.SetFloat("SplineArchitect_splineViewDistance", value);
            splineViewDistance = value;
        }

        internal static bool GetShowNormals()
        {
            if (!showNormalsInit)
            {
                showNormalsInit = true;
                showNormals = EditorPrefs.GetBool("SplineArchitect_showNormals", false);
            }

            return showNormals;
        }

        internal static void SetShowNormals(bool value)
        {
            showNormals = value;
            EditorPrefs.SetBool("SplineArchitect_showNormals", value);
        }

        internal static bool GetSubmenusOnTop()
        {
            if (!subMenusOnTopInit)
            {
                subMenusOnTopInit = true;
                subMenusOnTop = EditorPrefs.GetBool("SplineArchitect_submenusOnTop", false);
            }

            return subMenusOnTop;
        }

        internal static void SetSubmenusOnTop(bool value)
        {
            subMenusOnTop = value;
            EditorPrefs.SetBool("SplineArchitect_submenusOnTop", value);
        }
    }
}

#endif