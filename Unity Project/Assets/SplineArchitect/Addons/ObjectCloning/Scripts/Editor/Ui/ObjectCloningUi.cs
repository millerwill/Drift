// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ObjectCloningUi.cs
//
// Author: Mikael Danielsson
// Date Created: 20-04-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

using SplineArchitect.Libraries;
using SplineArchitect.Utility;
using SplineArchitect.CustomTools;

namespace SplineArchitect.Ui
{
    public class ObjectCloningUi
    {
        private static string[] cloneDirectionOptions = new string[] { "Forward", "Backward" };

        public static Rect CalcSplineObjectWindowSize(SplineObject so)
        {
            Rect rect = new Rect();
            rect.height = WindowBase.headerHeight + WindowBase.toolbarHeight + WindowBase.bottomHeight;
            rect.height += WindowBase.sectionHeight;
            rect.height += WindowBase.itemHeight * 5;

            rect.width = 255;

            return rect;
        }

        public static void DrawSplineObjectWindow(SplineObject so)
        {
            Spline selectedSpline = EHandleSelection.selectedSpline;

            bool leftMouseUp = false;
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0) leftMouseUp = true;

            EUiUtility.CreateSection("OBJECT CLONING");

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateXYZInputFields("Offset", so.cloneOffset, (offset, dif) =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneOffset = offset;
                    selected.MarkCloneVersionDirty();
                }, "Updated clone offset");
            }, 55, 10, 62);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateCheckbox(so.cloneUseFixedAmount, (value) =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneUseFixedAmount = value;
                    selected.MarkCloneVersionDirty();
                }, "Change fixed amount");
            }, 15);

            if (!so.cloneUseFixedAmount)
            {
                EUiUtility.CreateSliderAndDelayedInputField("Amount:", so.cloneAmount, (newValue, changeBySlider) => { }, 0, 100, leftMouseUp, 70, 58, true, false);
            }
            else
            {
                EUiUtility.CreateSliderAndDelayedInputField("Amount:", so.cloneAmount, (newValue, changeBySlider) =>
                {
                    EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                    {
                        if (newValue < 0) newValue = 0;
                        selected.cloneAmount = (int)newValue;
                        if (!changeBySlider) selected.MarkCloneVersionDirty();
                    }, "Changed clone amount", false, EHandleUndo.RecordType.REGISTER_COMPLETE_OBJECT);
                }, 0, 100, leftMouseUp, 70, 58, true, true, true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateToggleField("Snap end:", so.cloneSnapEnd, (value) =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneSnapEnd = value;
                    selected.MarkCloneVersionDirty();
                }, "Change snap end");
            }, !so.cloneUseFixedAmount, true);

            EUiUtility.CreateFloatFieldWithLabel("Snap offset:", so.cloneSnapEndOffset, (value) =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneSnapEndOffset = value;
                    selected.MarkCloneVersionDirty();
                }, "Set clone snap offset");
            }, 70, 78, true, so.cloneSnapEnd);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            GUILayout.FlexibleSpace();
            EUiUtility.CreatePopupField("Direction:", 69, (int)so.cloneDirection, cloneDirectionOptions, (int newValue) =>
            {
                CloneDirection direction = (CloneDirection)newValue;

                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneDirection = direction;
                    selected.MarkCloneVersionDirty();
                }, "Set clone direction", false, EHandleUndo.RecordType.REGISTER_COMPLETE_OBJECT);
            }, -1, true);
            GUILayout.EndHorizontal();

            SplineObject cloneParent = EHandleObjectCloning.GetCloneParent(so);

            if (cloneParent != null)
            {
                bool isOriginClone = EHandleObjectCloning.IsOriginClone(so, cloneParent);

                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundMainLayerButtons);
                GUILayout.FlexibleSpace();
                if (isOriginClone)
                    EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.warningMsgCantDisconnectOriginClone);
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textDisconnect, 76, 18, () =>
                {
                    EHandleUndo.RecordNow(cloneParent);
                    EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                    {
                        for(int i = 0; i < cloneParent.clones.Count; i++) 
                        {
                            if (selected == cloneParent.clones[i])
                                cloneParent.clones[i] = null;
                        }
                    });

                    PositionTool.locked = false;
                    EHandleSceneView.RepaintCurrent();
                }, selectedSpline != null && !isOriginClone);
                GUILayout.EndHorizontal();
            }
            else
            {
                if ((so.transform.childCount == 0 && !EHandlePrefab.IsPartOfAnyPrefab(so.gameObject)) || (so.transform.childCount == 0 && EHandlePrefab.IsPrefabRoot(so.gameObject)))
                {
                    GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundMainLayerButtons);
                    GUILayout.FlexibleSpace();
                    EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textClone, 48, 18, () =>
                    {
                        EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                        {
                            EHandleObjectCloning.EnableUsingSelf(selected.SplineParent, selected);
                            selected.MarkCloneVersionDirty();
                        });
                    }, selectedSpline != null);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    if (so.cloningEnabled || (so.clones != null && so.clones.Count > 0))
                    {
                        GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundMainLayerButtons);
                        GUILayout.FlexibleSpace();
                        EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textDisconnect, 76, 18, () =>
                        {
                            EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                            {
                                EHandleObjectCloning.DisconnectClonesAndDisable(selected);
                            });
                        }, true);

                        EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textDeleteClones, 92, 18, () =>
                        {
                            EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                            {
                                EHandleObjectCloning.Disable(selected);
                                selected.MarkCloneVersionDirty();
                            });
                        }, true);
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundMainLayerButtons);
                        GUILayout.FlexibleSpace();
                        if (so.transform.childCount == 0)
                            EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.warningMsgCantCloneWithNoChildren);
                        else if (so.Type != SplineObjectType.DEFORMATION)
                            EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.warningMsgCanOnlyCloneWithTypeDeformation);

                        EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textCloneChildren, 92, 18, () =>
                        {
                            EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                            {
                                EHandleObjectCloning.EnableUsingChildren(selected.SplineParent, selected);
                                selected.MarkCloneVersionDirty();
                            });
                        }, selectedSpline != null && so.transform.childCount > 0 && so.Type == SplineObjectType.DEFORMATION);
                        GUILayout.EndHorizontal();
                    }
                }
            }

            //Update amount slider on mouse up
            if(leftMouseUp)
            {
                if (so.Type != SplineObjectType.DEFORMATION)
                    return;

                if (!so.cloningEnabled)
                    return;

                EHandleObjectCloning.UpdateCloneAmount(so);
            }
        }
    }
}
