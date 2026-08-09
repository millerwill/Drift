// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandlePlane.cs
//
// Author: Mikael Danielsson
// Date Created: 28-09-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using SplineArchitect.Utility;
using SplineArchitect.Libraries;

namespace SplineArchitect
{
    public class EHandlePlane
    {
        private static Vector3[] triangleContainer = new Vector3[4];

        public static void OnSceneGUI(Spline spline, Event e)
        {
            if (!EGlobalSettings.GetPlaneVisibility())
                return;

            if (EHandleSelection.SelectedControlPoint == 0)
                return;

            if (e.type != EventType.MouseUp)
                return;

            EHandleTool.ActivatePositionToolForControlPoint(spline);
        }

        public static Bounds GetBounds(Spline spline)
        {
            const int extraSize = 3;

            float gridSize = EGlobalSettings.GetGridSize();

            float lowestX = 99999;
            float highestX = -99999;
            float lowestY = 99999;
            float highestY = -99999;
            float lowestZ = 99999;
            float highestZ = -99999;

            foreach (Segment s in spline.segments)
            {
                Vector3 anchor = s.GetPosition(ControlHandle.ANCHOR, Space.Self);
                Vector3 tangentA = s.GetPosition(ControlHandle.TANGENT_A, Space.Self);
                Vector3 tangentB = s.GetPosition(ControlHandle.TANGENT_B, Space.Self);

                if (anchor.x < lowestX) lowestX = anchor.x;
                if (anchor.x > highestX) highestX = anchor.x;
                if (tangentA.x < lowestX) lowestX = tangentA.x;
                if (tangentA.x > highestX) highestX = tangentA.x;
                if (tangentB.x < lowestX) lowestX = tangentB.x;
                if (tangentB.x > highestX) highestX = tangentB.x;

                if (anchor.y < lowestY) lowestY = anchor.y;
                if (anchor.y > highestY) highestY = anchor.y;
                if (tangentA.y < lowestY) lowestY = tangentA.y;
                if (tangentA.y > highestY) highestY = tangentA.y;
                if (tangentB.y < lowestY) lowestY = tangentB.y;
                if (tangentB.y > highestY) highestY = tangentB.y;

                if (anchor.z < lowestZ) lowestZ = anchor.z;
                if (anchor.z > highestZ) highestZ = anchor.z;
                if (tangentA.z < lowestZ) lowestZ = tangentA.z;
                if (tangentA.z > highestZ) highestZ = tangentA.z;
                if (tangentB.z < lowestZ) lowestZ = tangentB.z;
                if (tangentB.z > highestZ) highestZ = tangentB.z;
            }

            lowestX -= gridSize * extraSize;
            highestX += gridSize * extraSize;
            lowestY -= gridSize * extraSize;
            highestY += gridSize * extraSize;
            lowestZ -= gridSize * extraSize;
            highestZ += gridSize * extraSize;

            Vector3 min = new Vector3(lowestX, 0f, lowestZ);
            Vector3 max = new Vector3(highestX, 0f, highestZ);
            min = SnapToGrid(spline, min, false);
            max = SnapToGrid(spline, max, false);

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);

            return bounds;
        }

        public static Vector3 SnapToGrid(Spline spline, Vector3 point, bool transformToLocal = true)
        {
            Vector3 gridPoint = point;
            if(transformToLocal)
                gridPoint = spline.transform.InverseTransformPoint(gridPoint);

            gridPoint = GeneralUtility.RoundToClosest(gridPoint, EGlobalSettings.GetGridSize());

            if (transformToLocal)
                gridPoint = spline.transform.TransformPoint(gridPoint);

            return gridPoint;
        }

        public static float SnapToGridByDistance(float distance)
        {
            return GeneralUtility.RoundToClosest(distance, EGlobalSettings.GetGridSize());
        }

