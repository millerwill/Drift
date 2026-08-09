// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleSpline.cs
//
// Author: Mikael Danielsson
// Date Created: 28-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vector3 = UnityEngine.Vector3;

using SplineArchitect.Ui;
using SplineArchitect.Utility;
using SplineArchitect.ScriptableObjects;

namespace SplineArchitect
{
    internal class EHandleSpline
    {
        internal static bool controlPointCreationActive;
        internal static bool controlPointIndicatorDisabled;

        internal static float lengthAllSplines { private set; get; }
        internal static int totalLinesDrawn;
        internal static int hotControlId;

        private static List<Spline> markedInfoUpdates = new List<Spline>();
        private static List<int> controlPointsContainer = new List<int>();
        private static Plane projectionPlane = new Plane(Vector3.up, Vector3.zero);
        private static Vector3[] normalsContainer = new Vector3[3];
        private static Dictionary<Segment, Vector3Int> selectedSegmentContainer = new Dictionary<Segment, Vector3Int>();
        private static bool oldControlPointIndicatorDisabled;

        internal static void BeforeSceneGUIGlobal(SceneView sceneView, Event e)
        {
            lengthAllSplines = HandleRegistry.GetTotalLengthOfAllSplines();
            Spline spline = EHandleSelection.selectedSpline;

            if (!controlPointCreationActive)
                return;

            if (oldControlPointIndicatorDisabled != controlPointIndicatorDisabled)
            {
                oldControlPointIndicatorDisabled = controlPointIndicatorDisabled;
                EHandleSceneView.RepaintCurrent();
            }

#if UNITY_2022
            EHandleSceneView.RepaintCurrent();
#endif
            bool gridVisiblity = EGlobalSettings.GetPlaneVisibility();

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                EHandleSegment.adjustIndicatorStartPoint = new Vector3(0, 99999, 0);
                EHandleSegment.adjustIndicatorActive = false;
            }

            if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
            {
                if (spline != null && EHandleSelection.SelectedControlPoint > 0 && EGlobalSettings.GetHandleType() != ControlHandleType.AUTO)
                {
                    EHandleSegment.AdjustIndicator(spline, e);
                    GUIUtility.hotControl = GetHotControlId();
                }
            }
            else if (spline == null)
            {
                UpdateNewControlPointTangentDistance(Event.current);
                UpdateIndicator(e, ref EHandleSegment.segmentIndicatorData);

                //Create spline
                if (!controlPointIndicatorDisabled && e.type == EventType.MouseDown && Event.current.button == 0)
                {
                    CreateSpline(e, EHandleSegment.segmentIndicatorData);

                    e.Use();
                }
            }
            else
            {
                EHandleSegment.UpdateIndicatorHover(spline, e.mousePosition);

                //Hovering spline
                if (spline.indicatorDistanceToSpline < GetIndicatorActivationDistance(spline))
                {
                    if (e.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        bool extendBack = spline.indicatorSegment == 0;
                        bool extendFront = spline.indicatorSegment > spline.segments.Count - 1;

                        if (extendFront || extendBack)
                            EHandleSegment.CreateExtended(spline, extendBack);
                        else
                        {
                            EHandleSegment.CreateWithAutoSmooth(spline, spline.indicatorTime);
                        }

                        GUIUtility.hotControl = GetHotControlId();
                        Selection.activeTransform = spline.transform;
#if UNITY_6000_0_OR_NEWER
                        e.Use();
#endif
                    }
                }
                //Not hovering spline
                else
                {
                    UpdateNewControlPointTangentDistance(Event.current);

                    //Updated segment indicator
                    if (gridVisiblity) EHandleSegment.UpdateIndicatorPlane(spline, e);
                    else EHandleSegment.UpdateIndicator(spline, e);

                    if (!controlPointIndicatorDisabled && !spline.Loop && e.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        EHandleSegment.CreateFromWorldPoint(spline);
                        GUIUtility.hotControl = GetHotControlId();
                        Selection.activeTransform = spline.transform;
#if UNITY_6000_0_OR_NEWER
                        e.Use();
#endif
                    }
                }
            }
        }

        internal static void OnSceneGUI(Spline spline, Event e)
        {
            EHandleSegment.HandleDeletion(spline, e);
        }

