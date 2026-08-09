// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleSelection.cs
//
// Author: Mikael Danielsson
// Date Created: 06-10-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

using SplineArchitect.CustomTools;
using SplineArchitect.Ui;
using SplineArchitect.Utility;
using SplineArchitect.ScriptableObjects;

namespace SplineArchitect
{
    public static class EHandleSelection
    {
        //Selection Spline
        public static Spline selectedSpline { get; private set; }
        public static HashSet<Spline> selectedSplines { get; private set; } = new HashSet<Spline>();
        public static Spline hoveredSpline { get; private set; }
        private static Spline oldHoveredSpline;
        private static List<Object> selection = new List<Object>();

        //Selection SplineObject
        public static SplineObject selectedSplineObject;
        public static List<SplineObject> selectedSplineObjects = new List<SplineObject>();

        //Selection ControlPoint
        private static SelectionUndoState selectionUndoState;
        public static int hoveredCp { get; private set; }
        private static int oldHoveredCp;

        //Selected SplineConnector
        public static SplineConnector selectedSplineConnector { get; private set; }
        public static HashSet<SplineConnector> selectedSplineConnectors = new HashSet<SplineConnector>();

        //General
        public static bool stopNextUpdateSelection { private get; set; } = false;
        public static bool stopUpdateSelection { private get; set; } = false;
        private static Ray mouseRay;
        private static bool assemblyReload = true;
        private static bool markForForceUpdate = false;
        private static Dictionary<Segment, Vector3Int> selectedSegmentContainer1 = new Dictionary<Segment, Vector3Int>();
        private static Dictionary<Segment, Vector3Int> selectedSegmentContainer2 = new Dictionary<Segment, Vector3Int>();
        private static Dictionary<Segment, Vector3Int> selectedSegmentContainer3 = new Dictionary<Segment, Vector3Int>();

        internal static int SelectedControlPoint
        {
            get
            {
                if (selectionUndoState == null)
                    return 0;

                return selectionUndoState.selectedControlPoint;
            }
        }
        internal static int SelectedControlPointsCount
        {
            get
            {
                if (selectionUndoState == null)
                    return 0;

                return selectionUndoState.selectedControlPoints.Count;
            }
        }

        internal static void AfterAssemblyReload()
        {
            selectionUndoState = ScriptableObject.CreateInstance<SelectionUndoState>();
            selectionUndoState.hideFlags = HideFlags.HideAndDontSave;
        }

        internal static void BeforeSceneGUIGlobal(SceneView sceneView, Event e)
        {
            //Need to check this in unity 2022, else we get errors only while creating splines in 2D.
            if (EHandleSpline.controlPointCreationActive)
                return;

            // Position tool
            PositionTool.UpdateHoveredData(e, EMouseUtility.GetMouseRay(e.mousePosition));
            if (e.type == EventType.MouseDown && e.button == 0 && !EHandleModifier.AltActive(e))
            {
                if (PositionTool.Press(e, EMouseUtility.GetMouseRay(e.mousePosition)))
                {
#if UNITY_6000_0_OR_NEWER
                    e.Use();
#endif
                }
            }

            // Quick move tool
            if (QuickMoveTool.IsUpdating(e))
                return;

            // Box select tool
            if (BoxSelectTool.IsUpdating(e))
                return;

            if (GUIUtility.hotControl == 0)
            {
                bool hovering = false;

                if (TryUpdateHoveredControlPoint(e, selectedSpline, sceneView))
                    hovering = true;

                if (!hovering && TryUpdateHoveredSpline(e, HandleRegistry.GetSplinesUnsafe(), EHandleModifier.CtrlActive(e)))
                    hovering = true;
            }
            else
            {
                hoveredCp = 0;
                hoveredSpline = null;
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                TrySelectHoveredControlPoint(selectedSpline, e, EHandleModifier.CtrlActive(e));
                TrySelectHoveredSpline(e, EHandleModifier.CtrlActive(e));
            }

            if (e.type == EventType.Layout)
            {
                if (EHandleEvents.updateSelection)
                {
                    EHandleEvents.updateSelection = false;
                    ForceUpdate();
                }
            }

            if (assemblyReload)
            {
                assemblyReload = false;
                //This is needed else Look rotation viewing vector is zero after assembly reload.
                EHandleTool.ActivatePositionToolForControlPoint(selectedSpline);
            }

            if(markForForceUpdate)
            {
                markForForceUpdate = false;
                ForceUpdate();
            }
        }

