// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleEvents.cs
//
// Author: Mikael Danielsson
// Date Created: 04-02-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace SplineArchitect
{
    public static class EHandleEvents
    {
        internal static bool updateSelection;
        internal static bool waitForEditor = true;
        internal static bool sceneIsClosing;
        internal static bool buildRunning;
        internal static bool dragActive;
        internal static bool undoActive;
        internal static bool undoWasRedo;
        internal static bool controlPointCreationActive;
        internal static bool isSplineConnectorSelected;
        internal static bool isSplineObjectSelected;
        internal static Spline selectedSpline;
        internal static PlayModeStateChange playModeStateChange;

        private static List<Spline> markedInfoUpdates = new List<Spline>();
        private static List<Spline> InitalizeAfterDragSplines = new List<Spline>();
        private static List<SplineConnector> InitalizeAfterDragSplineConnectors = new List<SplineConnector>();
        private static List<SplineObject> InitalizeAfterDragSplineObjects = new List<SplineObject>();

        // Internal events
        internal static event Action beforeFirstUpdate;
        internal static event Action beforeUpdate;
        internal static event Action beforeAssetImport;
        internal static event Action<Event, Spline, SplineObject> beforeWindowExtendedGUI;
        internal static event Action<Event, bool> afterWindowSplineGUI;
        internal static event Action<Event, bool> afterWindowControlPointGUI;
        internal static event Action<Spline> afterSegmentRemoved;

        // Public events
        public static event Action<Spline> afterInitalizeSpline;
        public static event Action<Spline> afterDestroySpline;
        public static event Action<Spline> afterSplineJoin;
        public static event Action<Spline> afterSplineReverse;
        public static event Action<Spline> afterSplineLoop;
        public static event Action<Spline, Vector3> afterTransformToCenter;
        public static event Action<Spline, Spline> afterSplineSplit;
        public static event Action<Spline> afterSplineHovered;
        public static event Action<Segment> beforeSegmentRemoved;
        public static event Action<Segment> afterSegmentLinked;
        public static event Action<Segment> afterSegmentProjectedToPlane;
        public static event Action<Segment> afterSegmentCreated;
        public static event Action<Segment, ControlHandle> afterSegmentMovement;
        public static event Action<Segment, ControlHandle> afterSegmentHovered;
        public static event Action<SplineObject> afterSplineOnbjectSetPositionInUi;
        public static event Action<SplineObject> afterSplineObjectActivatePositionTool;
        public static event Action<SplineObject> afterSplineObjectParentChanged;
        public static event Action<SplineConnector, Segment> afterSplineConnectorAlignSegment;

        internal static void InitAfterDrag(SceneView sceneView)
        {
            if (Event.current.type == EventType.DragUpdated)
            {
                dragActive = true;
            }
            else if (Event.current.type == EventType.DragPerform || Event.current.type == EventType.DragExited)
            {
                dragActive = false;
            }

            if (Event.current.type == EventType.DragPerform)
            {
                foreach (Spline spline in InitalizeAfterDragSplines)
                {
                    if (spline == null)
                        continue;

                    spline.Initalize();
                }

                foreach (SplineConnector sc in InitalizeAfterDragSplineConnectors)
                {
                    if (sc == null)
                        continue;

                    sc.Initalize();
                }

                foreach (SplineObject so in InitalizeAfterDragSplineObjects)
                {
                    if (so == null)
                        continue;

                    so.Initalize();
                }

                InitalizeAfterDragSplines.Clear();
                InitalizeAfterDragSplineObjects.Clear();
            }
        }

        internal static void InvokeBeforeFirstUpdate()
        {
            beforeFirstUpdate?.Invoke();
        }

        internal static void InvokeBeforeUpdate()
        {
            beforeUpdate?.Invoke();
        }

        internal static void InvokeBeforeAssetImport()
        {
            beforeAssetImport?.Invoke();
        }

        internal static void InvokeBeforeSegmentRemoved(Segment segment)
        {
            beforeSegmentRemoved?.Invoke(segment);
        }

        internal static void InvokeAfterSegmentProjectedToPlane(Segment segment)
        {
            afterSegmentProjectedToPlane?.Invoke(segment);
        }

        internal static void InvokeAfterSplineConnectorAlignSegment(SplineConnector splineConnector, Segment segment)
        {
            afterSplineConnectorAlignSegment?.Invoke(splineConnector, segment);
        }

        internal static void InvokeAfterSegmentLinked(Segment segment)
        {
            afterSegmentLinked?.Invoke(segment);
        }

        internal static void InvokeAfterSegmentRemoved(Spline spline)
        {
            afterSegmentRemoved?.Invoke(spline);
        }

        internal static void InvokeAfterSegmentCreated(Segment segment)
        {
            afterSegmentCreated?.Invoke(segment);
        }

        internal static void InvokeAfterTransformToCenter(Spline spline, Vector3 dif)
        {
            afterTransformToCenter?.Invoke(spline, dif);
        }

        internal static void InvokeAfterInitalizeSpline(Spline spline)
        {
            afterInitalizeSpline?.Invoke(spline);
        }

        internal static void InvokeSplineSplit(Spline spline, Spline newSpline)
        {
            afterSplineSplit?.Invoke(spline, newSpline);
        }

        internal static void InvokeAfterSegmentMovement(Segment segment, ControlHandle controlHandle)
        {
            afterSegmentMovement?.Invoke(segment, controlHandle);
        }

        internal static void InvokeAfterSplineJoin(Spline spline)
        {
            afterSplineJoin?.Invoke(spline);
        }

        internal static void InvokeAfterSplineLoop(Spline spline)
        {
            afterSplineLoop?.Invoke(spline);
        }

        internal static void InvokeAfterSplineReverse(Spline spline)
        {
            afterSplineReverse?.Invoke(spline);
        }

        internal static void InvokeAfterDestroySpline(Spline spline)
        {
            afterDestroySpline?.Invoke(spline);
        }

        internal static void InvokeAfterSplineHovered(Spline spline)
        {
            afterSplineHovered?.Invoke(spline);
        }

        internal static void InvokeAfterSegmentHovered(Segment segment, ControlHandle controlHandle)
        {
            afterSegmentHovered?.Invoke(segment, controlHandle);
        }

        internal static void InvokeAfterSplineObjectParentChanged(SplineObject so)
        {
            afterSplineObjectParentChanged?.Invoke(so);
        }

        internal static void InvokeAfterSplineObjectActivatePositionTool(SplineObject splineObject)
        {
            afterSplineObjectActivatePositionTool?.Invoke(splineObject);
        }

        internal static void InvokeBeforeWindowExtendedGUI(Event e, Spline spline, SplineObject splineObject)
        {
            beforeWindowExtendedGUI?.Invoke(e, spline, splineObject);
        }

        internal static void InvokeAfterWindowSplineGUI(Event e, bool leftMouseUp)
        {
            afterWindowSplineGUI?.Invoke(e, leftMouseUp);
        }

        internal static void InvokeAfterWindowControlPointGUI(Event e,bool leftMouseUp)
        {
            afterWindowControlPointGUI?.Invoke(e, leftMouseUp);
        }

        internal static void InvokeAfterSplineObjectSetPositionInUi(SplineObject splineObject)
        {
            afterSplineOnbjectSetPositionInUi?.Invoke(splineObject);
        }

        internal static void MarkForInfoUpdate(Spline spline)
        {
            if (markedInfoUpdates.Contains(spline))
                return;

            markedInfoUpdates.Add(spline);
        }

        internal static List<Spline> GetMarkedForInfoUpdates()
        {
            return markedInfoUpdates;
        }

        internal static void ClearMarkedForInfoUpdates()
        {
            markedInfoUpdates.Clear();
        }

        internal static void ForceUpdateSelection()
        {       
            updateSelection = true;
        }

        internal static void InitalizeAfterDrag(Spline spline)
        {
            if (InitalizeAfterDragSplines.Contains(spline))
                return;

            InitalizeAfterDragSplines.Add(spline);
        }

        internal static void InitalizeAfterDrag(SplineConnector splineConnector)
        {
            if (InitalizeAfterDragSplineConnectors.Contains(splineConnector))
                return;

            InitalizeAfterDragSplineConnectors.Add(splineConnector);
        }

        internal static void InitalizeAfterDrag(SplineObject splineObject)
        {
            if (InitalizeAfterDragSplineObjects.Contains(splineObject))
                return;

            InitalizeAfterDragSplineObjects.Add(splineObject);
        }
    }
}

#endif
