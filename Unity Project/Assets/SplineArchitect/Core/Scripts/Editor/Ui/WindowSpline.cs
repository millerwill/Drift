// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: WindowControlpanel.cs
//
// Author: Mikael Danielsson
// Date Created: 05-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

using SplineArchitect.Libraries;
using SplineArchitect.Utility;
using SplineArchitect.ScriptableObjects;

namespace SplineArchitect.Ui
{
    public class WindowSpline : WindowBase
    {
        const int maxSplineNameLength = 25;
        const int maxNoisesLayers = 16;
        const float extendedMenuMargin = 3;

        private string windowTitle = "";
        private static GUIContent guiContentContainer = new GUIContent("Spline");

        private static string[] optionsComponentMode = new string[] { "Remove from build", "Inactivate after scene load", "Active" };
        private static string[] optionsDeformationType = new string[] { "Update", "Late update", "Do nothing" };
        private static Dictionary<Spline, AnimationCurve> scaleProfileDisplayCurves = new Dictionary<Spline, AnimationCurve>();

        //Addons
        private static List<(string, Action<Spline, bool>)> addonsDrawWindow = new();
        private static List<(string, GUIContent, GUIContent)> addonsButtons = new();
        private static List<(string, Func<Spline, Rect>)> addonsCalcWindowSize = new();

        protected override void OnGUIExtended()
        {
            Spline spline = EHandleSelection.selectedSpline;
            SplineObject so = EHandleSelection.selectedSplineObject;

            if (so != null && so.SoParent == null && spline != null && so.transform.parent != spline.transform)
                spline = null;

            EUiUtility.ResetGetBackgroundStyleId();
            if (spline == null)
            {
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateLabelField($"<b>No selected spline</b> ", LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                return;
            }
            else if(EGlobalSettings.GetControlPanelWindowMinimized())
            {
                //Minimize
                EUiUtility.CreateButton(ButtonType.SUB_MENU, LibraryGUIContent.iconMaximize, 25, 25, () =>
                {
                    EActionToSceneGUI.Add(() =>
                    {
                        EGlobalSettings.SetControlPanelWindowMinimized(false);
                    }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                    EHandleSceneView.RepaintCurrent();
                });

                return;
            }

            bool leftMouseUp = false;
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0) leftMouseUp = true;

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
            #region Top

            //Title
            windowTitle = spline.name;
            if (windowTitle.Length > maxSplineNameLength) windowTitle = $"{windowTitle.Substring(0, maxSplineNameLength)}..";
            if (EHandleSelection.selectedSplines.Count > 0) windowTitle = $"{windowTitle} + ({EHandleSelection.selectedSplines.Count})"; 
            EUiUtility.CreateLabelField($"<b>{windowTitle}</b>", LibraryGUIStyle.textHeaderBlack, true);

            if(!EGlobalSettings.GetIsWindowsFloating())
            {
                GUILayout.FlexibleSpace();
                //Minimize
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconMinimize, 19, 14, () =>
                {
                    EActionToSceneGUI.Add(() =>
                    {
                        EGlobalSettings.SetControlPanelWindowMinimized(true);
                    }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                    EHandleSceneView.RepaintCurrent();
                });
                //Close
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconClose, 19, 14, () =>
                {
                    EActionToSceneGUI.Add(() =>
                    {
                        CloseWindow();
                    }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                    EHandleSceneView.RepaintCurrent();
                });
            }

            GUILayout.EndHorizontal();

            EUiUtility.CreateHorizontalBlackLine();

            //Toolbar
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundToolbar);
            //Toggle loop
            EUiUtility.CreateButtonToggle(ButtonType.DEFAULT, LibraryGUIContent.iconLoop, LibraryGUIContent.iconLoopActive, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                {
                    bool loop = !selected.Loop;
                    selected.SetLoop(loop, false);
                    EHandleSpline.EnableDisableLoop(selected, loop);
                    EHandleEvents.InvokeAfterSplineLoop(selected);
                }, "Toggle loop");
                EHandleSceneView.RepaintCurrent();
            }, spline.Loop);

            EUiUtility.CreateSeparator();