        internal static void OnSelectionChange()
        {
            UpdateSelection(TryGetSelectionTransform());
        }

#if UNITY_6000_4_OR_NEWER
        internal static void OnHierarchyGUI(EntityId id, Rect selectionRect)
#else
        internal static void OnHierarchyGUI(int id, Rect selectionRect)
#endif
        {
            if (Event.current != null && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (selectionRect.Contains(Event.current.mousePosition))
                {
#if UNITY_6000_3_OR_NEWER
                    GameObject go = EditorUtility.EntityIdToObject(id) as GameObject;
#else
                    GameObject go = EditorUtility.InstanceIDToObject(id) as GameObject;
#endif

                    if (go == null || go.transform == null)
                        return;

                    Spline spline = go.transform.GetComponent<Spline>();

                    if (spline == null)
                        return;

                    //Dont record undo when no spline was selected. Else deformations/followers will get the wrong position after doing ctrl + z.
                    if(selectedSpline == null)
                    {
                        selectionUndoState.selectedControlPoints.Clear();
                        selectionUndoState.selectedControlPoint = 0;
                    }
                    else
                    {
                        //Also need to record the transform becouse of transform.position change
                        EHandleUndo.RecordNow(spline.transform, "Selected spline");
                        EHandleUndo.RecordNow(selectionUndoState, "Selected spline");
                        selectionUndoState.selectedControlPoints.Clear();
                        selectionUndoState.selectedControlPoint = 0;
                    }

                    EActionDelayed.Add(() =>
                    {
                        WindowBase.RepaintAll();
                    }, 0, 0, EActionDelayed.ActionFlag.FRAMES | EActionDelayed.ActionFlag.LATE);
                }
            }
            else if(Event.current != null && Event.current.type == EventType.DragPerform)
            {
                markForForceUpdate = true;
            }
        }

        internal static void OnUndo()
        {
            Transform selection = TryGetSelectionTransform();

            if (selection == null)
                return;

            UpdateSelection(selection);
        }

