// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: QuickMoveTool.cs
//
// Author: Mikael Danielsson
// Date Created: 28-05-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Utility;
using SplineUtility = SplineArchitect.Utility.SplineUtility;

namespace SplineArchitect.CustomTools
{
    public class QuickMoveTool
    {
        private static bool active;
        private static Vector3 startPosition;
        private static Vector3 startPositionOffset;
        private static Segment segment;
        private static ControlHandle controlHandle;

        private static Vector3 startCheckPosition;
        private static Vector3 endPosition;
        private static Vector3 planeUp;
        private static int hotControlId;

        public static bool IsUpdating(Event e)
        {
            if (EHandleSpline.controlPointCreationActive)
                return false;

            if (PositionTool.activePart != PositionTool.ActivePart.NONE)
                return false;

            // Deactivate
            if (active && e.type == EventType.MouseUp && e.button == 0)
            {
                float dragMin = EHandleSegment.GetControlPointSize(endPosition) * 1.5f;

                if(Vector3.Distance(endPosition, startCheckPosition) > dragMin)
                    e.Use();

                if(GUIUtility.hotControl == hotControlId)
                    GUIUtility.hotControl = 0;

                active = false;
                return false;
            }

            Spline selectedSpline = EHandleSelection.selectedSpline;
            int hoveredCp = EHandleSelection.hoveredCp;

            if (hoveredCp == 0)
                return false;

            if (selectedSpline == null)
                return false;

            // Activate
            if (!active && e.type == EventType.MouseDrag && e.button == 0)
            {
                int segmentIndex = SplineUtility.ControlPointIdToSegmentIndex(hoveredCp);

                if (segmentIndex < 0 || segmentIndex >= selectedSpline.segments.Count)
                    return false;

                controlHandle = SplineUtility.GetControlHandleType(hoveredCp);
                segment = selectedSpline.segments[segmentIndex];

                startPosition = segment.GetPosition(controlHandle);
                planeUp = segment.Rotation * Vector3.up;

                Plane startPlane = new Plane(planeUp, startPosition);
                Ray startRay = EMouseUtility.GetMouseRay(e.mousePosition);

                if (!startPlane.Raycast(startRay, out float startDistance))
                    return false;

                Vector3 startMousePlanePoint = startRay.GetPoint(startDistance);
                startPositionOffset = startPosition - startMousePlanePoint;

                startCheckPosition = startPosition;
                endPosition = startPosition;

                hotControlId = GUIUtility.GetControlID(FocusType.Passive);
                GUIUtility.hotControl = hotControlId;

                active = true;
            }

            if (!active)
                return false;

            Plane plane = new Plane(planeUp, startPosition);
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);

            if (plane.Raycast(mouseRay, out float distance))
            {
                EHandleUndo.RecordNow(selectedSpline, "Quick moved control point");
                EHandleUndo.RecordNow(selectedSpline.undoState, "Quick moved control point");
                Vector3 newPoint = mouseRay.GetPoint(distance) + startPositionOffset;
                endPosition = newPoint;

                if (EGlobalSettings.GetPlaneVisibility() && EGlobalSettings.GetPlaneType() == PlaneType.GRID)
                    newPoint = EHandlePlane.SnapToGrid(selectedSpline, newPoint);

                ControlHandleType controlHandleType = EGlobalSettings.GetHandleType();

                if (controlHandleType == ControlHandleType.BROKEN)
                    segment.SetBrokenPosition(controlHandle, newPoint);
                else if (controlHandleType == ControlHandleType.MIRRORED)
                    segment.SetMirroredPosition(controlHandle, newPoint);
                else if (controlHandleType == ControlHandleType.CONTINUOUS)
                    segment.SetContinuousPosition(controlHandle, newPoint);
                else
                {
                    Segment nextSegment = segment.NextSegment();
                    Segment prevSegment = segment.PrevSegment();

                    // Transalte new anchor pos
                    segment.SetAnchorPosition(newPoint);

                    if (segment.GetInterpolationType() != InterpolationType.LINE)
                        segment.ApplyAutoSmooth();

                    if (prevSegment != null && prevSegment.GetInterpolationType() != InterpolationType.LINE)
                        prevSegment.ApplyAutoSmooth();

                    if (nextSegment != null && nextSegment.GetInterpolationType() != InterpolationType.LINE)
                        nextSegment.ApplyAutoSmooth();
                }

                EHandleSegment.LinkMovement(segment);
                EHandleTool.ActivatePositionToolForControlPoint(EHandleSelection.selectedSpline);
                EHandleSceneView.RepaintCurrent();
            }

            if (e.type == EventType.MouseDrag)
                e.Use();

            return true;
        }
    }
}
