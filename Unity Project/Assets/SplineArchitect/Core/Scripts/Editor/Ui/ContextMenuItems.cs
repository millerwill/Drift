// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ContextMenuItems.cs
//
// Author: Mikael Danielsson
// Date Created: 31-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

#if SA_UNITY_SPLINES
using UnityEngine.Splines;
#endif

namespace SplineArchitect.Ui
{
    public static class ContextMenuItems
    {
        [MenuItem("GameObject/Spline Architect/Spline", false, 100)]
        public static void CreateSpline()
        {
            Spline spline = EHandleSpline.CreatedForContext(new GameObject());
            if (Selection.activeTransform != null)
                EHandleUndo.SetTransformParent(spline.transform, Selection.activeTransform);

            Selection.activeTransform = spline.transform;

            EHandleUndo.RecordNow(spline);
        }

        [MenuItem("GameObject/Spline Architect/Spline Connector", false, 101)]
        public static void CreateSplineConnector()
        {
            SplineConnector splineConnector = EHandleSplineConnector.CreatedForContext(new GameObject());
            if(Selection.activeTransform != null)
                EHandleUndo.SetTransformParent(splineConnector.transform, Selection.activeTransform);

            Selection.activeTransform = splineConnector.transform;
        }

        [MenuItem("CONTEXT/Spline/Convert to default spline", true)]
        private static bool ValidateConvertToDefaultSpline()
        {
            Spline spline = EHandleSelection.selectedSpline;
            if (spline == null) return false;
            return spline.splineType != SplineType.NOT_USED;
        }

        [MenuItem("CONTEXT/Spline/Convert to default spline", false, 3000)]
        public static void ConvertToDefaultSpline()
        {
            Spline spline = EHandleSelection.selectedSpline;

            EHandleUndo.RecordNow(spline, "Converted to default spline.");
            spline.splineType = SplineType.NOT_USED;
            spline.MarkCacheDirty();

            foreach (Spline spline2 in EHandleSelection.selectedSplines)
            {
                EHandleUndo.RecordNow(spline2, "Converted to default spline.");
                spline2.splineType = SplineType.NOT_USED;
                spline2.MarkCacheDirty();
            }
        }

        [MenuItem("CONTEXT/Spline/Convert to legacy spline", true)]
        private static bool ValidateConvertToLegacySpline()
        {
            Spline spline = EHandleSelection.selectedSpline;
            if (spline == null) return false;
            return spline.splineType == SplineType.NOT_USED;
        }

        [MenuItem("CONTEXT/Spline/Convert to legacy spline", false, 3001)]
        public static void ConvertToLegacySpline()
        {
            Spline spline = EHandleSelection.selectedSpline;

            EHandleUndo.RecordNow(spline, "Converted to legacy spline.");
            spline.splineType = SplineType.DYNAMIC;
            for (int i = 0; i < spline.SegmentCount; i++)
            {
                Segment s = spline.GetSegmentAtIndex(i);
                s.IsStatic = false;
            }
            spline.MarkCacheDirty();

            foreach (Spline spline2 in EHandleSelection.selectedSplines)
            {
                EHandleUndo.RecordNow(spline2, "Converted to legacy spline.");
                spline2.splineType = SplineType.DYNAMIC;
                for (int i = 0; i < spline2.SegmentCount; i++)
                {
                    Segment s = spline.GetSegmentAtIndex(i);
                    s.IsStatic = false;
                }
                spline2.MarkCacheDirty();
            }
        }

#if SA_UNITY_SPLINES
        [MenuItem("CONTEXT/Spline/Match closest unity spline", false, 2000)]
        private static void MatchClosestUnitySpline(MenuCommand command)
        {
            Match(EHandleSelection.selectedSpline);
            foreach (Spline spline in EHandleSelection.selectedSplines) Match(spline);

            EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) => 
            {
                undoState.selectedControlPoint = 0;
                undoState.selectedControlPoints.Clear();
            }, "Match spline to unity spline");

