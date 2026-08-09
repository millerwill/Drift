// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleSegment.cs
//
// Author: Mikael Danielsson
// Date Created: 03-03-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Ui;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public class EHandleSegment
    {
        public struct SegmentIndicatorData
        {
            public Vector3 anchor;
            public Vector3 tangentA;
            public Vector3 tangentB;

            public Vector3 newAnchor;
            public float newTangentADis;
            public float newTangentBDis;
            public Quaternion newRotation;

            public bool originFromStart;
            public bool isLine;
            public bool onTerrainSurface;

            //Only grid
            public Spline gridSpline;
        }

        internal static SegmentIndicatorData segmentIndicatorData = new SegmentIndicatorData();
        private static List<int> markedSegments = new List<int>();
        private static List<Segment> segmentContainer = new List<Segment>();
        private static Plane projectionPlane = new Plane(Vector3.up, Vector3.zero);
        private static Vector3[] normalsContainer = new Vector3[3];
        internal static Vector3 adjustIndicatorStartPoint = new Vector3(0, 99999, 0);
        internal static bool adjustIndicatorActive;

        internal static void BeforeSceneGUI(SceneView sceneView, Event e)
        {
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F)
            {
                Spline spline = EHandleSelection.selectedSpline;

                if (spline != null && EHandleSelection.SelectedControlPoint > 0)
                {
                    Transform editorCameraTransform = EHandleSceneView.GetCurrent().camera.transform;
                    Vector3 cameraDir = editorCameraTransform.forward;

                    int segmentIndex = SplineUtility.ControlPointIdToSegmentIndex(EHandleSelection.SelectedControlPoint);
                    Segment selectedSegment = spline.GetSegmentAtIndex(segmentIndex);
                    ControlHandle controlHandle = SplineUtility.GetControlHandleType(EHandleSelection.SelectedControlPoint);
                    float dis = Vector3.Distance(selectedSegment.GetPosition(ControlHandle.TANGENT_A), 
                                                 selectedSegment.GetPosition(ControlHandle.TANGENT_B));
                    dis /= 3;

                    sceneView.LookAt(selectedSegment.GetPosition(controlHandle), editorCameraTransform.rotation, dis);
                    e.Use();
                }
            }
        }

        internal static void LinkMovementAll(Spline spline)
        {
            foreach (Segment s in spline.segments)
            {
                LinkMovement(s);
            }
        }

        internal static void LinkMovement(Segment s)
        {
            if (s.LinkCount == 0)
                return;

            Vector3 newPosition = s.GetPosition(ControlHandle.ANCHOR);

            for (int i = 0; i < s.LinkCount; i++)
            {
                Segment link = s.GetLinkAtIndex(i);

                if (link == s)
                    continue;

                if (link.localSpace == null)
                {
                    Debug.LogWarning("[Spline Architect] Segment has no local space set.");
                    continue;
                }

                Vector3 dif = link.GetPosition(ControlHandle.ANCHOR) - newPosition;

                EHandleUndo.RecordNow(link.SplineParent);
                EHandleUndo.RecordNow(link.SplineParent.undoState);
                link.Translate(ControlHandle.ANCHOR, dif);
                link.Translate(ControlHandle.TANGENT_A, dif);
                link.Translate(ControlHandle.TANGENT_B, dif);

                if (link.SplineParent.Loop && link.SplineParent.segments[0] == link)
                {
                    int last = link.SplineParent.segments.Count - 1;

                    link.SplineParent.segments[last].Translate(ControlHandle.ANCHOR, dif);
                    link.SplineParent.segments[last].Translate(ControlHandle.TANGENT_A, dif);
                    link.SplineParent.segments[last].Translate(ControlHandle.TANGENT_B, dif);
                }
            }
        }

        internal static void SegmentMovement(Spline spline, Segment segment, ControlHandle controlHandle, Vector3 dif)
        {
            //In some cases when working with prefabs the spline parent can be null. So we set it here before handling the segment.
            segment.splineParent = spline;

            ControlHandleType handleType = EGlobalSettings.GetHandleType();

            if (handleType == ControlHandleType.CONTINUOUS)
            {
                EHandleSelection.UpdateSelectedSegments(spline, (selected, anchorSelected, tangentASelected, tangentBSelected) =>
                {
                    if (anchorSelected || (tangentASelected && tangentBSelected))
                    {
                        selected.TranslateAnchor(dif);
                        LinkMovement(selected);
                    }
                    else
                    {
                        if(tangentASelected) selected.SetContinuousPosition(ControlHandle.TANGENT_A, selected.GetPosition(ControlHandle.TANGENT_A) - dif);
                        else if(tangentBSelected) selected.SetContinuousPosition(ControlHandle.TANGENT_B, selected.GetPosition(ControlHandle.TANGENT_B) - dif);
                    }
                });
            }
            else if (handleType == ControlHandleType.MIRRORED)
            {
                EHandleSelection.UpdateSelectedSegments(spline, (selected, anchorSelected, tangentASelected, tangentBSelected) =>
                {
                    if (anchorSelected || (tangentASelected && tangentBSelected))
                    {
                        selected.TranslateAnchor(dif);
                        LinkMovement(selected);
                    }
                    else
                    {
                        if (tangentASelected) selected.SetMirroredPosition(ControlHandle.TANGENT_A, selected.GetPosition(ControlHandle.TANGENT_A) - dif);
                        else if (tangentBSelected) selected.SetMirroredPosition(ControlHandle.TANGENT_B, selected.GetPosition(ControlHandle.TANGENT_B) - dif);
                    }
                });
            }
            else if (handleType == ControlHandleType.BROKEN)
            {
                EHandleSelection.UpdateSelectedSegments(spline, (selected, anchorSelected, tangentASelected, tangentBSelected) =>
                {
                    if (anchorSelected)
                    {
                        selected.TranslateAnchor(dif);
                        LinkMovement(selected);
                    }
                    else
                    {
                        if (tangentASelected) selected.SetBrokenPosition(ControlHandle.TANGENT_A, selected.GetPosition(ControlHandle.TANGENT_A) - dif);
                        if (tangentBSelected) selected.SetBrokenPosition(ControlHandle.TANGENT_B, selected.GetPosition(ControlHandle.TANGENT_B) - dif);
                    }
                });
            }
            else if (handleType == ControlHandleType.AUTO)
            {
                segmentContainer.Clear();

                EHandleSelection.UpdateSelectedSegments(spline, (selected, anchorSelected, tangentASelected, tangentBSelected) =>
                {
                    Segment nextSegment = selected.NextSegment();
                    Segment prevSegment = selected.PrevSegment();

                    // Transalte new anchor pos
                    selected.TranslateAnchor(dif);

                    if (selected != null && !segmentContainer.Contains(selected) && segment.GetInterpolationType() != InterpolationType.LINE) segmentContainer.Add(selected);
                    if (prevSegment != null && !segmentContainer.Contains(prevSegment) && prevSegment.GetInterpolationType() != InterpolationType.LINE) segmentContainer.Add(prevSegment);
                    if (nextSegment != null && !segmentContainer.Contains(nextSegment) && nextSegment.GetInterpolationType() != InterpolationType.LINE) segmentContainer.Add(nextSegment);

                    LinkMovement(selected);
                });

                for (int i = 0; i < segmentContainer.Count; i++)
                {
                    segmentContainer[i].ApplyAutoSmooth();
                }
            }

            EHandleEvents.InvokeAfterSegmentMovement(segment, controlHandle);
        }

        internal static void AdjustIndicator(Spline spline, Event e)
        {
            if (GeneralUtility.IsEqual(adjustIndicatorStartPoint, new Vector3(0, 99999, 0)))
                adjustIndicatorStartPoint = e.mousePosition;

            if (Mathf.Abs(adjustIndicatorStartPoint.x - e.mousePosition.x) > 6 ||
                Mathf.Abs(adjustIndicatorStartPoint.y - e.mousePosition.y) > 6)
            {
                adjustIndicatorActive = true;
            }

            if (!adjustIndicatorActive)
                return;

            int segmentId = SplineUtility.ControlPointIdToSegmentIndex(EHandleSelection.SelectedControlPoint);
            Segment segment = spline.GetSegmentAtIndex(segmentId);
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);

            Vector3 planeNormal = segmentIndicatorData.newRotation * Vector3.up;
            Vector3 planePoint = segment.GetPosition(ControlHandle.ANCHOR);

            Vector3 newTangentPos = planePoint;

            projectionPlane.SetNormalAndPosition(planeNormal, planePoint);
            if (projectionPlane.Raycast(mouseRay, out float en))
                newTangentPos = mouseRay.GetPoint(en);

            if (EGlobalSettings.GetPlaneVisibility() && EGlobalSettings.GetPlaneType() == PlaneType.GRID)
            {
                newTangentPos = EHandlePlane.SnapToGrid(spline, newTangentPos);
            }

            EHandleUndo.RecordNow(spline);
            ControlHandle controlHandle = ControlHandle.TANGENT_A;
            if(segmentIndicatorData.originFromStart)
                controlHandle = ControlHandle.TANGENT_B;

            ControlHandleType handleType = EGlobalSettings.GetHandleType();
            if (handleType == ControlHandleType.CONTINUOUS)
                segment.SetContinuousPosition(controlHandle, newTangentPos);
            else
                segment.SetMirroredPosition(controlHandle, newTangentPos);

            EHandleSceneView.GetCurrent().Repaint();
        }

        internal static void UpdateIndicatorHover(Spline spline, Vector3 mousePosition)
        {
            List<Segment> segments = spline.segments;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(mousePosition);

            if (segments.Count > 1)
            {
                Vector3 closestPos = EHandleSpline.ClosestMousePoint(spline, mouseRay, 12, out float distanceToSpline, out float time, 40);
                spline.indicatorSegment = spline.GetSegment(time);

                if (!spline.Loop)
                {
                    Vector3 startPos = spline.GetPositionOnSegment(0, 0);
                    Vector3 endPos = spline.GetPositionOnSegment(segments.Count - 1, 1);

                    GetIndicatorExtendedData(spline, startPos, endPos, mouseRay, out Vector3 extendedPos, out float extendedDistance, out float extendedTime);
                    if (EGlobalSettings.GetHandleType() == ControlHandleType.AUTO)  extendedDistance = 9999;
                    if (extendedDistance < distanceToSpline)
                    {
                        distanceToSpline = extendedDistance;
                        closestPos = extendedPos;
                        time = extendedTime;
                    }

                    if (GeneralUtility.IsEqual(time, 0) || time < 0) spline.indicatorSegment = 0;
                    else if (GeneralUtility.IsEqual(time, 1) || time > 1) spline.indicatorSegment = spline.segments.Count;
                }

                spline.indicatorDistanceToSpline = distanceToSpline;
                spline.indicatorTime = time;
                spline.indicatorPosition = closestPos;
                spline.indicatorDirection = -spline.GetDirection(time);
            }
            else if (segments.Count == 1)
            {
                Vector3 point = segments[0].GetPosition(ControlHandle.ANCHOR);
                EHandleSegment.GetIndicatorExtendedData(spline, point, point, mouseRay, out Vector3 extendedPos, out float extendedDistance, out float extendedTime);
                if (EGlobalSettings.GetHandleType() == ControlHandleType.AUTO) extendedDistance = 9999;

                spline.indicatorDistanceToSpline = extendedDistance;
                spline.indicatorTime = extendedTime;
                spline.indicatorPosition = extendedPos;
                spline.indicatorDirection = segments[0].GetDirection();
                spline.indicatorSegment = GeneralUtility.IsEqual(extendedTime, 0) ? 0 : 1;
            }
        }

        internal static void UpdateIndicator(Spline spline, Event e)
        {
            // General
            bool is2D = EHandleSceneView.GetCurrent().in2DMode;
            float disBetweenTangents = EGlobalSettings.GetNewSegmentTangentDistance();
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);
            Transform cam = EHandleSceneView.GetCurrent().camera.transform;
            EHandleSpline.controlPointIndicatorDisabled = false;

            // Last segment
            Vector3 lastDirection = spline.segments[spline.segments.Count - 1].GetDirection();
            Vector3 lastAnchor = spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.ANCHOR);
            Vector3 lastTangentA = spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.TANGENT_A);
            Vector3 lastTangentB = spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.TANGENT_B);
            Quaternion lastRotation = spline.segments[spline.segments.Count - 1].Rotation;
            Vector3 lastTangentUpdate = lastTangentA + lastDirection * DistanceModifier(lastTangentA, disBetweenTangents);

            // First segment
            Vector3 firstDirection = spline.segments[0].GetDirection();
            Vector3 firstAnchor = spline.segments[0].GetPosition(ControlHandle.ANCHOR);
            Vector3 firstTangentA = spline.segments[0].GetPosition(ControlHandle.TANGENT_A);
            Vector3 firstTangentB = spline.segments[0].GetPosition(ControlHandle.TANGENT_B);
            Quaternion firstRotation = spline.segments[0].Rotation;
            Vector3 firstTangentUpdate = firstTangentB - firstDirection * DistanceModifier(firstTangentB, disBetweenTangents);

            // Null data
            Quaternion newRotation = Quaternion.identity;
            Vector3 hitPoint = Vector3.zero;
            // Is updated inside GetProjectedRotation
            bool start = true;
            RaycastHit hit;

            // Use closest segments plane
            if (EHandleModifier.CtrlShiftActive(e))
            {
                Vector3 hit1 = new Vector3(0, 99999, 0);
                Vector3 hit2 = new Vector3(0, 99999, 0);

                Vector3 normalAxel = Vector3.up;

                Vector3 planeNormal1 = spline.GetSegmentAtIndex(spline.SegmentCount - 1).Rotation * normalAxel;
                Vector3 planePoint1 = lastAnchor;
                projectionPlane.SetNormalAndPosition(planeNormal1, planePoint1);
                if (projectionPlane.Raycast(mouseRay, out float en))
                    hit1 = mouseRay.GetPoint(en);

                Vector3 planeNormal2 = spline.GetSegmentAtIndex(0).Rotation * normalAxel;
                Vector3 planePoint2 = firstAnchor;
                projectionPlane.SetNormalAndPosition(planeNormal2, planePoint2);
                if (projectionPlane.Raycast(mouseRay, out float en2))
                    hit2 = mouseRay.GetPoint(en2);

                float dis1 = Vector3.Distance(hit1, lastAnchor);
                float dis2 = Vector3.Distance(hit2, firstAnchor);

                if (dis1 > dis2)
                {
                    newRotation = GetProjectedRotation(hit2, planeNormal2);
                    hitPoint = hit2;
                }
                else
                {
                    newRotation = GetProjectedRotation(hit1, planeNormal1);
                    hitPoint = hit1;
                }

                if(GeneralUtility.IsEqual(hit1, new Vector3(0, 99999, 0)) && GeneralUtility.IsEqual(hit1, new Vector3(0, 99999, 0)))
                    EHandleSpline.controlPointIndicatorDisabled = true;

            }
            // Raycast to collider
            else if (!is2D && !EHandleModifier.CtrlShiftActive(e) && Physics.Raycast(mouseRay, out hit))
            {
                hitPoint = hit.point;
                newRotation = GetProjectedRotation(hitPoint, hit.normal);
            }
            // Use spline transform plane
            else
            {
                Vector3 planeNormal = spline.transform.up;
                Vector3 planePoint = spline.transform.position;

                projectionPlane.SetNormalAndPosition(planeNormal, planePoint);
                if (projectionPlane.Raycast(mouseRay, out float enter))
                {
                    hitPoint = mouseRay.GetPoint(enter);
                    newRotation = GetProjectedRotation(hitPoint, planeNormal);
                }
                else
                    EHandleSpline.controlPointIndicatorDisabled = true;
            }

            // Set to closest spline direction
            if (EHandleModifier.CtrlActive(e) && !EHandleModifier.ShiftActive(e))
            {
                Spline closest = SplineUtility.GetNearestSpline(hitPoint, HandleRegistry.GetSplinesUnsafe(), 20 * DistanceModifier(hitPoint), out float time, out _);

                if (closest != null)
                {
                    closest.GetNormalNonAlloc(normalsContainer, time);
                    Vector3 f = newRotation * Vector3.forward;
                    float dot1 = Vector3.Dot(normalsContainer[2], f);
                    float dot2 = Vector3.Dot(-normalsContainer[2], f);

                    if (dot2 > dot1) normalsContainer[2] = -normalsContainer[2];

                    newRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
                }
            }

            //Set indicator data
            float newTangentDistance = DistanceModifier(hitPoint, disBetweenTangents);

            //Auto smooth
            if (EGlobalSettings.GetHandleType() == ControlHandleType.AUTO)
            {
                bool usePrev = false;
                bool useNext = false;
                Vector3 a = Vector3.zero;

                if (start && spline.SegmentCount > 1)
                {
                    NativeSegment ns = new NativeSegment(firstAnchor, firstTangentA, firstTangentB, firstRotation, false);
                    ns = SplineUtility.CalculateAutoSmoothSegment(Segment.autoSmoothTension, Segment.autoSmoothRoundness, ns, hitPoint, true, spline.segments[1].GetPosition(ControlHandle.ANCHOR), true);
                    firstTangentA = ns.tangentA;
                    firstTangentB = ns.tangentB;

                    a = ns.anchor;
                    useNext = true;
                }
                else if(spline.SegmentCount > 1)
                {
                    NativeSegment ns = new NativeSegment(lastAnchor, lastTangentA, lastTangentB, lastRotation, false);
                    ns = SplineUtility.CalculateAutoSmoothSegment(Segment.autoSmoothTension, Segment.autoSmoothRoundness, ns, spline.segments[spline.segments.Count - 2].GetPosition(ControlHandle.ANCHOR), true, hitPoint, true);
                    lastTangentA = ns.tangentA;
                    lastTangentB = ns.tangentB;

                    a = ns.anchor;
                    usePrev = true;
                }

                Vector3 ta = hitPoint + newRotation * Vector3.forward * newTangentDistance;
                Vector3 tb = hitPoint - newRotation * Vector3.forward * newTangentDistance;

                NativeSegment ns3 = new NativeSegment(hitPoint, ta, tb, newRotation, false);
                ns3 = SplineUtility.CalculateAutoSmoothSegment(Segment.autoSmoothTension, Segment.autoSmoothRoundness, ns3, a, usePrev, a, useNext);
                newRotation = ns3.rotation;
                newTangentDistance = Vector3.Distance(ns3.anchor, ns3.tangentB);
            }
            else
            {
                firstTangentA = firstTangentUpdate;
                firstTangentB = firstTangentUpdate;
                lastTangentA = lastTangentUpdate;
                lastTangentB = lastTangentUpdate;
            }

            segmentIndicatorData.anchor = start ? firstAnchor : lastAnchor;
            segmentIndicatorData.tangentA = start ? firstTangentA : lastTangentA;
            segmentIndicatorData.tangentB = start ? firstTangentB : lastTangentB;
            segmentIndicatorData.newAnchor = hitPoint;
            segmentIndicatorData.newTangentADis = newTangentDistance;
            segmentIndicatorData.newTangentBDis = newTangentDistance;
            segmentIndicatorData.newRotation = newRotation;
            segmentIndicatorData.originFromStart = start;
            segmentIndicatorData.isLine = false;

            //If line
            if ((start && spline.segments[0].GetInterpolationType() == InterpolationType.LINE) ||
               (!start && spline.segments[spline.segments.Count - 1].GetInterpolationType() == InterpolationType.LINE))
            {
                segmentIndicatorData.newTangentADis = 0.01f;
                segmentIndicatorData.newTangentBDis = 0.01f;
                segmentIndicatorData.isLine = true;
            }

            Quaternion GetProjectedRotation(Vector3 point, Vector3 normal)
            {
                float disLast = Vector3.Distance(lastAnchor + lastDirection, point);
                float disFirst = Vector3.Distance(firstAnchor - firstDirection, point);
                start = disLast > disFirst;

                Vector3 forward = (firstTangentUpdate - point).normalized;
                if (!start) forward = -(lastTangentUpdate - point).normalized;

                forward = Vector3.ProjectOnPlane(forward, normal).normalized;
                return Quaternion.LookRotation(forward, normal);
            }
        }

        internal static void UpdateIndicatorPlane(Spline spline, Event e)
        {
            bool start = false;
            PlaneType planeType = EGlobalSettings.GetPlaneType();
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);
            EHandleSpline.controlPointIndicatorDisabled = false;

            Vector3 direction = Vector3.forward;
            Vector3 anchor = Vector3.zero;
            Vector3 tangentA = Vector3.zero;
            Vector3 tangentB = Vector3.zero;
            Vector3 upDirection = spline.transform.up;

            projectionPlane.SetNormalAndPosition(upDirection, spline.transform.position);
            if (projectionPlane.Raycast(mouseRay, out float enter))
            {
                Vector3 point = mouseRay.GetPoint(enter);

                direction = spline.segments[spline.segments.Count - 1].GetDirection();
                anchor = spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.ANCHOR);
                tangentA = spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.TANGENT_A);
                tangentB = spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.TANGENT_B);

                float distanceToStart = Vector3.Distance(spline.segments[0].GetPosition(ControlHandle.ANCHOR) + direction, point);
                float distanceToEnd = Vector3.Distance(spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.ANCHOR) - direction, point);
                start = distanceToStart < distanceToEnd;

                if (spline.segments.Count == 1)
                    start = !start;

                if (start)
                {
                    direction = spline.segments[0].GetDirection();
                    anchor = spline.segments[0].GetPosition(ControlHandle.ANCHOR);
                    tangentA = spline.segments[0].GetPosition(ControlHandle.TANGENT_A);
                    tangentB = spline.segments[0].GetPosition(ControlHandle.TANGENT_B);
                }
            }

            projectionPlane.SetNormalAndPosition(upDirection, anchor);
            if (projectionPlane.Raycast(mouseRay, out float enter2))
            {
                Vector3 point = mouseRay.GetPoint(enter2);

                if(planeType == PlaneType.GRID)
                    point = EHandlePlane.SnapToGrid(spline, point);

                Vector3 direction90 = Vector3.Cross(direction, upDirection);
                Vector3 closestPoint = Utility.LineUtility.GetNearestPoint(anchor, direction, point, out _);
                Utility.LineUtility.GetNearestPoint(anchor, direction90, point, out float time);
                float sign = Mathf.Sign(time) * (start ? -1 : 1);
                direction90 = direction90 * sign;

                segmentIndicatorData.gridSpline = spline;
                segmentIndicatorData.anchor = anchor;
                segmentIndicatorData.tangentA = tangentA;
                segmentIndicatorData.tangentB = tangentB;
                segmentIndicatorData.newAnchor = point;
                segmentIndicatorData.originFromStart = start;

                float dis = EGlobalSettings.GetNewSegmentTangentDistance();

                if (planeType == PlaneType.GRID)
                    dis = EHandlePlane.SnapToGridByDistance(dis);

                segmentIndicatorData.newTangentADis = dis;
                segmentIndicatorData.newTangentBDis = dis;
                if (GeneralUtility.IsEqual(closestPoint, point, DistanceModifier(point) * 4))
                    segmentIndicatorData.newRotation = Quaternion.LookRotation(direction, upDirection);
                else
                    segmentIndicatorData.newRotation = Quaternion.LookRotation(direction90, upDirection);

                if ((start && spline.segments[0].GetInterpolationType() == InterpolationType.LINE) ||
                    (!start && spline.segments[spline.segments.Count - 1].GetInterpolationType() == InterpolationType.LINE))
                {
                    segmentIndicatorData.newTangentADis = 0.01f;
                    segmentIndicatorData.newTangentBDis = 0.01f;
                }
            }
            else
                EHandleSpline.controlPointIndicatorDisabled = true;
        }

        internal static void HandleDeletion(Spline spline, Event e)
        {
            if (EHandleModifier.DeleteActive(e))
            {
                MarkForDeletion(SplineUtility.ControlPointIdToSegmentIndex(EHandleSelection.SelectedControlPoint));

                EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
                {
                    foreach (int i in undoState.selectedControlPoints)
                    {
                        MarkForDeletion(SplineUtility.ControlPointIdToSegmentIndex(i));
                    }

                    DeleteAndUnlinkMarked(spline, true);
                    undoState.selectedControlPoints.Clear();
                });
            }


            //Dont delete Selection.activeTransform if controlHandle is selected.
            if (EHandleSelection.SelectedControlPoint > 0 && e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
                e.Use();

#if UNITY_EDITOR_OSX
            //Dont delete Selection.activeTransform if controlHandle is selected.
            if (EHandleSelection.SelectedControlPoint > 0 && e.command && e.keyCode == KeyCode.Backspace && e.type == EventType.KeyDown)
                e.Use();
#endif
        }

        public static void MarkForDeletion(int segment)
        {
            if (markedSegments.Contains(segment))
                return;

            markedSegments.Add(segment);
        }

        public static void DeleteAndUnlinkMarked(Spline spline, bool updateControlPointSelection)
        {
            markedSegments.Sort();

            for (int i = markedSegments.Count - 1; i >= 0; i--)
            {
                int segmentIndex = markedSegments[i];

                if (segmentIndex < 0 || segmentIndex >= spline.segments.Count)
                    continue;

                Segment s = spline.segments[segmentIndex];

                if(s.linkTarget != LinkTarget.NONE)
                {
                    if(s.LinkCount > 0)
                    {
                        //Unlink on other segments
                        for (int i2 = 0; i2 < s.LinkCount; i2++)
                        {
                            Segment s2 = s.GetLinkAtIndex(i2);

                            if (s2 == s)
                                continue;

                            if (s2.LinkCount > 2)
                                continue;

                            EHandleUndo.RecordNow(s2.SplineParent, "Delete segement: " + segmentIndex);
                            s2.linkTarget = LinkTarget.NONE;
                        }
                    }

                    if(s.SplineConnector != null)
                    {
                        s.SplineConnector.RemoveConnection(s);
                    }
                }

                //If deleteing an Spline very fast after selecting it, the segement will be -333 and it will go into this if statement if "segement >= 0" is not here.
                //In this case the Spline should be deleted.
                if (spline.segments.Count > 1 && segmentIndex >= 0)
                {
                    EHandleEvents.InvokeBeforeSegmentRemoved(s);

                    EHandleUndo.RecordNow(spline, "Delete segement: " + segmentIndex);
                    if (segmentIndex > spline.segments.Count - 1)
                        spline.RemoveSegmentAt(spline.segments.Count - 1);
                    else
                        spline.RemoveSegmentAt(segmentIndex);

                    if (spline.Loop)
                    {
                        if(segmentIndex == 0)
                        {
                            spline.segments[spline.segments.Count - 1].SetPosition(ControlHandle.ANCHOR, spline.segments[0].GetPosition(ControlHandle.ANCHOR));
                            spline.segments[spline.segments.Count - 1].SetPosition(ControlHandle.TANGENT_A, spline.segments[0].GetPosition(ControlHandle.TANGENT_A));
                            spline.segments[spline.segments.Count - 1].SetPosition(ControlHandle.TANGENT_B, spline.segments[0].GetPosition(ControlHandle.TANGENT_B));
                        }

                        if (spline.segments.Count == 2)
                        {
                            spline.RemoveSegmentAt(spline.segments.Count - 1);
                            spline.SetLoop(false, false);
                        }
                    }

                    if (updateControlPointSelection)
                    {
                        EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
                        {
                            //If last selected cp was deleted and the spline is looped we need to select the second last cp.
                            if (spline.Loop && segmentIndex >= spline.segments.Count - 1)
                                undoState.selectedControlPoint = SplineUtility.SegmentIndexToControlPointId(spline.segments.Count - 2, ControlHandle.ANCHOR);
                            else if (segmentIndex == 0)
                                undoState.selectedControlPoint = SplineUtility.SegmentIndexToControlPointId(0, ControlHandle.ANCHOR);
                            else if (segmentIndex < spline.segments.Count && segmentIndex > 0)
                                undoState.selectedControlPoint = SplineUtility.SegmentIndexToControlPointId(segmentIndex, ControlHandle.ANCHOR);
                            else if (segmentIndex >= spline.segments.Count - 1)
                                undoState.selectedControlPoint = SplineUtility.SegmentIndexToControlPointId(spline.segments.Count - 1, ControlHandle.ANCHOR);
                        }, "Delete segement: " + segmentIndex);
                    }

                    EHandleEvents.InvokeAfterSegmentRemoved(spline);
                }
                else
                {
                    EHandleUndo.RecordNow(spline);
                    spline.RemoveSegmentAt(0);

                    EHandleUndo.MarkSplineForDestroy(spline);
                }
            }

            EHandleTool.ActivatePositionToolForControlPoint(spline);
            WindowBase.RepaintAll();
            markedSegments.Clear();
        }

        public static void HandleLinking(Spline spline)
        {
            for (int i = 0; i < spline.segments.Count; i++)
            {
                Segment s = spline.segments[i];

                if (s.linkTarget != s.oldLinkTarget)
                {
                    s.oldLinkTarget = s.linkTarget;

                    //In unity 2022 when appying changes to a prefab, the spline parent will be null.
                    s.splineParent = spline;

                    if (s.linkTarget == LinkTarget.ANCHOR)
                    {
                        s.LinkToSegment(s.GetPosition(ControlHandle.ANCHOR));
                    }
                    else if (s.linkTarget == LinkTarget.SPLINE_CONNECTOR)
                    {
                        s.LinkToConnector(s.GetPosition(ControlHandle.ANCHOR));
                    }
                    else
                    {
                        s.Unlink();
                    }
                }
            }
        }

        public static void GetIndicatorExtendedData(Spline spline, Vector3 startLinePos, Vector3 endLinePos, Ray mouseRay, out Vector3 closestPoint, out float distance, out float time)
        {
            float lineLength = Vector3.Distance(spline.segments[0].GetPosition(ControlHandle.ANCHOR), spline.segments[0].GetPosition(ControlHandle.TANGENT_B));
            Vector3 startDirection = -spline.segments[0].GetDirection();
            Vector3 newClosestPoint = Utility.LineUtility.GetNearestPointOnLineFromLine(startLinePos, -startDirection, mouseRay.origin, mouseRay.direction, lineLength, true);
            distance = EMouseUtility.MouseDistanceToPoint(newClosestPoint, mouseRay);
            closestPoint = newClosestPoint;
            time = 0;

            lineLength = Vector3.Distance(spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.ANCHOR), spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.TANGENT_A));
            Vector3 endDirection = -spline.segments[spline.segments.Count - 1].GetDirection();
            newClosestPoint = Utility.LineUtility.GetNearestPointOnLineFromLine(endLinePos, endDirection, mouseRay.origin, mouseRay.direction, lineLength, true);
            float distanceToExtendedEnd = EMouseUtility.MouseDistanceToPoint(newClosestPoint, mouseRay);
            if (distanceToExtendedEnd < distance)
            {
                distance = distanceToExtendedEnd;
                closestPoint = newClosestPoint;
                time = 1;
            }
        }

        public static void CreateWithAutoSmooth(Spline spline, float time)
        {
            EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
            {
                undoState.selectedControlPoints.Clear();
                EHandleUndo.RecordNow(spline, "Create segement: " + spline.indicatorSegment);
                Segment segment = spline.CreateSegmentAutoSmooth(time);
                undoState.selectedControlPoint = SplineUtility.SegmentIndexToControlPointId(segment.IndexInSpline, ControlHandle.ANCHOR);
            });
        }

        public static void CreateFromWorldPoint(Spline spline)
        {
            // New segment id
            int segementId = 0;
            if (!segmentIndicatorData.originFromStart) segementId = spline.segments.Count;

            // Record undo
            EHandleUndo.RecordNow(spline, "Create segement: " + spline.indicatorSegment, EHandleUndo.RecordType.REGISTER_COMPLETE_OBJECT);

            // Clear selection
            EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
            {
                undoState.selectedControlPoints.Clear();
            });

            // Create segment
            Vector3 point = segmentIndicatorData.newAnchor;
            float tADis = segmentIndicatorData.newTangentADis;
            float tBDis = segmentIndicatorData.newTangentBDis;
            Quaternion rotation = segmentIndicatorData.newRotation;
            spline.CreateSegment(segementId, point, tADis, tBDis, rotation);

            EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
            {
                undoState.selectedControlPoint = SplineUtility.SegmentIndexToControlPointId(segementId, ControlHandle.ANCHOR);
            }, "Create segement: " + spline.indicatorSegment);

            // Set previus tangent
            if (!segmentIndicatorData.originFromStart)
            {
                if (EGlobalSettings.GetHandleType() == ControlHandleType.AUTO)
                {
                    spline.segments[spline.segments.Count - 2].SetPosition(ControlHandle.TANGENT_B, segmentIndicatorData.tangentB);
                    spline.segments[spline.segments.Count - 2].SetPosition(ControlHandle.TANGENT_A, segmentIndicatorData.tangentA);
                }
                else
                {
                    spline.segments[spline.segments.Count - 2].SetPosition(ControlHandle.TANGENT_A, segmentIndicatorData.tangentA);
                }
            }
            else
            {
                if (EGlobalSettings.GetHandleType() == ControlHandleType.AUTO)
                {
                    spline.segments[1].SetPosition(ControlHandle.TANGENT_B, segmentIndicatorData.tangentB);
                    spline.segments[1].SetPosition(ControlHandle.TANGENT_A, segmentIndicatorData.tangentA);
                }
                else
                {
                    spline.segments[1].SetPosition(ControlHandle.TANGENT_B, segmentIndicatorData.tangentB);
                }
            }
        }

        public static void CreateExtended(Spline spline, bool createAtStart)
        {
            EHandleUndo.RecordNow(spline, "Create segement: " + spline.indicatorSegment, EHandleUndo.RecordType.REGISTER_COMPLETE_OBJECT);
            EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
            {
                undoState.selectedControlPoints.Clear();
            });

            if (createAtStart)
            {
                Vector3 firstAnchor = spline.segments[0].GetPosition(ControlHandle.ANCHOR);
                Vector3 correctedTangentA = firstAnchor - (firstAnchor - spline.indicatorPosition) / 2;
                spline.GetSegmentAtIndex(0).SetPosition(ControlHandle.TANGENT_B, correctedTangentA);

                Vector3 anchor = spline.indicatorPosition;
                Vector3 tangentA = correctedTangentA;
                Vector3 tangentB = spline.indicatorPosition + (spline.indicatorDirection * DistanceModifier(spline.indicatorPosition) * 12);

                float tADis = Vector3.Distance(anchor, tangentA);
                float tBDis = Vector3.Distance(anchor, tangentB);

                spline.CreateSegment(0, anchor, tADis, tBDis, spline.GetSegmentAtIndex(0).Rotation);
                EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
                {
                    undoState.selectedControlPoint = SplineUtility.SegmentIndexToControlPointId(0, ControlHandle.ANCHOR);
                }, "Create segement: " + spline.indicatorSegment);
            }
            else
            {
                Vector3 lastAnchor = spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.ANCHOR);
                Vector3 correctedTangentB =  lastAnchor - (lastAnchor - spline.indicatorPosition) / 2;
                spline.GetSegmentAtIndex(spline.segments.Count - 1).SetPosition(ControlHandle.TANGENT_A, correctedTangentB);

                Vector3 anchor = spline.indicatorPosition;
                Vector3 tangentA = spline.indicatorPosition - (spline.indicatorDirection * DistanceModifier(spline.indicatorPosition) * 12);
                Vector3 tangentB = correctedTangentB;

                float tADis = Vector3.Distance(anchor, tangentA);
                float tBDis = Vector3.Distance(anchor, tangentB);

                spline.CreateSegment(spline.segments.Count, anchor, tADis, tBDis, spline.GetSegmentAtIndex(spline.segments.Count - 1).Rotation);
                EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
                {
                    undoState.selectedControlPoint = SplineUtility.SegmentIndexToControlPointId(spline.segments.Count - 1, ControlHandle.ANCHOR);
                }, "Create segement: " + spline.indicatorSegment);
            }
        }

        public static void CreateSegmentsFromEditorCameraDirection(Spline spline)
        {
            Transform editorCamera = EHandleSceneView.GetCurrent().camera.transform;
            Quaternion rotation = Quaternion.LookRotation(editorCamera.transform.right, -editorCamera.transform.forward);
            spline.transform.position = editorCamera.position + editorCamera.forward * 50;
            spline.transform.rotation = rotation;
            Vector3 anchor1 = spline.transform.position - editorCamera.transform.right * 12;
            Vector3 anchor2 = spline.transform.position + editorCamera.transform.right * 12;

            spline.CreateSegment(0, anchor1, 5, 5, rotation);
            spline.CreateSegment(1, anchor2, 5, 5, rotation);

            spline.RebuildCache();
            EHandleTool.ActivatePositionToolForControlPoint(spline);
        }

        public static float DistanceModifier(Vector3 position, float strength = 1)
        {
            SceneView sceneView = EHandleSceneView.GetCurrent();
            if (sceneView == null) return 1;

            if (sceneView.orthographic)
            {
                float orthoSize = sceneView.camera.orthographicSize;
                return 0.0133f * orthoSize * strength;
            }
            else
            {
                float distance = Vector3.Distance(sceneView.camera.transform.position, position);
                return 0.0066f * distance * strength;
            }
        }

        public static float GetControlPointSize(Vector3 position)
        {
            float controlPointSize = EGlobalSettings.GetControlPointSize();

            SceneView sceneView = EHandleSceneView.GetCurrent();
            if (sceneView == null || sceneView.camera == null)
                return controlPointSize;

            Camera camera = sceneView.camera;

            if (camera.orthographic)
            {
                controlPointSize *= 0.0133f * camera.orthographicSize;
            }
            else
            {
                Vector3 cameraSpacePosition = camera.worldToCameraMatrix.MultiplyPoint(position);
                float distance = Mathf.Abs(cameraSpacePosition.z);

                float controlPointScaleDistance = EGlobalSettings.GetControlPointScaleDistance();
                distance = Mathf.Min(distance, controlPointScaleDistance);

                controlPointSize *= 0.0066f * distance;
            }

            return controlPointSize;
        }
    }
}
