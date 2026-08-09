// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleTool.cs
//
// Author: Mikael Danielsson
// Date Created: 17-04-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

using SplineArchitect.CustomTools;
using SplineArchitect.Ui;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public static class EHandleTool
    {
        internal static int rotationHandleID = -99999;
        internal static int scaleHandleID = -99999;
        private static Vector3[] normalsContainer = new Vector3[3];
        private static PivotMode pivotMode;
        private static Quaternion controlPointRotationHandle = Quaternion.identity;

        internal static void BeforeSceneGUIGlobal(SceneView sceneView, Event e)
        {
            //Update PositionTool orientation.
            if (e.type == EventType.MouseUp)
            {
                controlPointRotationHandle = Quaternion.identity;

                Spline spline = EHandleSelection.selectedSpline;
                if (spline != null && spline.segments.Count > 1)
                {
                    UpdateOrientationForPositionTool(sceneView, spline);
                    EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                    {
                        selected.activationPosition = selected.localSplinePosition;
                    });
                }
            }

            //Needs to be last
            if (e.type == EventType.Repaint)
            {
                if (pivotMode != Tools.pivotMode)
                {
                    pivotMode = Tools.pivotMode;
                    ActivatePositionToolForControlPoint(EHandleSelection.selectedSpline);
                }

                //Needs to reset rotationHandleID and scaleHandleID. Else the selection system can belive that they are selected when they are not, and you cant selected splines becouse of that.
                rotationHandleID = -99999;
                scaleHandleID = -99999;
                HideOrUnhideTools();
            }
        }

        internal static void OnSceneGUI(Spline spline, Event e, SceneView sceneView)
        {
            SplineObject so = EHandleSelection.selectedSplineObject;

            if (EHandleSelection.SelectedControlPoint == 0 && so != null && so.SplineParent != null && so.Type != SplineObjectType.NONE)
            {
                if (so.SoParent != null && so.SoParent.Type == SplineObjectType.FOLLOWER)
                    return;

                if (Tools.current == Tool.Move) 
                    SplineObjectMoveTool(spline, so);
                else if (Tools.current == Tool.Rotate) 
                    SplineObjectRotateTool(spline, so);
                else if (Tools.current == Tool.Scale) 
                    SplineObjectScaleTool(spline, so);

                return;
            }

            int segmentIndex = SplineUtility.ControlPointIdToSegmentIndex(EHandleSelection.SelectedControlPoint);

            if (segmentIndex < spline.segments.Count && segmentIndex >= 0)
            {
                Segment segment = spline.segments[segmentIndex];

                if (Tools.current == Tool.Move) 
                    ControlPointMoveTool(spline, segment);
                else if (Tools.current == Tool.Rotate && (Tools.pivotMode == PivotMode.Pivot || EHandleSelection.GetSelectedAnchorCount(spline) == 1)) 
                    ControlPointRotationTool(spline, segment);
                else if (Tools.current == Tool.Rotate)
                    ControlPointRotationToolMultiselect(spline);
            }
        }

        internal static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if(state == PlayModeStateChange.EnteredEditMode)
            {
                Spline spline = EHandleSelection.selectedSpline;

                if (spline == null)
                    return;

                SplineObject so = EHandleSelection.selectedSplineObject;

                if (so == null) ActivatePositionToolForControlPoint(spline);
                else ActivatePositionToolForSplineObject(spline, so);
            }
        }

        internal static void OnUndoGlobal()
        {
            ActivatePositionToolForControlPoint(EHandleSelection.selectedSpline);

            //Need to run last, else it will update before positions has changed and will get the wrong position.
            EActionDelayed.Add(() => {
                UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), EHandleSelection.selectedSpline);
            }, 0, 0, EActionDelayed.ActionFlag.LATE | EActionDelayed.ActionFlag.FRAMES);

            EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
            {
                selected.activationPosition = selected.localSplinePosition;
            });
        }

        internal static void OnPivotRotationChanged()
        {
            UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), EHandleSelection.selectedSpline);
        }

        private static void HideOrUnhideTools()
        {
            Spline spline = EHandleSelection.selectedSpline;
            SplineObject so = EHandleSelection.selectedSplineObject;
            Tools.hidden = false;
            PositionTool.Deactivate();

            if (spline == null)
                return;

            if (so != null)
            {
                //None types done use any custom tool
                if (so.Type == SplineObjectType.NONE)
                    return;

                if (spline.segments.Count < 2)
                    return;

                if(so.SplineParent == null)
                    return;
            }
            else 
            {
                if (EHandleSelection.SelectedControlPoint <= 0)
                    return;
            }

            Tools.hidden = true;

            if (Tools.current == Tool.Move)
                PositionTool.Activate();
        }

        private static void SplineObjectMoveTool(Spline spline, SplineObject so)
        {
            //Lock posiiton tool if any scale is zero
            Vector3 combinedScale = SplineObjectUtility.GetCombinedParentScales(so);
            if (GeneralUtility.IsZero(combinedScale.x) || GeneralUtility.IsZero(combinedScale.y) || GeneralUtility.IsZero(combinedScale.z))
            {
                PositionTool.lockedWarningMsg = "[Spline Architect] Position handle is locked, scale can't be zero!";
                PositionTool.locked = true;
            }

            if (!PositionTool.Drag(out Vector3 dif))
                return;

            Vector3 combinedParentScale = SplineObjectUtility.GetCombinedParentScales(so);

            EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
            {
                Vector3 scaledDif = new Vector3(dif.x / combinedParentScale.x, dif.y / combinedParentScale.y, dif.z / combinedParentScale.z);
                scaledDif = Vector3.Scale(so.transform.localScale, scaledDif);
                Vector3 newPosition = selected.activationPosition + scaledDif;
                selected.localSplinePosition.x = Mathf.Round(newPosition.x * 100) / 100;
                selected.localSplinePosition.y = Mathf.Round(newPosition.y * 100) / 100;
                selected.localSplinePosition.z = Mathf.Round(newPosition.z * 100) / 100;

                selected.localSplinePosition = EHandleSplineObject.SnapPosition(selected.localSplinePosition);

            }, "Moved object");
        }

        private static void SplineObjectRotateTool(Spline spline, SplineObject so)
        {
            //Draw on top
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            //Normals
            float time = so.splinePosition.z / spline.Length;
            if (so.AlignToEnd) time = (spline.Length - so.splinePosition.z) / spline.Length;
            float fixedTime = spline.TimeToFixedTime(time);
            spline.GetNormalNonAlloc(normalsContainer, fixedTime, Space.World, so.RotationMode);

            if (so.AlignToEnd)
            {
                normalsContainer[0] = -normalsContainer[0];
                normalsContainer[2] = -normalsContainer[2];
            }

            //Rotations
            Quaternion parentRotations = SplineObjectUtility.GetCombinedParentRotations(so.SoParent);
            Quaternion localSplineRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            Quaternion combinedRotations = localSplineRotation * parentRotations;
            Quaternion handleRotation = combinedRotations * so.localSplineRotation;
            if (GeneralUtility.IsZero(handleRotation)) handleRotation = Quaternion.identity;

            //Pivot
            Vector3 pivot = spline.SplinePositionToWorldPosition(so.localSplinePosition, 
                so.transform.parent, SplineObjectUtility.GetCombinedParentMatrixs(so.SoParent), so.AlignToEnd, so.RotationMode);

            //Tool handle
            rotationHandleID = GUIUtility.GetControlID("RotationHandle".GetHashCode(), FocusType.Passive) + 1;
            Quaternion newRotation = Handles.RotationHandle(handleRotation, pivot);
            newRotation = Quaternion.Inverse(combinedRotations) * newRotation;

            if (GUI.changed)
            {
                Vector3 dif = newRotation.eulerAngles - so.localSplineRotation.eulerAngles;
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.localSplineRotation = Quaternion.Euler(selected.localSplineRotation.eulerAngles + dif);
                    WindowBase.RepaintAll();
                }, "Rotated object");
            }
        }

        private static void SplineObjectScaleTool(Spline spline, SplineObject so)
        {
            //Draw on top
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            //Normals
            float time = so.splinePosition.z / spline.Length;
            if (so.AlignToEnd) time = (spline.Length - so.splinePosition.z) / spline.Length;
            float fixedTime = spline.TimeToFixedTime(time);
            spline.GetNormalNonAlloc(normalsContainer, fixedTime);

            if (so.AlignToEnd)
            {
                normalsContainer[0] = -normalsContainer[0];
                normalsContainer[2] = -normalsContainer[2];
            }

            //Handle rotation
            Quaternion parentRotations = SplineObjectUtility.GetCombinedParentRotations(so.SoParent);
            Quaternion localSplineRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            Quaternion handleRotation = localSplineRotation * so.splineRotation;
            if(GeneralUtility.IsZero(handleRotation)) handleRotation = Quaternion.identity;

            //Pivot
            Vector3 pivot = spline.SplinePositionToWorldPosition(so.localSplinePosition, so.transform.parent, SplineObjectUtility.GetCombinedParentMatrixs(so.SoParent), so.AlignToEnd);

            //Tool handle
            scaleHandleID = GUIUtility.GetControlID("ScaleHandle".GetHashCode(), FocusType.Passive) + 1;
            Vector3 newScale = Handles.ScaleHandle(so.transform.localScale, pivot, handleRotation, HandleUtility.GetHandleSize(pivot));

            if (GUI.changed)
            {
                Vector3 dif = newScale - so.transform.localScale;
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.transform.localScale += dif;
                }, "Scaled object", true);
            }
        }

        private static void ControlPointMoveTool(Spline spline, Segment segment)
        {
            Vector3 dif;
            Vector3 groundNormal = Vector3.up;
            bool toolActive;

            if (PositionTool.activePart == PositionTool.ActivePart.SURFACE)
                toolActive = PositionTool.DragOnSurface(out dif, out groundNormal);
            else
                toolActive = PositionTool.Drag(out dif);

            if (toolActive)
            {
                ControlHandle type = SplineUtility.GetControlHandleType(EHandleSelection.SelectedControlPoint);
                EHandleUndo.RecordNow(spline, "Move control point: " + EHandleSelection.SelectedControlPoint);
                EHandleUndo.RecordNow(spline.undoState, "Move control point: " + EHandleSelection.SelectedControlPoint);
                EHandleSegment.SegmentMovement(spline, segment, type, dif);
            }
        }

        private static void ControlPointRotationTool(Spline spline, Segment segment)
        {
            bool oldEnable = GUI.enabled;
            if (segment.IsStatic || spline.splineType != SplineType.NOT_USED || segment.linkTarget == LinkTarget.SPLINE_CONNECTOR)
                GUI.enabled = false;

            Quaternion handleRotation = segment.Rotation;
            if (GeneralUtility.IsZero(handleRotation, 0.001f)) handleRotation = Quaternion.identity;
            rotationHandleID = GUIUtility.GetControlID("RotationHandleControlPoint".GetHashCode(), FocusType.Passive) + 1;
            Quaternion newRotation = Handles.RotationHandle(handleRotation, segment.GetPosition(ControlHandle.ANCHOR));

            if (GUI.changed)
            {
                Quaternion delta = newRotation * Quaternion.Inverse(segment.Rotation);
                EHandleUndo.RecordNow(spline, "Rotated segment");
                EHandleUndo.RecordNow(spline.undoState, "Rotated segment");
                segment.Rotation = newRotation;

                EHandleSelection.UpdateSelectedSegmentsRecordUndo(spline, (s) =>
                {
                    if (s == segment)
                        return;

                    s.Rotation = delta * s.Rotation;
                }, "Rotated segments");
                WindowBase.RepaintAll();
            }

            GUI.enabled = oldEnable;
        }

        private static void ControlPointRotationToolMultiselect(Spline spline)
        {
            Vector3 pivot = EHandleSelection.GetCenterFromAnchorSelection(spline);
            rotationHandleID = GUIUtility.GetControlID("RotationHandleControlPointMultiselect".GetHashCode(), FocusType.Passive) + 1;
            Quaternion newRotation = Handles.RotationHandle(controlPointRotationHandle, pivot);

            if (GUI.changed)
            {
                Quaternion delta = newRotation * Quaternion.Inverse(controlPointRotationHandle);
                EHandleUndo.RecordNow(spline, "Rotate Points");
                EHandleUndo.RecordNow(spline.undoState, "Rotate Points");
                for (int i = 0; i < spline.SegmentCount; i++)
                {
                    Segment s = spline.GetSegmentAtIndex(i);

                    bool selected = EHandleSelection.IsPrimiarySelection(s);

                    if(!selected)
                        selected = EHandleSelection.IsSecondarySelection(s);

                    if (!selected)
                        continue;

                    Vector3 anchor = s.GetPosition(ControlHandle.ANCHOR);
                    Vector3 offset = anchor - pivot;
                    anchor = pivot + delta * offset;
                    s.SetPosition(ControlHandle.ANCHOR, anchor);

                    Vector3 tangentA = s.GetPosition(ControlHandle.TANGENT_A);
                    offset = tangentA - pivot;
                    tangentA = pivot + delta * offset;
                    s.SetPosition(ControlHandle.TANGENT_A, tangentA);

                    Vector3 tangentB = s.GetPosition(ControlHandle.TANGENT_B);
                    offset = tangentB - pivot;
                    tangentB = pivot + delta * offset;
                    s.SetPosition(ControlHandle.TANGENT_B, tangentB);

                    EHandleSegment.LinkMovement(s);
                }

                controlPointRotationHandle = newRotation;
                WindowBase.RepaintAll();
            }
        }

        public static void UpdateOrientationForPositionTool()
        {
            UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), EHandleSelection.selectedSpline);
        }

        public static void UpdateOrientationForPositionTool(SceneView sceneView, Spline spline)
        {
            if (spline == null)
                return;

            SplineObject selectedSo = EHandleSelection.selectedSplineObject;

            if (EHandleSelection.SelectedControlPoint > 0)
            {
                int segementId = SplineUtility.ControlPointIdToSegmentIndex(EHandleSelection.SelectedControlPoint);

                if (segementId >= spline.segments.Count)
                    segementId = spline.segments.Count - 1;

                Segment segment = spline.GetSegmentAtIndex(segementId);

                PositionTool.UpdateOrientation(sceneView.camera.transform.position, segment.Rotation * Vector3.forward, segment.Rotation * Vector3.up);
            }
            else if (selectedSo != null)
                ActivatePositionToolForSplineObject(spline, selectedSo);
        }

        public static void ActivatePositionToolForSplineObject(Spline spline, SplineObject so)
        {
            if (spline == null)
                return;

            if (so == null)
                return;

            if (so.transform.parent == null)
                return;

            SceneView sceneView = EHandleSceneView.GetCurrent();

            float time = so.splinePosition.z / spline.Length;
            if(so.AlignToEnd) time = (spline.Length - so.splinePosition.z) / spline.Length;
            float fixedTime = spline.TimeToFixedTime(time);
            spline.GetNormalNonAlloc(normalsContainer, fixedTime, Space.World, so.RotationMode);
            Quaternion splineRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            Quaternion combinedRotation = splineRotation * SplineObjectUtility.GetCombinedParentRotations(so.SoParent);

            normalsContainer[0] = combinedRotation * Vector3.right;
            normalsContainer[1] = combinedRotation * Vector3.up;
            normalsContainer[2] = combinedRotation * Vector3.forward;

            if (so.AlignToEnd)
            {
                normalsContainer[0] = -normalsContainer[0];
                normalsContainer[2] = -normalsContainer[2];
            }

            Vector3 position = spline.SplinePositionToWorldPosition(so.localSplinePosition, 
                so.transform.parent, SplineObjectUtility.GetCombinedParentMatrixs(so.SoParent), so.AlignToEnd, so.RotationMode);
            PositionTool.ActivateAndSetPosition(PositionTool.ActivateType.SPLINE_OBJECT, position, sceneView.camera.transform.position, normalsContainer[2], normalsContainer[1], false);

            so.activationPosition = so.localSplinePosition;

            foreach (SplineObject so2 in EHandleSelection.selectedSplineObjects)
            {
                so2.activationPosition = so2.localSplinePosition;
            }

            EHandleEvents.InvokeAfterSplineObjectActivatePositionTool(so);
        }

        public static void ActivatePositionToolForControlPoint(Spline spline)
        {
            if (spline == null)
                return;

            if (spline.SegmentCount == 0)
                return;

            if (EHandleSelection.SelectedControlPoint <= 0)
                return;

            if (EHandleSelection.selectedSplineObject != null)
                return;

            // Segment id
            int segementId = SplineUtility.ControlPointIdToSegmentIndex(EHandleSelection.SelectedControlPoint);
            if (segementId >= spline.segments.Count) segementId = spline.segments.Count - 1;

            // Get segment
            Segment segment = spline.segments[segementId];

            // Set activationType
            ControlHandle segementType = SplineUtility.GetControlHandleType(EHandleSelection.SelectedControlPoint);
            ControlHandle type = SplineUtility.GetControlHandleType(EHandleSelection.SelectedControlPoint);
            PositionTool.ActivateType activationType = type != ControlHandle.ANCHOR ? PositionTool.ActivateType.TANGENT : PositionTool.ActivateType.ANCHOR;

            Vector3 point = segment.GetPosition(type);
            if (Tools.pivotMode == PivotMode.Center)
                point = EHandleSelection.GetCenterFromAnchorSelection(spline);

            // Activate position tool
            PositionTool.ActivateAndSetPosition(activationType,
                                                point,
                                                EHandleSceneView.GetCurrent().camera.transform.position,
                                                segment.Rotation * Vector3.forward,
                                                segment.Rotation * Vector3.up,
                                                true);

            // Lock if needed
            if (segment.linkTarget == LinkTarget.SPLINE_CONNECTOR)
            {
                PositionTool.locked = true;
                PositionTool.lockedWarningMsg = "[Spline Architect] Can't move position handle when connected to a Spline Connector.";
            }
            else
            {
                PositionTool.locked = false;
            }
        }
    }
}