        private static void UpdateSelection(Transform newSelection)
        {
            WindowBase.RepaintAll();

            if (stopNextUpdateSelection)
            {
                stopNextUpdateSelection = false;
                return;
            }

            if (stopUpdateSelection)
                return;

            SplineObject oldSelectedSplineObject = selectedSplineObject;

            selectedSpline = null;
            selectedSplines.Clear();
            selectedSplineObject = null;
            selectedSplineObjects.Clear();
            selectedSplineConnector = null;
            selectedSplineConnectors.Clear();
            EHandleEvents.selectedSpline = null;
            EHandleEvents.isSplineObjectSelected = false;
            EHandleEvents.isSplineConnectorSelected = false;

            if (newSelection == null)
                return;

            Spline spline = TryFindSpline(newSelection);
            SplineObject so = newSelection.GetComponent<SplineObject>();
            SplineConnector sc = newSelection.GetComponent<SplineConnector>();

            //Select Spline
            if (so == null)
            {
                if (spline != null && spline.IsEnabled())
                {
                    selectedSpline = spline;
                    EHandleEvents.selectedSpline = selectedSpline;
                }

                foreach (Object o in Selection.objects)
                {
                    GameObject go = o as GameObject;

                    if(go == null)
                        continue;

                    Spline spline2 = go.GetComponent<Spline>();

                    if (spline2 != null && spline2 != selectedSpline)
                    {
                        if (selectedSplines.Contains(spline2))
                            continue;

                        selectedSplines.Add(spline2);
                    }
                }
            }
            //Select SplineObject
            else if (so != null)
            {
                if (!so.enabled)
                    return;

                //Inactive SplineObjects can be created using scripts. We should not select them if the user trys to.
                if (so.SplineParent != null && !so.SplineParent.ContainsSplineObject(so))
                    return;

                selectedSplineObject = so;
                EHandleEvents.isSplineObjectSelected = true;

                if (spline != null)
                {
                    selectedSpline = spline;
                    EHandleEvents.selectedSpline = selectedSpline;

                    if (!EHandleUndo.UndoTriggered() && oldSelectedSplineObject == null)
                    {
                        //Needs to deslect becouse if the user select an object in the hirarcy menu.
                        UpdateSelectedControlPointsRecordUndo((undoState) =>
                        {
                            undoState.selectedControlPoints.Clear();
                            undoState.selectedControlPoint = 0;
                        });
                    }

                    if (spline.segments.Count > 1)
                        EHandleTool.ActivatePositionToolForSplineObject(spline, so);
                }

                foreach (Object o in Selection.objects)
                {
                    GameObject go = o as GameObject;

                    if (go == null)
                        continue;

                    SplineObject so2 = go.GetComponent<SplineObject>();

                    if (so2 != null && so2 != selectedSplineObject)
                    {
                        selectedSplineObjects.Add(so2);

                        if (so2.SplineParent == null)
                            continue;

                        if(!selectedSplines.Contains(so2.SplineParent) && so2.SplineParent != selectedSpline)
                            selectedSplines.Add(so2.SplineParent);
                    }
                }
            }

            if(sc != null)
            {
                selectedSplineConnector = sc;
                EHandleEvents.isSplineConnectorSelected = true;

                foreach (Object o in Selection.objects)
                {
                    GameObject go = o as GameObject;

                    if (go == null)
                        continue;

                    SplineConnector sc2 = go.GetComponent<SplineConnector>();
                    if (sc2 != null && sc2 != selectedSplineConnector)
                        selectedSplineConnectors.Add(sc2);
                }
            }

            Spline TryFindSpline(Transform transform)
            {
                Spline spline2 = transform.GetComponent<Spline>();

                for (int i = 0; i < 25; i++)
                {
                    if (spline2 != null)
                        return spline2;

                    if (transform.parent == null)
                        break;

                    transform = transform.parent;
                    spline2 = transform.GetComponent<Spline>();
                }

                return null;
            }
        }

        internal static bool TryUpdateHoveredSpline(Event e, HashSet<Spline> splines, bool multiselectActive)
        {
            hoveredSpline = null;

            if (EGlobalSettings.GetSplineHideMode() > 0)
                return false;

            if (EHandleSpline.controlPointCreationActive)
                return false;

            mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);
            float closestDistance = 99999999;
            Vector3 mousePoint = Vector3.zero;
            Spline hovered = null;

            foreach (Spline spline in splines)
            {
                if (spline == null)
                    continue;

                if (spline.transform == null)
                    continue;

                if (!spline.IsEnabled())
                    continue;

                if (spline.segments.Count < 2)
                    continue;

                if (spline.IsPickingDisabled() || spline.IsHiddenInSceneView())
                    continue;

                //If selected spline
                if (spline == selectedSpline)
                {
                    //Skip if ctrl is not hold down
                    if (!multiselectActive)
                        continue;

                    //Skip if so is selected
                    if (selectedSplineObject != null && selectedSplineObject.SplineParent == spline)
                        continue;

                    //Skip if control point is selected
                    if (selectedSpline == spline && SelectedControlPoint != 0)
                        continue;
                }

                if (!spline.bounds.IntersectRay(mouseRay))
                    continue;

                mousePoint = EHandleSpline.ClosestMousePoint(spline, mouseRay, 12, out float distance, out float time, 20);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    hovered = spline;

                    float distanceCheck = EHandleSegment.DistanceModifier(mousePoint) * (0.3f + (spline.Width * 0.1f));
                    if (distanceCheck < 0.025f) distanceCheck = 0.025f;

                    if (closestDistance < distanceCheck)
                    {
                        hoveredSpline = hovered;
                    }
                }
            }