        internal static void InitalizeEditor(Spline spline, bool editorInitalized)
        {
            if (spline.editorInitialized)
                return;

            if (spline.splineType != SplineType.NOT_USED)
            {
                if (spline.splineType == SplineType.STATIC_3D || spline.splineType == SplineType.STATIC_2D)
                {
                    spline.splineType = SplineType.NOT_USED;

                    for (int i = 0; i < spline.SegmentCount; i++) 
                    {
                        Segment s = spline.GetSegmentAtIndex(i);
                        s.IsStatic = true;
                        s.UpdateRotation();
                    }

                    Debug.Log($"[Spline Architect] Spline '{spline.name}' was converted to the new spline type system. All segments are now Static.");
                }

                if (spline.splineType == SplineType.DYNAMIC)
                {
                    for (int i = 0; i < spline.SegmentCount; i++)
                    {
                        Segment s = spline.GetSegmentAtIndex(i);
                        s.UpdateRotation(true);
                    }
                }
            }

            //Create segments if none exists.
            if (spline.segments.Count == 0)
                EHandleSegment.CreateSegmentsFromEditorCameraDirection(spline);

            MarkForInfoUpdate(spline);

            if (editorInitalized)
            {
                // Copy case, and also runs during some undo cases.
                // Do not assume this only runs on copied splines.
                if (!spline.editorInitialized && !EHandlePrefab.prefabStageOpenedLastFrame && !EHandlePrefab.prefabStageClosedLastFrame)
                {
                    EHandleSelection.ForceUpdate();
                }
            }

            spline.EstablishLinks();

            EHandleDeformation.ProcessSplineObjects(spline, false);
            EHandleMeshContainer.DeleteDuplicates(spline);

            EHandleEvents.InvokeAfterInitalizeSpline(spline);
            spline.editorInitialized = true;
        }

        internal static void UpdateLinksOnTransformChange(Spline spline)
        {
            if (spline.Monitor.EditorTransformChange())
            {
                EHandleSegment.LinkMovementAll(spline);
            }
        }

        private static void UpdateIndicator(Event e, ref EHandleSegment.SegmentIndicatorData segmentIndicatorData)
        {
            bool is2D = EHandleSceneView.GetCurrent().in2DMode;
            bool hitCollider = false;
            bool onTerrainSurface = false;
            Vector3 hitPoint = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);
            Transform cam = EHandleSceneView.GetCurrent().camera.transform;
            controlPointIndicatorDisabled = false;

            RaycastHit hit;
            if (!is2D && Physics.Raycast(mouseRay, out hit))
            {
                hitPoint = hit.point;

                Vector3 forward = Vector3.ProjectOnPlane(cam.up, hit.normal);
                rotation = Quaternion.LookRotation(forward, hit.normal);
                hitCollider = true;

                if (hit.collider is TerrainCollider)
                    onTerrainSurface = true;
            }
            else
            {
                projectionPlane.SetNormalAndPosition(cam.forward, cam.position + cam.forward * 50);
                if (is2D) projectionPlane.SetNormalAndPosition(Vector3.forward, Vector3.zero);
                if (projectionPlane.Raycast(mouseRay, out float enter))
                {
                    hitPoint = mouseRay.GetPoint(enter);
                    if (is2D)
                        rotation = Quaternion.LookRotation(Vector3.right, -Vector3.forward);
                    else
                        rotation = Quaternion.LookRotation(cam.up, -cam.forward);
                }
                else
                    controlPointIndicatorDisabled = true;
            }

            if (EHandleModifier.CtrlActive(e) && !EHandleModifier.ShiftActive(e))
            {
                if (hitCollider && !is2D)
                {
                    Spline closest = SplineUtility.GetNearestSpline(hitPoint, HandleRegistry.GetSplinesUnsafe(), 20 * EHandleSegment.DistanceModifier(hitPoint), out float time, out _);
                    if (closest != null)
                    {
                        closest.GetNormalNonAlloc(normalsContainer, time);
                        rotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
                    }
                }
                else
                {
                    Spline intersectingSpline = SplineUtility.GetIntersectingSpline(mouseRay, HandleRegistry.GetSplinesUnsafe());
                    if (intersectingSpline != null)
                    {
                        Vector3 point = ClosestMousePoint(intersectingSpline, mouseRay, 10, out _, out float time, 7);
                        intersectingSpline.GetNormalNonAlloc(normalsContainer, time);
                        rotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
                    }
                }
            }