            void Match(Spline spline)
            {
                if (spline == null)
                    return;

                Vector3 splineCenterPoint = spline.GetCenter();
                UnityEngine.Splines.Spline closestUnitySpline = null;
                UnityEngine.Splines.SplineContainer closestSplineContainer = null;
                float disCheck = 99999;

                SplineContainer[] splineContainers = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                foreach (SplineContainer container in splineContainers)
                {
                    if (container == null)
                        continue;

                    if (!container.enabled)
                        continue;

                    foreach (UnityEngine.Splines.Spline unitySpline in container.Splines)
                    {
                        float3 point = float3.zero;

                        foreach (BezierKnot knot in unitySpline)
                        {
                            point += knot.Position;
                        }

                        point /= unitySpline.Count;
                        point = container.transform.TransformPoint(point);

                        float dis = Vector3.Distance(point, splineCenterPoint);

                        if (dis < disCheck)
                        {
                            closestUnitySpline = unitySpline;
                            disCheck = dis;
                            closestSplineContainer = container;
                        }
                    }
                }

                if (closestUnitySpline == null)
                    return; 

                EHandleUndo.RecordNow(spline, "Match spline to unity spline");
                for (int i = 0; i < closestUnitySpline.Count; i++)
                {
                    UnityEngine.Splines.BezierKnot knot = closestUnitySpline[i];

                    Vector3 anchor = knot.Position;
                    Vector3 tangentOut = (Vector3)knot.TangentOut;
                    Vector3 tangentIn = (Vector3)knot.TangentIn;
                    Vector3 tangentA = anchor + (Vector3)math.rotate(knot.Rotation, tangentOut);
                    Vector3 tangentB = anchor + (Vector3)math.rotate(knot.Rotation, tangentIn);

                    float tADis = Vector3.Distance(anchor, tangentA);
                    float tBDis = Vector3.Distance(anchor, tangentB);

                    anchor = closestSplineContainer.transform.TransformPoint(anchor);
                    tangentA = closestSplineContainer.transform.TransformPoint(tangentA);
                    tangentB = closestSplineContainer.transform.TransformPoint(tangentB);

                    if (spline.SegmentCount - 1 < i)
                    {
                        spline.CreateSegment(anchor, tADis, tBDis, knot.Rotation);
                    }
                    else
                    {
                        Segment segment = spline.GetSegmentAtIndex(i);
                        if(!segment.IsStatic)
                            segment.Rotation = knot.Rotation;
                        segment.SetPosition(ControlHandle.ANCHOR, anchor);
                        segment.SetPosition(ControlHandle.TANGENT_A, tangentA);
                        segment.SetPosition(ControlHandle.TANGENT_B, tangentB);
                    }
                }

                int dif = spline.SegmentCount - closestUnitySpline.Count;

                for (int i2 = dif; i2 > 0; i2--)
                {
                    spline.RemoveSegmentAt(spline.SegmentCount - 1);
                }

                spline.SetLoop(closestUnitySpline.Closed);
            }
        }

        [MenuItem("CONTEXT/Spline/Convert to unity spline", false, 2001)]
        private static void ConvertToUnitySpline(MenuCommand command)
        {
            Spline selectedSpline = EHandleSelection.selectedSpline;

            if (selectedSpline == null)
                return;

            // Check if spline container allready exists
            UnityEngine.Splines.SplineContainer splineContainer = selectedSpline.gameObject.GetComponent<UnityEngine.Splines.SplineContainer>();
            if (splineContainer == null)
                splineContainer = EHandleUndo.AddComponent<UnityEngine.Splines.SplineContainer>(selectedSpline.gameObject);

            EHandleUndo.RecordNow(splineContainer, $"Converted splines to a Unity Spline.");
            splineContainer.Spline = CreateUnitySpline(selectedSpline);

            for (int i = selectedSpline.RootSplineObjectCount - 1; i >= 0; i--)
            {
                SplineObject so = selectedSpline.GetRootSplineObjectAtIndex(i);
                EHandleUndo.DestroyObjectImmediate(so.gameObject);
            }
            Selection.activeGameObject = selectedSpline.gameObject;
            EHandleUndo.DestroyObjectImmediate(selectedSpline);

            foreach (Spline spline in EHandleSelection.selectedSplines)
            {
                splineContainer.AddSpline(CreateUnitySpline(spline));
                EHandleUndo.DestroyObjectImmediate(spline.gameObject);
            }

            UnityEngine.Splines.Spline CreateUnitySpline(Spline spline)
            {
                UnityEngine.Splines.Spline unitySpline = new UnityEngine.Splines.Spline();

                int segmentCount = spline.SegmentCount;

                for (int i = 0; i < segmentCount; i++)
                {
                    Segment segment = spline.GetSegmentAtIndex(i);

                    if (spline.Loop && (i + 2) > segmentCount)
                        break;

                    Vector3 anchor = segment.GetPosition(ControlHandle.ANCHOR);
                    Vector3 tangentA = segment.GetPosition(ControlHandle.TANGENT_A);
                    Vector3 tangentB = segment.GetPosition(ControlHandle.TANGENT_B);

                    anchor = splineContainer.transform.InverseTransformPoint(anchor);
                    tangentA = splineContainer.transform.InverseTransformPoint(tangentA);
                    tangentB = splineContainer.transform.InverseTransformPoint(tangentB);

                    Vector3 tangentIn = tangentB - anchor;
                    Vector3 tangentOut = tangentA - anchor;

                    UnityEngine.Splines.BezierKnot knot = new UnityEngine.Splines.BezierKnot(anchor, tangentIn, tangentOut);
                    unitySpline.Add(knot);
                }

                unitySpline.Closed = spline.Loop;
                unitySpline.SetTangentMode(TangentMode.Continuous);
                return unitySpline;
            }
        }

        [MenuItem("CONTEXT/SplineContainer/Convert to spline architect spline", false, 2000)]
        private static void ConvertToSpline(MenuCommand command)
        {
            UnityEngine.Splines.SplineContainer splineContainer = command.context as UnityEngine.Splines.SplineContainer;

            if (splineContainer == null || splineContainer.Splines.Count == 0)
                return;

            int count = 0;

            Spline splineToSelect = null;

            foreach (UnityEngine.Splines.Spline unitySpline in splineContainer.Splines)
            {
                if (unitySpline == null || unitySpline.Count < 2)
                    continue;

                Spline spline;
                if (count == 0)
                {
                    spline = EHandleUndo.AddComponent<Spline>(splineContainer.gameObject);
                    splineToSelect = spline;
                }
                else
                {
                    GameObject go = new GameObject($"Spline ({count})");
                    EHandleUndo.RegisterCreatedObject(go);
                    spline = EHandleUndo.AddComponent<Spline>(go);
                }

                EHandleUndo.RecordNow(spline);
                for (int i = 0; i < unitySpline.Count; i++)
                {
                    UnityEngine.Splines.BezierKnot knot = unitySpline[i];

                    Vector3 anchor = knot.Position;
                    Vector3 tangentOut = (Vector3)knot.TangentOut;
                    Vector3 tangentIn = (Vector3)knot.TangentIn;

                    Vector3 tangentA = anchor + (Vector3)math.rotate(knot.Rotation, tangentOut);
                    Vector3 tangentB = anchor + (Vector3)math.rotate(knot.Rotation, tangentIn);

                    float tADis = Vector3.Distance(anchor, tangentA);
                    float tBDis = Vector3.Distance(anchor, tangentB);

                    anchor = splineContainer.transform.TransformPoint(anchor);
                    tangentA = splineContainer.transform.TransformPoint(tangentA);
                    tangentB = splineContainer.transform.TransformPoint(tangentB);
                    
                    spline.CreateSegment(anchor, tADis, tBDis, knot.Rotation);
                }

                spline.SetLoop(unitySpline.Closed);

                count++;
            }

            for (int i = splineContainer.gameObject.GetComponentCount() - 1; i >= 0; i--)
            { 
                Component component = splineContainer.gameObject.GetComponentAtIndex(i);

                if (component is Spline || component is Transform || component is SplineContainer)
                    continue;

                EHandleUndo.DestroyObjectImmediate(component);
            }

            EHandleUndo.DestroyObjectImmediate(splineContainer);

            if (splineToSelect != null)
                Selection.activeGameObject = splineToSelect.gameObject;
        }
#endif
    }
}