            if (oldHoveredSpline != hoveredSpline)
            {
                EHandleEvents.InvokeAfterSplineHovered(hoveredSpline);
                oldHoveredSpline = hoveredSpline;
                EHandleSceneView.RepaintCurrent();
            }

            if (hoveredSpline != null)
                return true;

            return false;
        }

        internal static void TrySelectHoveredSpline(Event e, bool multiselectActive)
        {
            if (hoveredSpline == null)
                return;

            if (multiselectActive)
            {
                selection.Clear();
                selection.AddRange(Selection.objects);

                if (selectedSpline == hoveredSpline || selectedSplines.Contains(hoveredSpline))
                    selection.Remove(hoveredSpline.gameObject);
                else
                {
                    selection.Add(hoveredSpline.gameObject);
                    Selection.activeTransform = hoveredSpline.transform;
                }

                Selection.objects = selection.ToArray();
            }
            else
            {
                Selection.objects = null;
                Selection.activeTransform = hoveredSpline.transform;
            }

            e.Use();

            UpdateSelectedControlPointsRecordUndo((undoState) =>
            {
                undoState.selectedControlPoints.Clear();
                undoState.selectedControlPoint = 0;
            }, "Select Spline");
        }

        internal static bool TryUpdateHoveredControlPoint(Event e, Spline spline, SceneView sceneView)
        {
            if (spline == null)
                return false;

            hoveredCp = 0;
            Segment hoveredSegment = null;
            ControlHandle hoveredControlHandle = ControlHandle.NONE;

            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);

            //Bounds for control points
            if (!spline.controlPointsBounds.IntersectRay(mouseRay))
                return false;

            Vector3 editorCameraPosition = sceneView.camera.transform.position;
            List<int> intersectingControlPoint = EHandleSpline.GetIntersectingControlPoints(spline, mouseRay);

            float distanceCheck = 999999;

            foreach(int i in intersectingControlPoint)
            {
                int segmentId = SplineUtility.ControlPointIdToSegmentIndex(i);
                ControlHandle controlHandle = SplineUtility.GetControlHandleType(i);
                InterpolationType interpolationMode = spline.segments[segmentId].GetInterpolationType();

                if ((EGlobalSettings.GetHandleType() == ControlHandleType.AUTO || interpolationMode == InterpolationType.LINE) && controlHandle != ControlHandle.ANCHOR)
                    continue;

                float distanceToCamera = Vector3.Distance(spline.segments[segmentId].GetPosition(controlHandle), editorCameraPosition);

                if (distanceToCamera < distanceCheck)
                {
                    distanceCheck = distanceToCamera;
                    hoveredCp = i;
                    hoveredSegment = spline.segments[segmentId];
                    hoveredControlHandle = controlHandle;
                }
            }

            if (oldHoveredCp != hoveredCp)
            {
                EHandleEvents.InvokeAfterSegmentHovered(hoveredSegment, hoveredControlHandle);
                oldHoveredCp = hoveredCp;
                hoveredSpline = null;
                EHandleSceneView.RepaintCurrent();
            }

            if (hoveredCp != 0)
                return true;

            return false;
        }

        internal static void TrySelectHoveredControlPoint(Spline spline, Event e, bool multiselectActive)
        {
            if (hoveredCp == 0)
                return;

            if (spline == null || !spline.IsEnabled())
                return;

            UpdateSelectedControlPointsRecordUndo((undoState) =>
            {
                if (multiselectActive)
                {
                    if (hoveredCp == SelectedControlPoint)
                    {
                        if (SelectedControlPointsCount == 0)
                            undoState.selectedControlPoint = 0;
                        else
                        {
                            undoState.selectedControlPoint = undoState.selectedControlPoints[0];
                            undoState.selectedControlPoints.RemoveAt(0);
                        }
                    }
                    else if (undoState.selectedControlPoints.Contains(hoveredCp))
                        undoState.selectedControlPoints.Remove(hoveredCp);
                    else if (!undoState.selectedControlPoints.Contains(undoState.selectedControlPoint))
                    {
                        undoState.selectedControlPoints.Add(undoState.selectedControlPoint);
                        undoState.selectedControlPoint = hoveredCp;
                    }
                }
                else
                {
                    undoState.selectedControlPoint = hoveredCp;
                    undoState.selectedControlPoints.Clear();
                }

                Selection.objects = null;
                Selection.activeTransform = spline.transform;

                selectedSplines.Clear();
                selectedSplineObjects.Clear();
                selectedSplineObject = null;

                //Inactivate the next mouseDown and mouseUp event DuringSceneGUI.
                e.Use();

                EHandleTool.ActivatePositionToolForControlPoint(spline);

                WindowBase.RepaintAll();
            }, "Selected/Deselect control point: " + hoveredCp);
        }

        public static Transform TryGetSelectionTransform()
        {
            if (Selection.activeTransform != null)
            {
                return Selection.activeTransform;
            }
            else if (Selection.activeObject != null)
            {
                GameObject gameObject = Selection.activeObject as GameObject;
                if (gameObject != null && gameObject.transform != null)
                {
                    return gameObject.transform;
                }
            }

            return null;
        }

        public static void MarkForForceUpdate()
        {
            markForForceUpdate = true;
        }

        public static void ForceUpdate()
        {
            UpdateSelection(TryGetSelectionTransform());
        }

        //public static void SelectPrimaryControlPoint(Spline spline, int controlpointId)
        //{
        //    selectedSplineObject = null;
        //    selectedSplineObjects.Clear();
        //    Selection.activeTransform = spline.transform;
        //    spline.selectedControlPoint = controlpointId;
        //    EHandleTool.ActivatePositionToolForControlPoint(spline);
        //}

        //public static void SelectSecondaryAnchors(Spline spline, int[] anchors)
        //{
        //    spline.selectedControlPoints.Clear();
        //    foreach (int a in anchors)
        //    {
        //        spline.selectedControlPoints.Add(a);
        //    }
        //}

        public static bool IsPrimiarySelection(Segment segment)
        {
            if (segment == null)
                return false;

            if (selectedSpline == null)
                return false;

            if (SelectedControlPoint == 0)
                return false;

            Segment selectedSegment = selectedSpline.segments[SplineUtility.ControlPointIdToSegmentIndex(SelectedControlPoint)];

            return selectedSegment == segment;
        }

        public static bool IsSecondarySelection(Segment segment)
        {
            if (segment == null)
                return false;

            if (selectedSpline == null)
                return false;

            if (SelectedControlPointsCount <= 0)
                return false;

            foreach (int controlPointId in selectionUndoState.selectedControlPoints)
            {
                int segmentIndex = SplineUtility.ControlPointIdToSegmentIndex(controlPointId);

                if (selectedSpline.segments.Count <= segmentIndex)
                    continue;

                Segment selectedSegment = selectedSpline.segments[segmentIndex];

                if (selectedSegment == segment)
                    return true;
            }

            return false;
        }

        public static bool IsPrimiarySelection(Spline spline)
        {
            if (spline == selectedSpline)
                return true;

            return false;
        }

        public static bool IsSecondarySelection(Spline spline)
        {
            if (selectedSplines.Contains(spline))
                return true;

            return false;
        }

        public static bool IsConnectedToSelection(Spline spline)
        {
            if(selectedSpline != null)
            {
                if (spline == selectedSpline)
                    return false;

                foreach(Segment s in selectedSpline.segments)
                {
                    if (s.linkTarget != LinkTarget.ANCHOR || s.LinkCount == 0)
                        continue;

                    for (int i = 0; i < s.LinkCount; i++)
                    {
                        Segment link = s.GetLinkAtIndex(i);

                        if (link.splineParent == selectedSpline)
                            continue;

                        if (link.splineParent == spline)
                        {
                            return true;
                        }
                    }
                }
            }

            foreach(Spline spline2 in selectedSplines)
            {
                if (spline == spline2)
                    return false;

                foreach (Segment s in spline2.segments)
                {
                    if (s.linkTarget != LinkTarget.ANCHOR || s.LinkCount == 0)
                        continue;

                    for (int i = 0; i < s.LinkCount; i++)
                    {
                        Segment link = s.GetLinkAtIndex(i);

                        if (link.splineParent == spline2)
                            continue;

                        if (link.splineParent == spline)
                            return true;
                    }
                }
            }
            
            if(selectedSplineConnector != null)
            {
                for (int i = 0; i < selectedSplineConnector.ConnectionCount; i++)
                {
                    Segment s = selectedSplineConnector.GetConnectionAtIndex(i);
                    if (s.splineParent == spline)
                        return true;
                }
            }

            if (selectedSplineConnectors.Count > 0)
            {
                foreach (SplineConnector sc in selectedSplineConnectors)
                {
                    for (int i = 0; i < sc.ConnectionCount; i++)
                    {
                        Segment s = sc.GetConnectionAtIndex(i);
                        if (s.splineParent == spline)
                            return true;
                    }
                }
            }

            return false;
        }

        public static bool IsChildOfSelected(Spline spline)
        {
            Transform selected = Selection.activeTransform;

            if (IsAncestorOfTransform(spline.transform.parent, selected))
                return true;

            return false;

            bool IsAncestorOfTransform(Transform ancestor, Transform transform)
            {
                if (transform == null)
                    return false;

                for (int i = 0; i < 25; i++)
                {
                    if (ancestor == null)
                        break;

                    if (transform == ancestor)
                        return true;

                    ancestor = ancestor.parent;
                }

                return false;
            }
        }

        public static ControlHandle IsHovering(int segmentIndex)
        {
            if(SplineUtility.SegmentIndexToControlPointId(segmentIndex, ControlHandle.ANCHOR) == hoveredCp)
                return ControlHandle.ANCHOR;
            else if (SplineUtility.SegmentIndexToControlPointId(segmentIndex, ControlHandle.TANGENT_A) == hoveredCp)
                return ControlHandle.TANGENT_A;
            else if (SplineUtility.SegmentIndexToControlPointId(segmentIndex, ControlHandle.TANGENT_B) == hoveredCp)
                return ControlHandle.TANGENT_B;

            return ControlHandle.NONE;
        }

        public static void UpdatedSelectedSplinesRecordUndo(Action<Spline> action, string recordName, EHandleUndo.RecordType recordType = EHandleUndo.RecordType.RECORD_OBJECT)
        {
            EHandleUndo.RecordNow(selectedSpline, recordName, recordType);
            EHandleUndo.RecordNow(selectedSpline.undoState, recordName, recordType);
            action.Invoke(selectedSpline);

            foreach (Spline spline2 in selectedSplines)
            {
                EHandleUndo.RecordNow(spline2, recordName, recordType);
                EHandleUndo.RecordNow(spline2.undoState, recordName, recordType);
                action.Invoke(spline2);
            }
        }

        public static void UpdatedSelectedSplines(Action<Spline> action)
        {
            action.Invoke(selectedSpline);
            foreach (Spline spline2 in selectedSplines) action.Invoke(spline2);
        }

        public static void UpdatedSelectedSplineObjectsRecordUndo(Action<SplineObject> action, string recordName, bool recordTransform = false, EHandleUndo.RecordType recordType = EHandleUndo.RecordType.RECORD_OBJECT)
        {
            Object objectToRecord = selectedSplineObject;

            if (recordTransform)
                objectToRecord = selectedSplineObject.transform;

            EHandleUndo.RecordNow(objectToRecord, recordName, recordType);
            EHandleUndo.RecordNow(selectedSplineObject.undoState, recordName, recordType);
            action.Invoke(selectedSplineObject);

            foreach (SplineObject so2 in selectedSplineObjects)
            {
                Object objectToRecord2 = so2;

                if (recordTransform)
                    objectToRecord2 = so2.transform;

                EHandleUndo.RecordNow(objectToRecord2, recordName, recordType);
                EHandleUndo.RecordNow(so2.undoState, recordName, recordType);
                action.Invoke(so2);
            }
        }

        public static void UpdatedSelectedSplineObjects(Action<SplineObject> action)
        {
            if (selectedSplineObject == null)
                return;

            action.Invoke(selectedSplineObject);

            foreach (SplineObject so2 in selectedSplineObjects)
            {
                action.Invoke(so2);
            }
        }

        public static void UpdateSelectedSegments(Spline spline, Action<Segment, bool, bool, bool> action)
        {
            GetSelectedSegments(selectedSegmentContainer1, spline);

            foreach (KeyValuePair<Segment, Vector3Int> kvp in selectedSegmentContainer1)
            {
                Segment seg = kvp.Key;
                bool anchorSelected = kvp.Value.x == 1;
                bool tangentASelected = kvp.Value.y == 1;
                bool tangentBSelected = kvp.Value.z == 1;

                action.Invoke(seg, anchorSelected, tangentASelected, tangentBSelected);
            }
        }

        public static void UpdateSelectedSegments(Spline spline, Action<Segment> action)
        {
            GetSelectedSegments(selectedSegmentContainer1, spline);

            foreach (KeyValuePair<Segment, Vector3Int> kvp in selectedSegmentContainer1)
            {
                Segment seg = kvp.Key;
                action.Invoke(seg);
            }
        }

        public static void UpdateSelectedSegmentsRecordUndo(Spline spline, Action<Segment> action, string recordName, EHandleUndo.RecordType recordType = EHandleUndo.RecordType.RECORD_OBJECT)
        {
            EHandleUndo.RecordNow(spline, recordName, recordType);
            EHandleUndo.RecordNow(spline.undoState, recordName, recordType);
            UpdateSelectedSegments(spline, action);
        }

        internal static void UpdateSelectedControlPointsRecordUndo(Action<SelectionUndoState> onUpdate, string recordName = null, EHandleUndo.RecordType recordType = EHandleUndo.RecordType.RECORD_OBJECT)
        {
            EHandleUndo.RecordNow(selectionUndoState, recordName, recordType);
            onUpdate.Invoke(selectionUndoState);
        }

        internal static void UpdateSelectedControlPoints(Action<SelectionUndoState> onUpdate)
        {
            onUpdate.Invoke(selectionUndoState);
        }

        internal static bool ContainsSelectedControlPoint(int controlPoint)
        {
            if (selectionUndoState == null)
                return false;

            return selectionUndoState.selectedControlPoints.Contains(controlPoint);
        }

        internal static void SelectAllAnchors(Spline spline)
        {
            int totalAnchors = spline.segments.Count;
            int[] anchors = new int[totalAnchors - 1];

            for (int i = 0; i < totalAnchors - 1; i++)
                anchors[i] = i * 3 + 1003;

            EHandleUndo.RecordNow(selectionUndoState, "Selected all anchors");
            selectionUndoState.selectedControlPoint = 1000;
            selectionUndoState.selectedControlPoints.Clear();
            selectionUndoState.selectedControlPoints.AddRange(anchors);
        }

        internal static void GetSelectedControlPoints(List<int> result)
        {
            result.Clear();
            result.AddRange(selectionUndoState.selectedControlPoints);
        }

        internal static void SelectControlPointsRecordUndo(List<int> controlPoints, string recordName = null, EHandleUndo.RecordType recordType = EHandleUndo.RecordType.RECORD_OBJECT)
        {
            EHandleUndo.RecordNow(selectionUndoState, recordName, recordType);
            selectionUndoState.selectedControlPoints.AddRange(controlPoints);
        }

        internal static void SelectControlPoints(List<int> controlPoints)
        {
            selectionUndoState.selectedControlPoints.AddRange(controlPoints);
        }

        public static Vector3 GetCenterFromAnchorSelection(Spline spline)
        {
            GetSelectedSegments(selectedSegmentContainer2, spline);

            Vector3 center = Vector3.zero;

            foreach (KeyValuePair<Segment, Vector3Int> kvp in selectedSegmentContainer2)
            {
                Segment seg = kvp.Key;

                if (seg == null)
                    continue;

                if (kvp.Value.x == 0 && (kvp.Value.y == 0 || kvp.Value.z == 0))
                {
                    if(kvp.Value.y == 1)
                        center += seg.GetPosition(ControlHandle.TANGENT_A);
                    else if (kvp.Value.z == 1)
                        center += seg.GetPosition(ControlHandle.TANGENT_B);
                }
                else
                    center += seg.GetPosition(ControlHandle.ANCHOR);
            }

            if(selectedSegmentContainer2.Count > 0)
                center = center / selectedSegmentContainer2.Count;

            if(selectedSegmentContainer2.Count > 1 && EGlobalSettings.GetPlaneVisibility() && EGlobalSettings.GetPlaneType() == PlaneType.GRID && selectedSpline != null)
                center = EHandlePlane.SnapToGrid(selectedSpline, center);

            return center;
        }

        public static int GetSelectedAnchorCount(Spline spline)
        {
            GetSelectedSegments(selectedSegmentContainer3, spline);
            return selectedSegmentContainer3.Count;
        }

        public static void GetAllSelectedSplinesNonAlloc(List<Spline> allSplines)
        {
            if(selectedSpline != null)
                allSplines.Add(selectedSpline);

            if (selectedSplines != null && selectedSplines.Count > 0)
                allSplines.AddRange(selectedSplines);
        }

        public static void GetSelectedSegments(Dictionary<Segment, Vector3Int> container, Spline spline)
        {
            container.Clear();
            UpdateContainer(SelectedControlPoint);
            for (int i = 0; i < SelectedControlPointsCount; i++)
            {
                int controlPointId = selectionUndoState.selectedControlPoints[i];
                UpdateContainer(controlPointId);
            }

            void UpdateContainer(int controlPointId)
            {
                int segmentIndex = SplineUtility.ControlPointIdToSegmentIndex(controlPointId);
                ControlHandle ch = SplineUtility.GetControlHandleType(controlPointId);

                if(segmentIndex < 0 || segmentIndex >= spline.segments.Count)
                    return;

                Segment segment = spline.segments[segmentIndex];
                if (!container.ContainsKey(segment))
                    container.Add(segment, new Vector3Int(ch == ControlHandle.ANCHOR ? 1 : 0, ch == ControlHandle.TANGENT_A ? 1 : 0, ch == ControlHandle.TANGENT_B ? 1 : 0));
                else
                {
                    Vector3Int v = container[segment];
                    if (ch == ControlHandle.ANCHOR) v.x = 1;
                    else if (ch == ControlHandle.TANGENT_A) v.y = 1;
                    else if (ch == ControlHandle.TANGENT_B) v.z = 1;
                    container[segment] = v;
                }
            }
        }
    }
}