            float tangentDistance = EHandleSegment.DistanceModifier(hitPoint, EGlobalSettings.GetNewSegmentTangentDistance());
            if (EGlobalSettings.GetPlaneVisibility() && EGlobalSettings.GetPlaneType() == PlaneType.GRID)
            {
                tangentDistance = EHandlePlane.SnapToGridByDistance(tangentDistance);
                float gridSize = EGlobalSettings.GetGridSize();
                if(tangentDistance < gridSize) tangentDistance = gridSize;
            }

            segmentIndicatorData.newAnchor = hitPoint;
            segmentIndicatorData.newTangentADis = tangentDistance;
            segmentIndicatorData.newTangentBDis = tangentDistance;
            segmentIndicatorData.newRotation = rotation;
            segmentIndicatorData.onTerrainSurface = onTerrainSurface;
        }

        private static void CreateSpline(Event e, EHandleSegment.SegmentIndicatorData segmentIndicatorData)
        {
            // Create spline
            GameObject go = new GameObject();
            go.transform.position = segmentIndicatorData.newAnchor;
            if(!segmentIndicatorData.onTerrainSurface)
                go.transform.rotation = segmentIndicatorData.newRotation;
            Spline spline = CreatedForContext(go);

            EHandleUndo.RecordNow(spline);
            spline.CreateSegment(0, segmentIndicatorData.newAnchor, segmentIndicatorData.newTangentADis, segmentIndicatorData.newTangentBDis, segmentIndicatorData.newRotation);

            // Set transform position
            Vector3 oldPos = spline.transform.position;
            spline.transform.position = spline.GetSegmentAtIndex(0).GetPosition(ControlHandle.ANCHOR);
            Vector3 dif = spline.transform.position - oldPos;

            for (int i = 0; i < spline.SegmentCount; i++)
            {
                Segment s = spline.GetSegmentAtIndex(i);
                s.TranslateAnchor(dif);
            }

            // Selection
            Selection.activeTransform = spline.transform;
            EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
            {
                undoState.selectedControlPoint = SplineUtility.SegmentIndexToControlPointId(0, ControlHandle.ANCHOR);
            });
        }

        internal static Spline CreatedForContext(GameObject go)
        {
            go.name = $"Spline ({HandleRegistry.GetSplinesUnsafe().Count + 1})";
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                SceneManager.MoveGameObjectToScene(go, prefabStage.scene);
                EHandleUndo.RegisterCreatedObject(go, "Created Spline");
                EHandleUndo.SetTransformParent(go.transform, prefabStage.prefabContentsRoot.transform, "Created Spline");
            }
            else
            {
                EHandleUndo.RegisterCreatedObject(go, "Created Spline");
            }

            return EHandleUndo.AddComponent<Spline>(go);
        }

        internal static void MoveControlPointsToPlane(Spline spline)
        {
            EHandleSelection.GetSelectedSegments(selectedSegmentContainer, spline);
            Plane plane = new Plane(spline.transform.up, spline.transform.position);

            foreach(KeyValuePair<Segment, Vector3Int> kvp in selectedSegmentContainer)
            {
                Segment segment = kvp.Key;

                Vector3 anchorPosition = segment.GetPosition(ControlHandle.ANCHOR);
                Vector3 anchorHitPoint = anchorPosition - plane.normal * plane.GetDistanceToPoint(anchorPosition);

                if (kvp.Value.x == 1 || (kvp.Value.y == 1 && kvp.Value.z == 1))
                {
                    segment.SetAnchorPosition(anchorHitPoint);
                    Vector3 planeNormal = plane.normal;

                    if (!segment.IsStatic)
                    {
                        Vector3 currentForward = segment.Rotation * Vector3.forward;
                        Vector3 forward = Vector3.ProjectOnPlane(currentForward, planeNormal);

                        if (forward.sqrMagnitude < 0.0001f)
                        {
                            Vector3 currentRight = segment.Rotation * Vector3.right;
                            forward = Vector3.ProjectOnPlane(currentRight, planeNormal);
                        }

                        segment.Rotation = Quaternion.LookRotation(forward.normalized, planeNormal);
                    }
                    else
                    {
                        Vector3 ta = segment.GetPosition(ControlHandle.TANGENT_A);
                        Vector3 tb = segment.GetPosition(ControlHandle.TANGENT_B);
                        segment.SetPosition(ControlHandle.TANGENT_A, new Vector3(ta.x, anchorHitPoint.y, ta.z));
                        segment.SetPosition(ControlHandle.TANGENT_B, new Vector3(tb.x, anchorHitPoint.y, tb.z));
                    }
                }
                else
                {
                    ControlHandle controlHandle = kvp.Value.y == 1 ? ControlHandle.TANGENT_A : ControlHandle.TANGENT_B;
                    ControlHandleType controlHandleType = EGlobalSettings.GetHandleType();

                    Vector3 tangentHitPoint;

                    if (controlHandle == ControlHandle.TANGENT_A)
                    {
                        Vector3 tangentAPos = segment.GetPosition(ControlHandle.TANGENT_A);
                        tangentHitPoint = tangentAPos - plane.normal * plane.GetDistanceToPoint(tangentAPos);
                    }
                    else
                    {
                        Vector3 tangentBPos = segment.GetPosition(ControlHandle.TANGENT_B);
                        tangentHitPoint = tangentBPos - plane.normal * plane.GetDistanceToPoint(tangentBPos);
                    }

                    if(controlHandleType == ControlHandleType.MIRRORED)
                        segment.SetMirroredPosition(controlHandle, tangentHitPoint);
                    else if (controlHandleType == ControlHandleType.BROKEN)
                        segment.SetPosition(controlHandle, tangentHitPoint);
                    else
                        segment.SetContinuousPosition(controlHandle, tangentHitPoint);
                }
            }

            EHandleSegment.LinkMovementAll(spline);
        }

        internal static void EnableDisableLoop(Spline spline, bool enable)
        {
            if (EHandleUndo.UndoTriggered())
                return;

            if (enable)
            {
                Segment first = spline.GetSegmentAtIndex(0);
                Segment last = spline.CreateSegment(spline.segments.Count,
                                     spline.segments[0].GetPosition(ControlHandle.ANCHOR),
                                     spline.segments[0].TangentADistance,
                                     spline.segments[0].TangentBDistance,
                                     spline.segments[0].Rotation);

                last.Contrast = first.Contrast;
                last.Scale = first.Scale;
                last.Noise = first.Noise;
                last.SaddleSkew = first.SaddleSkew;
                last.ZRotation = first.ZRotation;
            }
            else
            {
                EHandleSegment.MarkForDeletion(spline.segments.Count - 1);
                EHandleSegment.DeleteAndUnlinkMarked(spline, false);
            }
        }

        internal static Spline Split(Spline spline, int segmentIndex)
        {
            GameObject splineGo = new GameObject(spline.name + "(split)");
            splineGo.transform.rotation = spline.GetSegmentAtIndex(segmentIndex).Rotation;
            splineGo.transform.position = spline.GetSegmentAtIndex(segmentIndex).GetPosition(ControlHandle.ANCHOR);
            EHandleUndo.RegisterCreatedObject(splineGo, "Splited Spline");
            Spline newSpline = CreatedForContext(splineGo);

            EHandleUndo.RecordNow(newSpline);
            EHandleUndo.RecordNow(spline);

            for (int i = segmentIndex; i < spline.segments.Count; i++)
            {
                Segment s = spline.segments[i];

                if(i == segmentIndex)
                    newSpline.CreateSegment(0, s.GetPosition(ControlHandle.ANCHOR), s.TangentADistance, s.TangentBDistance, s.Rotation);
                else
                {
                    newSpline.AddSegment(s);
                }
            }

            for (int i = spline.segments.Count - 1; i > segmentIndex; i--)
                spline.RemoveSegmentAt(i);

            if(spline.transform.parent != null)
                EHandleUndo.SetTransformParent(splineGo.transform, spline.transform.parent);

            return newSpline;
        }

        internal static void JoinSelection()
        {
            Spline selected = EHandleSelection.selectedSpline;
            List<Spline> secondarySelection = new List<Spline>();
            secondarySelection.AddRange(EHandleSelection.selectedSplines);

            //Needs to be: RegisterCompleteObjectUndo. Else Links will disappear during Undo. Seems to not be needed on closestAc at the bottom of this function.
            EHandleUndo.RecordNow(selected, "Join selected Splines", EHandleUndo.RecordType.REGISTER_COMPLETE_OBJECT);

            int iterations = EHandleSelection.selectedSplines.Count;

            while (iterations > 0)
            {
                iterations--;

                Vector3 primaryStart = selected.segments[0].GetPosition(ControlHandle.ANCHOR);
                Vector3 primaryEnd = selected.segments[selected.segments.Count - 1].GetPosition(ControlHandle.ANCHOR);

                JoinType joinType = JoinType.END_TO_START;
                float distanceCheck = 999999;
                Spline closestSpline = null;

                for (int i = secondarySelection.Count - 1; i >= 0; i--)
                {
                    Spline spline = secondarySelection[i];

                    Vector3 secondaryStart = spline.segments[0].GetPosition(ControlHandle.ANCHOR);
                    Vector3 secondaryEnd = spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.ANCHOR);

                    float distanceToPoint = Vector3.Distance(primaryStart, secondaryStart);
                    if (distanceToPoint < distanceCheck)
                    {
                        joinType = JoinType.START_TO_START;
                        closestSpline = spline;
                        distanceCheck = distanceToPoint;
                    }

                    distanceToPoint = Vector3.Distance(primaryStart, secondaryEnd);
                    if (distanceToPoint < distanceCheck)
                    {
                        joinType = JoinType.START_TO_END;
                        closestSpline = spline;
                        distanceCheck = distanceToPoint;
                    }

                    distanceToPoint = Vector3.Distance(primaryEnd, secondaryStart);
                    if (distanceToPoint < distanceCheck)
                    {
                        joinType = JoinType.END_TO_START;
                        closestSpline = spline;
                        distanceCheck = distanceToPoint;
                    }

                    distanceToPoint = Vector3.Distance(primaryEnd, secondaryEnd);
                    if (distanceToPoint < distanceCheck)
                    {
                        joinType = JoinType.END_TO_END;
                        closestSpline = spline;
                        distanceCheck = distanceToPoint;
                    }
                }

                secondarySelection.Remove(closestSpline);
                EHandleUndo.RecordNow(closestSpline);
                selected.Join(closestSpline, joinType);
            }

            //Dont know why I can't do this within the while loop above but it does not work.
            foreach (Spline spline in EHandleSelection.selectedSplines)
            {
                EHandleUndo.DestroyObjectImmediate(spline.gameObject);
            }
        }

        internal static void MarkForInfoUpdate(Spline spline)
        {
            if (spline == null)
                return;

            if (markedInfoUpdates.Contains(spline))
                return;

            markedInfoUpdates.Add(spline);
        }

        internal static void ProcessMarkedForInfoUpdates()
        {
            foreach(Spline spline in EHandleEvents.GetMarkedForInfoUpdates())
            {
                if (markedInfoUpdates.Contains(spline))
                    continue;

                markedInfoUpdates.Add(spline);
            }

            EHandleEvents.ClearMarkedForInfoUpdates();

            for (int i2 = markedInfoUpdates.Count - 1; i2 >= 0; i2--)
                markedInfoUpdates[i2].UpdateInfo();

            if (markedInfoUpdates.Count > 0)
                WindowBase.RepaintAll();

            markedInfoUpdates.Clear();
        }

        internal static float GetSplineMemoryUsage(Spline spline)
        {
            float size = 0;

            if (spline.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                return size;

            if (spline.componentMode == ComponentMode.INACTIVE)
                return size;

            if (spline.DistanceMap.IsCreated)
                size += 4 * spline.DistanceMap.Length;

            if (spline.PositionMapLocal.IsCreated)
                size += 12 * spline.PositionMapLocal.Length;

            if (spline.NormalsLocal.IsCreated)
                size += 12 * spline.NormalsLocal.Length;

            return size;
        }

        internal static float GetComponentMemoryUsage(Spline spline)
        {
            float size = 0;

            if (spline == null || spline.gameObject == null)
            {
                return size;
            }

            if (spline.componentMode != ComponentMode.REMOVE_FROM_BUILD || EHandlePrefab.IsPartOfAnyPrefab(spline.gameObject) )
            {
                size += Spline.dataUsage;
                size += spline.segments.Count * Segment.dataUsage;
            }

            size += (spline.followersInBuild + spline.deformationsInBuild) * SplineObject.dataUsage;
            size += (spline.followersInBuild + spline.deformationsInBuild) * MeshContainer.dataUsage;

            return size;
        }

        internal static float GetIndicatorActivationDistance(Spline spline)
        {
            float size = HandleUtility.GetHandleSize(spline.indicatorPosition) * 0.4f;
            size = Mathf.Clamp(size, 0, 4);
            return size;
        }

        internal static int GetNextControlPoint(Spline spline, bool backwards = false)
        {
            //General
            int cp = EHandleSelection.SelectedControlPoint;
            Segment oldSegment = spline.segments[SplineUtility.ControlPointIdToSegmentIndex(cp)];
            ControlHandle controlHandle = SplineUtility.GetControlHandleType(cp);

            //Go to next control point
            if (controlHandle == ControlHandle.ANCHOR)
                cp = backwards ? cp + 2: cp + 1;
            else if (controlHandle == ControlHandle.TANGENT_B)
                cp = backwards ? cp - 4 : cp - 2;
            else if(controlHandle == ControlHandle.TANGENT_A)
                cp = backwards ? cp - 1 : cp + 4;

            bool outOfRange = RangeCheck(SplineUtility.ControlPointIdToSegmentIndex(cp));

            //If line go to next anchor
            Segment newSegment = spline.segments[SplineUtility.ControlPointIdToSegmentIndex(cp)];
            bool jumpDirectlyToAnchor = newSegment.GetInterpolationType() == InterpolationType.LINE || EGlobalSettings.GetHandleType() == ControlHandleType.AUTO;
            if (!outOfRange && jumpDirectlyToAnchor)
            {
                int controlPointAnchorId = SplineUtility.SegmentIndexToControlPointId(SplineUtility.ControlPointIdToSegmentIndex(cp), ControlHandle.ANCHOR);
                if (oldSegment == newSegment)
                {
                    if(backwards) cp = controlPointAnchorId - 3;
                    else cp = controlPointAnchorId + 3;
                }

                outOfRange = RangeCheck(SplineUtility.ControlPointIdToSegmentIndex(cp));

                //If new segment is spline go back to first tangent. We dont want to jump to the anchor directly.
                newSegment = spline.segments[SplineUtility.ControlPointIdToSegmentIndex(cp)];
                if (!outOfRange && jumpDirectlyToAnchor)
                {
                    if (backwards) cp += 1;
                    else cp += 2;
                }
            }

            RangeCheck(SplineUtility.ControlPointIdToSegmentIndex(cp));

            if (jumpDirectlyToAnchor)
            {
                int segmentId = SplineUtility.ControlPointIdToSegmentIndex(cp);
                cp = SplineUtility.SegmentIndexToControlPointId(segmentId, ControlHandle.ANCHOR);
            }

            return cp;

            bool RangeCheck(int segmentId)
            {
                if (spline.segments.Count == segmentId || (spline.Loop && spline.segments.Count - 1 == segmentId))
                {
                    cp = 1002;
                    return true;
                }
                if (cp < 1000)
                {
                    cp = spline.Loop ? spline.segments.Count * 3 + 995 : spline.segments.Count * 3 + 998;
                    return true;
                }

                return false;
            }
        }

        internal static List<int> GetIntersectingControlPoints(Spline spline, Ray mouseRay)
        {
            controlPointsContainer.Clear();

            int iterations = spline.segments.Count;

            if (spline.Loop)
                iterations--;

            for (int i = 0; i < iterations; i++)
            {
                Vector3 point = spline.segments[i].GetPosition(ControlHandle.ANCHOR);
#if UNITY_EDITOR_OSX
                float distanceCheck = EHandleSegment.GetControlPointSize(point) * 2.5f;
#else
                float distanceCheck = EHandleSegment.GetControlPointSize(point) * 1.9f;
#endif
                float v = EMouseUtility.MouseDistanceToPoint(point, mouseRay);
                if (v < distanceCheck)
                {
                    controlPointsContainer.Add(SplineUtility.SegmentIndexToControlPointId(i, ControlHandle.ANCHOR));
                }

                point = spline.segments[i].GetPosition(ControlHandle.TANGENT_A);
#if UNITY_EDITOR_OSX
                distanceCheck = EHandleSegment.GetControlPointSize(point) * 2f;
#else
                distanceCheck = EHandleSegment.GetControlPointSize(point) * 1.5f;
#endif
                v = EMouseUtility.MouseDistanceToPoint(point, mouseRay);
                if (v < distanceCheck)
                {
                    controlPointsContainer.Add(SplineUtility.SegmentIndexToControlPointId(i, ControlHandle.TANGENT_A));
                }

                point = spline.segments[i].GetPosition(ControlHandle.TANGENT_B);
#if UNITY_EDITOR_OSX
                distanceCheck = EHandleSegment.GetControlPointSize(point) * 2f;
#else
                distanceCheck = EHandleSegment.GetControlPointSize(point) * 1.5f;
#endif
                v = EMouseUtility.MouseDistanceToPoint(spline.segments[i].GetPosition(ControlHandle.TANGENT_B), mouseRay);
                if (v < distanceCheck)
                {
                    controlPointsContainer.Add(SplineUtility.SegmentIndexToControlPointId(i, ControlHandle.TANGENT_B));
                }
            }

            return controlPointsContainer;
        }

        internal static Segment GetClosestSegment(HashSet<Spline> splines, Vector3 point, out float distance, out Spline spline, Segment segmentToSkip = null)
        {
            distance = 999999;
            Segment segment = null;
            spline = null;

            foreach (Spline spline2 in splines)
            {
                Vector3 closestPoint = spline2.bounds.ClosestPoint(point);
                float distanceToBounds = Vector3.Distance(closestPoint, point);

                if (distanceToBounds > 15)
                    continue;

                foreach(Segment s in spline2.segments)
                {
                    if(spline2.Loop && spline2.segments[spline2.segments.Count - 1] == s)
                        continue;

                    if (segmentToSkip == s)
                        continue;

                    float d = Vector3.Distance(s.GetPosition(ControlHandle.ANCHOR), point);

                    if (d < distance)
                    {
                        spline = spline2;
                        segment = s;
                        distance = d;
                    }
                }
            }

            return segment;
        }

        internal static Segment GetClosestSegmentToDirection(HashSet<Spline> splines, Vector3 direction, Vector3 origin, out float distance, out Spline spline, float maxDistance = 125)
        {
            distance = 999999;
            Segment segment = null;
            spline = null;

            foreach (Spline spline2 in splines)
            {
                float distanceToBounds = Vector3.Distance(spline2.transform.position, origin);

                if (distanceToBounds > maxDistance)
                    continue;

                foreach (Segment s in spline2.segments)
                {
                    if (spline2 == EHandleSelection.selectedSpline && EHandleSelection.SelectedControlPoint != 0)
                    {
                        //Skip self
                        if (s == spline2.segments[SplineUtility.ControlPointIdToSegmentIndex(EHandleSelection.SelectedControlPoint)])
                            continue;
                    }

                    Vector3 anchor = s.GetPosition(ControlHandle.ANCHOR);
                    Vector3 point = Utility.LineUtility.GetNearestPoint(origin, direction, anchor, out _);
                    float d = Vector3.Distance(anchor, point);

                    if (d < distance)
                    {
                        spline = spline2;
                        segment = s;
                        distance = d;
                    }
                }
            }

            return segment;
        }

        internal static Vector3 ClosestMousePointStepByStep(Spline spline, Ray mouseRay, float steps, out float distance, out float time)
        {
            time = 0;
            distance = 999999;
            float dCheck = 999999;
            Vector3 position = Vector3.zero;
            for (float t = 0; t < 1; t += steps)
            {
                Vector3 point = spline.GetPosition(t);
                float d2 = EMouseUtility.MouseDistanceToPoint(point, mouseRay);

                if (d2 < dCheck)
                {
                    dCheck = d2;
                    distance = d2;
                    time = t;
                    position = point;
                }
            }

            return position;
        }

        internal static Vector3 ClosestMousePoint(Spline spline, Ray mouseRay, int precision, out float distance, out float time, float steps = 5)
        {
            steps = 100 / spline.Length / steps;
            if (steps > 0.1f) steps = 0.1f;
            if (steps < 0.0001f) steps = 0.0001f;

            Vector3 position = ClosestMousePointStepByStep(spline, mouseRay, steps, out distance, out time);

            for (int i = precision; i > 0; i--)
            {
                steps = steps / 1.6f;
                float timeForwards = time + steps;
                float timeBackwards = time - steps;
                timeForwards = SplineUtility.GetValidatedTime(timeForwards, spline.Loop);
                timeBackwards = SplineUtility.GetValidatedTime(timeBackwards, spline.Loop);

                if (!spline.Loop)
                {
                    if (timeForwards > 1) timeForwards = 1;
                    if (timeBackwards < 0) timeBackwards = 0;
                }

                Vector3 pForward = spline.GetPosition(timeForwards);
                float dForward = EMouseUtility.MouseDistanceToPoint(pForward, mouseRay);

                Vector3 pBackwards = spline.GetPosition(timeBackwards);
                float dBackwards = EMouseUtility.MouseDistanceToPoint(pBackwards, mouseRay);

                if (dForward > dBackwards)
                {
                    position = pBackwards;
                    time = timeBackwards;
                    distance = dBackwards;
                }
                else
                {
                    position = pForward;
                    time = timeForwards;
                    distance = dForward;
                }
            }

            return position;
        }

        internal static void AlignSelectedSegments(Spline spline)
        {
            int selectedSegment = SplineUtility.ControlPointIdToSegmentIndex(EHandleSelection.SelectedControlPoint);

            int startSegment = selectedSegment;
            int endSegment = selectedSegment;

            controlPointsContainer.Clear();
            EHandleSelection.GetSelectedControlPoints(controlPointsContainer);
            foreach (int anchorId in controlPointsContainer)
            {
                int index = SplineUtility.ControlPointIdToSegmentIndex(anchorId);

                if (index < startSegment)
                    startSegment = index;

                if(index > endSegment)
                    endSegment = index;
            }

            Vector3 anchorStart = spline.segments[startSegment].GetPosition(ControlHandle.ANCHOR);
            Vector3 tangentAStart = spline.segments[startSegment].GetPosition(ControlHandle.TANGENT_A);
            Vector3 tangentBStart = spline.segments[startSegment].GetPosition(ControlHandle.TANGENT_B);
            float tangentAStartLength = Vector3.Distance(anchorStart, tangentAStart);
            float tangentBStartLength = Vector3.Distance(anchorStart, tangentBStart);

            Vector3 anchorEnd = spline.segments[endSegment].GetPosition(ControlHandle.ANCHOR);
            Vector3 tangentAEnd = spline.segments[endSegment].GetPosition(ControlHandle.TANGENT_A);
            Vector3 tangentBEnd = spline.segments[endSegment].GetPosition(ControlHandle.TANGENT_B);
            float tangentAEndLength = Vector3.Distance(anchorEnd, tangentAEnd);
            float tangentBEndLength = Vector3.Distance(anchorEnd, tangentBEnd);

            Vector3 direction = (anchorEnd - anchorStart).normalized;

            spline.segments[startSegment].SetPosition(ControlHandle.TANGENT_A, anchorStart + direction * tangentAStartLength);
            spline.segments[startSegment].SetPosition(ControlHandle.TANGENT_B, anchorStart - direction * tangentBStartLength);

            spline.segments[endSegment].SetPosition(ControlHandle.TANGENT_A, anchorEnd + direction * tangentAEndLength);
            spline.segments[endSegment].SetPosition(ControlHandle.TANGENT_B, anchorEnd - direction * tangentBEndLength);

            if(selectedSegment != startSegment && selectedSegment != endSegment)
                EHandleSegment.MarkForDeletion(selectedSegment);

            controlPointsContainer.Clear();
            EHandleSelection.GetSelectedControlPoints(controlPointsContainer);
            foreach (int id in controlPointsContainer)
            {
                int segmentId = SplineUtility.ControlPointIdToSegmentIndex(id);

                if (segmentId == startSegment || segmentId == endSegment)
                    continue;

                EHandleSegment.MarkForDeletion(segmentId);
            }

            EHandleSegment.DeleteAndUnlinkMarked(spline, true);
        }

        private static void UpdateNewControlPointTangentDistance(Event e)
        {
#if UNITY_EDITOR_OSX
            if (e.type != EventType.KeyDown || (e.keyCode != KeyCode.UpArrow && e.keyCode != KeyCode.DownArrow) || !e.shift)
                return;
#else
            if (e.type != EventType.ScrollWheel || !e.shift)
                return;
#endif

            float delta = 0;
            bool gridVisibility = EGlobalSettings.GetPlaneVisibility() && EGlobalSettings.GetPlaneType() == PlaneType.GRID;
            float gridSize = EGlobalSettings.GetGridSize();

#if UNITY_EDITOR_OSX
            if (e.keyCode == KeyCode.UpArrow)
#else
            if (e.delta.x > 0)
#endif
            {
                if (gridVisibility) delta += gridSize;
                else delta += 2;
            }

#if UNITY_EDITOR_OSX
            else if (e.keyCode == KeyCode.DownArrow)
#else
            else if (e.delta.x < 0)
#endif
            {
                if (gridVisibility) delta -= gridSize;
                else delta -= 2;
            }

            float value = EGlobalSettings.GetNewSegmentTangentDistance();
            EGlobalSettings.SetNewSegmentTangentDistance(value + delta);

            e.Use();
        }

        private static int GetHotControlId()
        {
            if (hotControlId == 0) hotControlId = GUIUtility.GetControlID(FocusType.Passive);
            return hotControlId;
        }
    }
}