        internal static void DrawLabels(Spline spline)
        {
            bool drawGridDistanceLabels = EGlobalSettings.GetPlaneDistanceLabels();
            Handles.color = Color.black;
            ControlHandle selectedType = SplineUtility.GetControlHandleType(EHandleSelection.SelectedControlPoint);
            bool in2DMode = EHandleSceneView.GetCurrent().in2DMode;

            for (int i = 0; i < spline.segments.Count; i++)
            {
                Segment s = spline.segments[i];
                ControlHandle hoveredControlHandle = EHandleSelection.IsHovering(i);

                bool isPrimarySelection = EHandleSelection.IsPrimiarySelection(s);
                ControlHandle primarySelectedlHandle = isPrimarySelection ? SplineUtility.GetControlHandleType(EHandleSelection.SelectedControlPoint) : ControlHandle.NONE;
                bool isSecondarySelection = EHandleSelection.IsSecondarySelection(s);

                Vector3 anchor = s.GetPosition(ControlHandle.ANCHOR);
                Vector3 tangentA = s.GetPosition(ControlHandle.TANGENT_A);
                Vector3 tangentB = s.GetPosition(ControlHandle.TANGENT_B);

                float anchorDistance;
                Vector3 labelPointAnchor = GetLabelPoint(spline, anchor, out anchorDistance);

                float tangentADistance;
                Vector3 labelPointTangentA = GetLabelPoint(spline, tangentA, out tangentADistance);

                float tangentBDistance;
                Vector3 labelPointTangentB = GetLabelPoint(spline, tangentB, out tangentBDistance);

                bool skipDraw = selectedType != ControlHandle.ANCHOR && (GeneralUtility.IsEqual(labelPointAnchor, labelPointTangentB) || GeneralUtility.IsEqual(labelPointAnchor, labelPointTangentA));
                if (!GeneralUtility.IsEqual(labelPointAnchor, anchor, 0.01f) && !skipDraw)
                {
                    Handles.DrawLine(labelPointAnchor, anchor);

                    float value = Mathf.Round(anchorDistance * 100f) / 100f;
                    bool labelSelectionCheck = true;
                    if (drawGridDistanceLabels && labelSelectionCheck) Handles.Label(labelPointAnchor, value.ToString(), LibraryGUIStyle.textSceneView);
                }

                if (s.GetInterpolationType() == InterpolationType.SPLINE && EGlobalSettings.GetHandleType() != ControlHandleType.AUTO)
                {
                    skipDraw = selectedType != ControlHandle.TANGENT_A && (GeneralUtility.IsEqual(labelPointTangentA, labelPointAnchor) || GeneralUtility.IsEqual(labelPointTangentA, labelPointTangentB));

                    if (!GeneralUtility.IsEqual(labelPointTangentA, tangentA, 0.01f) && !skipDraw)
                    {
                        Handles.DrawLine(labelPointTangentA, tangentA);

                        float value = Mathf.Round(tangentADistance * 100f) / 100f;
                        bool labelSelectionCheck = true;
                        if (drawGridDistanceLabels && labelSelectionCheck) Handles.Label(labelPointTangentA, value.ToString(), LibraryGUIStyle.textSceneView);
                    }

                    skipDraw = selectedType != ControlHandle.TANGENT_B && (GeneralUtility.IsEqual(labelPointTangentB, labelPointAnchor) || GeneralUtility.IsEqual(labelPointTangentB, labelPointTangentA));

                    if (!GeneralUtility.IsEqual(labelPointTangentB, tangentB, 0.01f) && !skipDraw)
                    {
                        Handles.DrawLine(labelPointTangentB, tangentB);

                        float value = Mathf.Round(tangentBDistance * 100f) / 100f;
                        bool labelSelectionCheck = true;
                        if (drawGridDistanceLabels && labelSelectionCheck) Handles.Label(labelPointTangentB, value.ToString(), LibraryGUIStyle.textSceneView);
                    }
                }
            }

            Vector3 GetLabelPoint(Spline spline, Vector3 point, out float distance)
            {
                Vector3 planeNormal = spline.transform.up;
                Vector3 planePoint = spline.transform.position;

                distance = Vector3.Dot(point - planePoint, planeNormal);

                return point - planeNormal * distance;
            }
        }

        internal static void DrawPlane(Matrix4x4 matrix, Bounds bounds)
        {
            Color color1 = EGlobalSettings.GetPlaneColor();
            Color color2 = new Color(color1.r, color1.g, color1.b, 0.1f);

            if (EGlobalSettings.GetPlaneOccluded())
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            else
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            float y = (bounds.min.y + bounds.max.y) / 2f + 0.025f;

            Vector3 bottomLeft = matrix.MultiplyPoint3x4(new Vector3(bounds.min.x, y, bounds.min.z));
            Vector3 bottomRight = matrix.MultiplyPoint3x4(new Vector3(bounds.max.x, y, bounds.min.z));
            Vector3 topLeft = matrix.MultiplyPoint3x4(new Vector3(bounds.min.x, y, bounds.max.z));
            Vector3 topRight = matrix.MultiplyPoint3x4(new Vector3(bounds.max.x, y, bounds.max.z));

            triangleContainer[0] = bottomLeft;
            triangleContainer[1] = topLeft;
            triangleContainer[2] = topRight;
            triangleContainer[3] = bottomRight;

            Handles.color = color1;
            Handles.DrawSolidRectangleWithOutline(triangleContainer, color2, color1);
        }

        internal static void DrawGrid(Matrix4x4 matrix, Bounds bounds, float size)
        {
            int count = 0;

            Color color1 = EGlobalSettings.GetPlaneColor();
            Color color2 = new Color(color1.r, color1.g, color1.b, 0.33f);

            if (EGlobalSettings.GetPlaneOccluded())
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            else
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            float increment = bounds.min.x;
            float length = bounds.max.x + size;
            float y = (bounds.min.y + bounds.max.y) / 2 + 0.025f;

            while (increment < length)
            {
                Vector3 point1 = new Vector3(increment, y, bounds.min.z);
                Vector3 point2 = new Vector3(increment, y, bounds.max.z);
                point1 = matrix.MultiplyPoint3x4(point1);
                point2 = matrix.MultiplyPoint3x4(point2);

                if (count != 0)
                {
                    Handles.color = color2;
                    Handles.DrawLine(point1, point2);
                }
                else
                {
                    Handles.color = color1;
                    Handles.DrawLine(point1, point2, EHandleSceneView.GetCurrent().in2DMode ? 2 : 1);
                }

                increment += size;

                if (count == 9) count = 0;
                else count++;
            }

            //Increment
            increment = bounds.min.z;

            //Length
            length = bounds.max.z + size;

            count = 0;
            while (increment < length)
            {
                Vector3 point1 = new Vector3(bounds.min.x, y, increment);
                Vector3 point2 = new Vector3(bounds.max.x, y, increment);
                point1 = matrix.MultiplyPoint3x4(point1);
                point2 = matrix.MultiplyPoint3x4(point2);

                if (count != 0)
                {
                    Handles.color = color2;
                    Handles.DrawLine(point1, point2);
                }
                else
                {
                    Handles.color = color1;
                    Handles.DrawLine(point1, point2, EHandleSceneView.GetCurrent().in2DMode ? 2 : 1);
                }
                increment += size;

                if (count == 9) count = 0;
                else count++;
            }
        }
    }
}