            //Select spline or select all anchors
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconSelectSpline, 35, 19, () =>
            {
                EHandleUndo.RecordNow(spline, "Selected Spline");

                if (Selection.activeTransform != null && Selection.activeTransform == spline.transform && EHandleSelection.SelectedControlPoint == 0)
                {
                    EHandleUndo.RecordNow(spline, "Selected all anchors");
                    int totalAnchors = spline.segments.Count;
                    int[] anchors = new int[totalAnchors - 1];

                    for (int i = 0; i < totalAnchors - 1; i++)
                        anchors[i] = i * 3 + 1003;

                    EHandleSelection.SelectAllAnchors(spline);
                }
                // Select and center spline
                else
                {
                    Selection.objects = null;
                    Selection.activeTransform = spline.transform;
                    EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
                    {
                        undoState.selectedControlPoints.Clear();
                        undoState.selectedControlPoint = 0;
                    }, "Selected spline");

                    //Also need to record the transform becouse of transform.screenPos change
                    EHandleUndo.RecordNow(spline.transform, "Selected Spline");
                    spline.TransformToCenter(out Vector3 dif);
                    if (!GeneralUtility.IsZero(dif))
                        spline.MarkEditorCacheDirty();
                }

                EHandleSelection.ForceUpdate();
                EHandleSceneView.RepaintCurrent();
            });

            //Reverse Control points
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconReverse, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                {
                    selected.ReverseSegments();
                    EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
                    {
                        undoState.selectedControlPoints.Clear();
                        undoState.selectedControlPoint = 0;
                    }, "Reverse control points");

                    foreach (Segment s in selected.segments)
                        s.Unlink();

                    EHandleEvents.InvokeAfterSplineReverse(selected);
                    selected.MarkEditorCacheDirty();
                }, "Reverse control points");

                if (EHandleSelection.selectedSplineObject != null)
                {
                    EActionDelayed.Add(() =>
                    {
                        EHandleTool.ActivatePositionToolForSplineObject(spline, EHandleSelection.selectedSplineObject);
                    }, 0, 0, EActionDelayed.ActionFlag.FRAMES);
                }
                else EHandleTool.ActivatePositionToolForControlPoint(spline);
                EHandleSceneView.RepaintCurrent();
            });

            //Join selected splines
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconJoin, 35, 19, () =>
            {
                EHandleSpline.JoinSelection();
                EHandleEvents.InvokeAfterSplineJoin(spline);
                EHandleSceneView.RepaintCurrent();
            }, EHandleSelection.selectedSplines.Count > 0);

            //Align Control points
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconAlign, 35, 19, () =>
            {
                EHandleUndo.RecordNow(spline, "Aligned control points");
                EHandleSpline.AlignSelectedSegments(spline);
                EHandleSelection.UpdateSelectedControlPointsRecordUndo((undoState) =>
                {
                    undoState.selectedControlPoints.Clear();
                    undoState.selectedControlPoint = 0;
                }, "Aligned control points");
                EHandleSceneView.RepaintCurrent();
            }, EHandleSelection.GetSelectedAnchorCount(spline) > 1);
            GUILayout.EndHorizontal();

            if (EGlobalSettings.GetSubmenusOnTop()) DrawBottom(spline);
            #endregion

            #region General
            // SPLINE MENU DEFORMATION
            if (EHandleUi.ActiveSplineMenu == "general")
            {
                //Sub title
                EUiUtility.CreateSection("GENERAL");

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                if (!EHandlePrefab.IsPrefabRoot(spline.gameObject) && (EHandlePrefab.IsPartOfAnyPrefab(spline.gameObject) || EHandlePrefab.prefabStageOpen))
                    EUiUtility.CreateErrorWarningMessageIcon(LibraryGUIContent.warningMsgComponentOptionPrefab, true);
                else
                    EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineComponentMode, true);
                EUiUtility.CreatePopupField("Component:", 110, (int)spline.componentMode, optionsComponentMode, (int newValue) =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.componentMode = (ComponentMode)newValue;
                        if (selected.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                        {
                            for (int i = 0; i < spline.AllSplineObjectCount; i++)
                            {
                                SplineObject so = spline.GetSplineObjectAtIndex(i);
                                EHandleUndo.RecordNow(so);
                                so.componentMode = ComponentMode.REMOVE_FROM_BUILD;
                            }
                        }
                        EditorUtility.SetDirty(selected);
                    }, "Change component mode");
                }, -1, true);
                GUILayout.EndHorizontal();

                EUiUtility.CreatePopupField("Noise group:", 110, (int)spline.noiseGroup, EHandleUi.optionsNoiseGroupsAndNone, (int newValue) =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.noiseGroup = (NoiseGroup)newValue;
                        EditorUtility.SetDirty(selected);
                    }, "Change noise group");
                });

                // Scale profile
                EUiUtility.CreateCurveField("Scale profile:", spline.scaleProfileCurve, GetScaleProfileDisplayCurve(spline), (newCurve) =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.scaleProfileCurve.CopyFrom(newCurve);
                        spline.MarkCacheDirty();
                    }, "Changed scale curve", EHandleUndo.RecordType.REGISTER_COMPLETE_OBJECT);
                }, 111);

                //Normal resolution
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgNormalResolution, true);

                EUiUtility.CreateSliderAndInputField("Normal resolution:", spline.GetNormalResolution(), (newValue, changeBySlider) => {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.SetResolutionNormal(Mathf.Round(newValue));
                        EditorUtility.SetDirty(selected);
                    }, "Changed normal resolution");
                }, 100, 5000, 70, 38, 0, true);
                GUILayout.EndHorizontal();

                //Spline resolution
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineResolution, true);
                EUiUtility.CreateSliderAndInputField("Spline resolution:", spline.GetSplineResolution(true), (newValue, changeBySlider) => {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.SetSplineResolution(Mathf.Round(newValue));
                        EditorUtility.SetDirty(spline);
                    }, "Changed spline resolution");
                }, 10, 5000, 70, 38, 0, true);
                GUILayout.EndHorizontal();

                wantsMouseMove = true;
                autoRepaintOnSceneChange = true;

                //Sub title
                EUiUtility.CreateSubSection("SPLINE RENDERING", EHandleUi.RenderingMinimized, () =>
                {
                    EHandleUi.UpdateMenuStateRecordUndo((undoState) => 
                    {
                        undoState.renderingMinimized = !undoState.renderingMinimized;
                    }, "Toggle rendering");
                });

                if (!EHandleUi.RenderingMinimized)
                {
                    GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                    EUiUtility.CreateToggleField("Render in game:", spline.RenderInGame, (newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            selected.RenderInGame = newValue;
                            InternalEditorUtility.RepaintAllViews();
                        }, "Toggle render in game");
                    }, spline.componentMode == ComponentMode.ACTIVE, true, 100, 18);

                    GUILayout.Space(34);

                    EUiUtility.CreateToggleField("Occluded:", spline.Occluded, (newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            selected.Occluded = newValue;
                            InternalEditorUtility.RepaintAllViews();
                        }, "Toggle render occluded");
                    }, true, true, 66, 18);
                    GUILayout.EndHorizontal();

                    EUiUtility.CreateColorField("Color:", spline.color, (newColor) =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            selected.color = newColor;
                            EditorUtility.SetDirty(selected);
                        }, "Changed spline color");
                    }, 110);

                    EUiUtility.CreateSliderAndInputField("Width:", spline.Width, (newValue, changeBySlider) => {

                        if (newValue < 0.01f)
                            return;

                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            selected.Width = newValue;
                            EditorUtility.SetDirty(selected);
                        }, "Changed render width");
                    }, 0.1f, 10, 70, 38, 0);
                }

                if (spline.componentMode == ComponentMode.ACTIVE)
                {
                    //Sub title
                    EUiUtility.CreateSubSection("DEFORMATION SCHEDULING", EHandleUi.SchedulingMinimized, () =>
                    {
                        EHandleUi.UpdateMenuStateRecordUndo((undoState) =>
                        {
                            undoState.schedulingMinimized = !undoState.schedulingMinimized;
                        }, "Toggle deformation scheduling");
                    });

                    if(!EHandleUi.SchedulingMinimized)
                    {
                        EUiUtility.CreatePopupField("Start:", 110, (int)spline.jobStartType, optionsDeformationType, (int newValue) =>
                        {
                            EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                            {
                                selected.jobStartType = (JobType)newValue;
                                EditorUtility.SetDirty(selected);
                            }, "Change job start");
                        }, -1);

                        EUiUtility.CreatePopupField("End:", 110, (int)spline.jobEndType, optionsDeformationType, (int newValue) =>
                        {
                            EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                            {
                                selected.jobEndType = (JobType)newValue;
                                EditorUtility.SetDirty(selected);
                            }, "Change job end");
                        }, -1);

                        EUiUtility.CreateSliderAndInputField("Job interval:", spline.jobInterval, (newValue, changeBySlider) => {
                            EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                            {
                                selected.jobInterval = Mathf.RoundToInt(newValue);
                                EditorUtility.SetDirty(selected);
                            }, "Changed job interval");
                        }, 0, 32, 70, 38, 0);

                        EUiUtility.CreateSliderAndInputField("Initial delay:", spline.initialJobDelay, (newValue, changeBySlider) => {
                            EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                            {
                                selected.initialJobDelay = Mathf.RoundToInt(newValue);
                                EditorUtility.SetDirty(selected);
                            }, "Changed initial delay");
                        }, 0, 32, 70, 38, 0);

                        EUiUtility.CreateSliderAndInputField("Frame spreading:", spline.FrameSpreading - 1, (newValue, changeBySlider) => {
                            EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                            {
                                selected.FrameSpreading = (Mathf.RoundToInt(newValue) + 1);
                                EditorUtility.SetDirty(selected);
                            }, "Changed frame spreading");
                        }, 0, 16, 70, 38, 0);
                    }
                }
            }
            #endregion

            #region Noise
            // SPLINE MENU NOISE
            else if (EHandleUi.ActiveSplineMenu == "noise")
            {
                EUiUtility.CreateSection("NOISE EFFECTS");

                int selectedIndex = EHandleUi.SelectedNoiseLayerIndex;

                for (int i = 0; i < spline.noises.Count; i++)
                {
                    //Used for getting the correct value when commiting delayed float field values.
                    int index = i;
                    NoiseLayer noise = spline.noises[i];
                    bool isSelected = selectedIndex == i;

                    GUIStyle backgroundStyleHeader = EUiUtility.GetBackgroundStyle();
                    if (isSelected) backgroundStyleHeader = LibraryGUIStyle.backgroundSelectedLayerHeader;

                    GUILayout.BeginHorizontal(backgroundStyleHeader);

                    string text = EConversionUtility.CapitalizeString($"{noise.type}");
                    string text2 = EConversionUtility.CapitalizeString($"{noise.group}");
                    EUiUtility.CreateLabelField($"{i + 1} {text} ({text2})", isSelected ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault, true);

                    EUiUtility.CreateSpaceWidth(12);

                    //Move down
                    EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconDownArrow, 22, 19, () =>
                    {
                        EHandleUndo.RecordNow(spline, "Moved noise layer down");
                        NoiseLayer noiseA = spline.noises[i];
                        NoiseLayer noiseB = spline.noises[i + 1];
                        spline.noises[i] = noiseB;
                        spline.noises[i + 1] = noiseA;

                        EHandleUi.UpdateMenuStateRecordUndo((undoState) =>
                        {
                            undoState.selectedNoiseLayerIndex = i + 1;
                        });
                    }, i < (spline.noises.Count - 1) && spline.noises.Count > 1);

                    //Move up
                    EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconUpArrow, 22, 19, () =>
                    {
                        EHandleUndo.RecordNow(spline, "Moved noise layer up");
                        NoiseLayer noiseA = spline.noises[i];
                        NoiseLayer noiseB = spline.noises[i - 1];
                        spline.noises[i] = noiseB;
                        spline.noises[i - 1] = noiseA;

                        EHandleUi.UpdateMenuStateRecordUndo((undoState) =>
                        {
                            undoState.selectedNoiseLayerIndex = i - 1;
                        });

                    }, i > 0 && spline.noises.Count > 1);

                    EUiUtility.CreateButton(ButtonType.DEFAULT_RED, LibraryGUIContent.iconRemove, 22, 19, () =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                selected.noises.RemoveAt(i);
                                EHandleUi.UpdateMenuStateRecordUndo((undoState) =>
                                {
                                    if(!isSelected && i < undoState.selectedNoiseLayerIndex)
                                        undoState.selectedNoiseLayerIndex--;
                                    else if(isSelected)
                                        undoState.selectedNoiseLayerIndex = -1;
                                });
                            }
                        }, "Removed noise effect");
                    });

                    EUiUtility.CreateSpaceWidth(11);

                    EUiUtility.CreateButton(ButtonType.DEFAULT_GREEN, isSelected ? LibraryGUIContent.iconMinimize : LibraryGUIContent.iconSelectLayer, 22, 19, () =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                EHandleUi.UpdateMenuStateRecordUndo((undoState) => 
                                {
                                    undoState.selectedNoiseLayerIndex = isSelected ? -1 : i;
                                });
                            }
                        }, "Selected noise layer.");
                    });

                    EUiUtility.CreateToggleField("", noise.enabled, (newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise12 = selected.noises[i];
                                noise12.enabled = newValue;
                                selected.noises[i] = noise12;
                            }
                        }, "Toggled layer");
                    }, true, true, 0, 16);

                    GUILayout.EndHorizontal();

                    if (!isSelected)
                        continue;

                    GUIStyle backgroundStyle = LibraryGUIStyle.backgroundSelectedLayer;
                    EUiUtility.CreateHorizontalBlackLine();

                    //Layer section GENERAL
                    EUiUtility.CreateLayerSection("GENERAL", 163, 50);

                    GUILayout.BeginHorizontal(backgroundStyle);
                    EUiUtility.CreateSpaceWidth(18);
                    EUiUtility.CreatePopupField("Noise:", 85, (int)noise.type, EHandleUi.optionsNoiseType, (int newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            NoiseLayer noise3 = selected.noises[i];
                            noise3.type = (NoiseType)newValue;
                            selected.noises[i] = noise3;
                        }, "Changed noise type");
                    }, 46, true, true, false);

                    EUiUtility.CreateSpaceWidth(2);
                    EUiUtility.CreatePopupField("Group:", 85, (int)noise.group - 1, EHandleUi.optionsNoiseGroups, (int newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            NoiseLayer noise0 = selected.noises[i];
                            noise0.group = (NoiseGroup)(newValue + 1);
                            selected.noises[i] = noise0;
                        }, "Changed noise group");
                    }, 51, true, true, false);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(backgroundStyle);
                    EUiUtility.CreateSpaceWidth(21);
                    EUiUtility.CreateDelayedFloatFieldWithLabel("Seed:", noise.seed, (newValue) => 
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > index)
                            {
                                NoiseLayer noise5 = selected.noises[index];
                                noise5.seed = newValue;
                                selected.noises[index] = noise5;
                            }
                        }, "Changed noise seed");
                    }, 40, 42, true);

                    EUiUtility.CreateSpaceWidth(46);
                    EUiUtility.CreateXYZInputDelayedFields("Scale:", noise.scale, (newValue, dif) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise4 = selected.noises[i];
                                noise4.scale = newValue;
                                selected.noises[i] = noise4;
                            }
                        }, "Changed noise scale");
                    }, 50, 10, 52, leftMouseUp, false, true);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(backgroundStyle);
                    bool amplitude = noise.type == NoiseType.DOMAIN_WARPED_NOISE;
                    bool fullSettings = noise.type == NoiseType.FMB_NOISE || noise.type == NoiseType.HYBRID_MULTI_FRACTAL || noise.type == NoiseType.RIDGED_PERLIN_NOISE;

                    GUI.enabled = fullSettings || amplitude;

                    EUiUtility.CreateDelayedFloatFieldWithLabel("Amplitude:", noise.amplitude, (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise6 = selected.noises[i];
                                noise6.amplitude = newValue;
                                selected.noises[i] = noise6;
                            }
                        }, "Changed noise amplitude");
                    }, 40, 68, true);

                    EUiUtility.CreateSpaceWidth(19);

                    GUI.enabled = fullSettings;

                    EUiUtility.CreateDelayedFloatFieldWithLabel("Frequency:", noise.frequency, (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise7 = selected.noises[i];
                                noise7.frequency = newValue;
                                selected.noises[i] = noise7;
                            }
                        }, "Changed noise frequency");
                    }, 40, 71, true);

                    EUiUtility.CreateSpaceWidth(20);

                    EUiUtility.CreateDelayedFloatFieldWithLabel("Octaves:", noise.octaves, (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise8 = selected.noises[i];
                                noise8.octaves = Mathf.RoundToInt(newValue);
                                selected.noises[i] = noise8;
                            }
                        }, "Changed noise octaves");
                    }, 40, 58, true);
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundLayerSection, GUILayout.Height(3));
                    GUILayout.Space(3);
                    GUILayout.EndHorizontal();
                    EUiUtility.CreateHorizontalBlackLine();
                }

                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundMainLayerButtons);

                //Remove all
                GUILayout.FlexibleSpace();
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textDeleteLayers, 84, 18, () =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.noises.Clear();
                    }, "Removed all noise layer");
                }, spline.noises.Count > 0);

                //Create layer
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textCreateLayer, 80, 18, () =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.noises.Add(new NoiseLayer(NoiseType.PERLIN_NOISE, Vector3.one, 0));
                        NoiseLayer nl = selected.noises[selected.noises.Count - 1];
                        selected.noises[selected.noises.Count - 1] = nl;

                        EHandleUi.UpdateMenuStateRecordUndo((undoState) =>
                        {
                            undoState.selectedNoiseLayerIndex = selected.noises.Count - 1;
                        });
                    }, "Created noise");
                }, spline.noises.Count < maxNoisesLayers);

                GUILayout.EndHorizontal();
            }
            #endregion

            #region Info
            // SPLINE MENU INFO
            else if (EHandleUi.ActiveSplineMenu == "info")
            {
                float splineData = EHandleSpline.GetSplineMemoryUsage(spline);
                float componentData = EHandleSpline.GetComponentMemoryUsage(spline);
                float deformationData = spline.deformationsMemoryUsage;
                float length = spline.Length;
                float vertices = spline.vertices;
                float deformations = spline.deformations;
                float followers = spline.followers;
                float deformationsInBuild = spline.deformationsInBuild;
                float followersInBuild = spline.followersInBuild;

                foreach (Spline spline2 in EHandleSelection.selectedSplines)
                {
                    splineData += EHandleSpline.GetSplineMemoryUsage(spline2);
                    componentData = EHandleSpline.GetComponentMemoryUsage(spline2);
                    deformationData += spline2.deformationsMemoryUsage;
                    length += spline2.Length;
                    vertices += spline2.vertices;
                    deformations += spline2.deformations;
                    followers += spline2.followers;
                    deformationsInBuild = spline.deformationsInBuild;
                    followersInBuild = spline.followersInBuild;
                }

                EUiUtility.CreateSection("INFO");

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineData, true);
                EUiUtility.CreateLabelField("Spline data: " + GeneralUtility.GetMemorySizeFormat(splineData), LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgComponentData, true);
                EUiUtility.CreateLabelField("Component data: " + GeneralUtility.GetMemorySizeFormat(componentData), LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgMeshData, true);
                EUiUtility.CreateLabelField($"Mesh data: {GeneralUtility.GetMemorySizeFormat(deformationData)}", LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                EUiUtility.CreateLabelField("Length: " + length.ToString(), LibraryGUIStyle.textDefault);
                EUiUtility.CreateLabelField("Vertices: " + vertices.ToString(), LibraryGUIStyle.textDefault);
                EUiUtility.CreateLabelField($"Deformations: {deformations} ({deformationsInBuild} in build)", LibraryGUIStyle.textDefault);
                EUiUtility.CreateLabelField($"Followers: {followers} ({followersInBuild} in build)", LibraryGUIStyle.textDefault);
            }
            #endregion

            #region addons
            else
            {
                bool foundAddon = false;

                for (int i = 0; i < addonsDrawWindow.Count; i++)
                {
                    if (addonsDrawWindow[i].Item1 == EHandleUi.ActiveSplineMenu)
                    {
                        foundAddon = true;
                        addonsDrawWindow[i].Item2.Invoke(spline, leftMouseUp);
                    }
                }

                if (foundAddon == false)
                {
                    EHandleUi.UpdateMenuState((undoState) => 
                    {
                        undoState.activeSplineMenu = "general";
                    });
                }
            }
            #endregion

            if(!EGlobalSettings.GetSubmenusOnTop()) DrawBottom(spline);

            EHandleEvents.InvokeAfterWindowSplineGUI(Event.current, leftMouseUp);
        }

        private void DrawBottom(Spline spline)
        {
            EUiUtility.CreateHorizontalBlackLine();
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundBottomMenu);

            //spline general
            EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, LibraryGUIContent.iconGeneral, LibraryGUIContent.iconGeneralActive, 35, 18, () =>
            {
                EHandleUi.UpdateMenuStateRecordUndo((undoState) =>
                {
                    undoState.activeSplineMenu = "general";
                }, "Changed sub menu");

                GUI.FocusControl(null);
            }, EHandleUi.ActiveSplineMenu == "general");

            //Addons
            for (int i = 0; i < addonsButtons.Count; i++)
            {
                EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, addonsButtons[i].Item2, addonsButtons[i].Item3, 35, 18, () =>
                {
                    EHandleUi.UpdateMenuStateRecordUndo((undoState) =>
                    {
                        undoState.activeSplineMenu = addonsButtons[i].Item1;
                    }, "Changed sub menu");

                    GUI.FocusControl(null);
                }, EHandleUi.ActiveSplineMenu == addonsButtons[i].Item1);
            }

            //Noise
            EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, LibraryGUIContent.iconNoise, LibraryGUIContent.iconNoiseActive, 35, 18, () =>
            {
                EHandleUi.UpdateMenuStateRecordUndo((undoState) =>
                {
                    undoState.activeSplineMenu = "noise";
                }, "Changed sub menu");

                GUI.FocusControl(null);
            }, EHandleUi.ActiveSplineMenu == "noise");

            //Info
            EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, LibraryGUIContent.iconInfo, LibraryGUIContent.iconInfoActive, 35, 18, () =>
            {
                EHandleUi.UpdateMenuStateRecordUndo((undoState) =>
                {
                    undoState.activeSplineMenu = "info";
                }, "Changed sub menu");

                GUI.FocusControl(null);
            }, EHandleUi.ActiveSplineMenu == "info");

            GUILayout.EndHorizontal();
        }

        protected override void UpdateWindowSize()
        {
            Spline spline = EHandleSelection.selectedSpline;
            SplineObject so = EHandleSelection.selectedSplineObject;
 
            if (so != null && so.SoParent == null && spline != null && so.transform.parent != spline.transform)
                spline = null;

            //Size when no spline is selected
            cachedRect.width = 136;
            cachedRect.height = itemHeight + 2;

            if (spline == null)
                return;

            if(EGlobalSettings.GetControlPanelWindowMinimized())
            {
                cachedRect.width = 27;
                cachedRect.height = 27;
            }
            else
            {
                if (EHandleUi.ActiveSplineMenu == "general")
                {
                    cachedRect.width = 260;
                    cachedRect.height = headerHeight + toolbarHeight + bottomHeight;
                    cachedRect.height += itemHeight * 5;
                    cachedRect.height += sectionHeight * 2;

                    if (!EHandleUi.RenderingMinimized)
                        cachedRect.height += itemHeight * 3;

                    if (spline.componentMode == ComponentMode.ACTIVE)
                    {
                        cachedRect.height += sectionHeight;

                        if (!EHandleUi.SchedulingMinimized)
                            cachedRect.height += itemHeight * 5;
                    }
                }
                else if (EHandleUi.ActiveSplineMenu == "noise")
                {
                    cachedRect.height = headerHeight + toolbarHeight + bottomHeight;
                    cachedRect.height += itemHeight;
                    cachedRect.height += sectionHeight;
                    for (int i = 0; i < spline.noises.Count; i++)
                    {
                        NoiseLayer nl = spline.noises[i];
                        bool isSelected = EHandleUi.SelectedNoiseLayerIndex == i; 

                        if (!isSelected) cachedRect.height += itemHeight * 1;
                        else
                        {
                            cachedRect.height += 20;
                            cachedRect.height += itemHeight * 4;
                        }
                    }

                    if (spline.noises.Count > 0)
                        cachedRect.width = 388;
                    else
                        cachedRect.width = 213;
                }
                else if (EHandleUi.ActiveSplineMenu == "info")
                {
                    cachedRect.height = headerHeight + toolbarHeight + bottomHeight;
                    cachedRect.height += itemHeight * 7;
                    cachedRect.height += sectionHeight;

                    cachedRect.width = 213;
                }
                else
                {
                    for (int i = 0; i < addonsCalcWindowSize.Count; i++)
                    {
                        if (EHandleUi.ActiveSplineMenu == addonsCalcWindowSize[i].Item1)
                        {
                            Rect rect = addonsCalcWindowSize[i].Item2.Invoke(spline);
                            cachedRect.height = rect.height;
                            cachedRect.width = rect.width;
                            break;
                        }
                    }
                }

                //Expand window for title.
                guiContentContainer.text = windowTitle;
                Vector2 labelSize = LibraryGUIStyle.textHeader.CalcSize(guiContentContainer);
                labelSize.x += LibraryGUIStyle.textHeader.padding.left + LibraryGUIStyle.textHeader.padding.right + 70;
                if (labelSize.x > cachedRect.width) cachedRect.width = labelSize.x;
            }
        }

        protected override void HandleExtendedWindow()
        {
            if (!EHandleUi.initialized)
                return;

            Spline spline = EHandleSelection.selectedSpline;
            SplineObject so = EHandleSelection.selectedSplineObject;

            if (spline == null && so == null)
            {
                if (extendedWindow != null)
                    extendedWindow.CloseWindow();

                return;
            }

            if(!EHandleSceneView.mouseDragEnabled)
            {
                //Create extended window
                if (((spline != null && EHandleSelection.SelectedControlPoint != 0) || so != null) && extendedWindow == null)
                {
                    extendedWindow = CreateInstance<WindowExtended>();
                    extendedWindow.OpenWindow(false);
                    extendedWindow.toolbarToggleBase = toolbarToggleBase;
                }

                //Close extended window
                if ((spline == null || EHandleSelection.SelectedControlPoint == 0) && so == null && extendedWindow != null)
                    extendedWindow.CloseWindow();
            }

            //Updated position on extended window
            Vector2 newExtendedMenuPosition = cachedRect.position + new Vector2(cachedRect.size.x + extendedMenuMargin, 0);
            if (!EGlobalSettings.GetWindowHorizontalOrder()) newExtendedMenuPosition = cachedRect.position + new Vector2(0, cachedRect.size.y + extendedMenuMargin);
            if (extendedWindow != null && !GeneralUtility.IsEqual(newExtendedMenuPosition, extendedWindow.cachedRect.position))
            {
                extendedWindow.UpdateChacedPosition(newExtendedMenuPosition);
            }
        }

        public static void AddSubMenu(string id, GUIContent button, GUIContent buttonActive, Action<Spline, bool> DrawWindow, Func<Spline, Rect> calcWindowSize)
        {
            for (int i = 0; i < addonsDrawWindow.Count; i++)
            {
                if (addonsDrawWindow[i].Item1 == id)
                {
                    addonsDrawWindow.RemoveAt(i);
                    addonsButtons.RemoveAt(i);
                    addonsCalcWindowSize.RemoveAt(i);
                }
            }

            addonsDrawWindow.Add((id, DrawWindow));
            addonsButtons.Add((id, button, buttonActive));
            addonsCalcWindowSize.Add((id, calcWindowSize));
        }

        private static AnimationCurve GetScaleProfileDisplayCurve(Spline spline)
        {
            if (scaleProfileDisplayCurves.ContainsKey(spline))
                return scaleProfileDisplayCurves[spline];

            AnimationCurve curve = new AnimationCurve();
            scaleProfileDisplayCurves.Add(spline, curve);

            return curve;
        }
    }
}
