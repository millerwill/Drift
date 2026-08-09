// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EUiUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 16-02-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Reflection;

using UnityEditor;
using UnityEngine;

using SplineArchitect.CustomTools;
using SplineArchitect.Libraries;
using SplineArchitect.Ui;

namespace SplineArchitect.Utility
{
    public class EUiUtility
    {
        private static int backgroundStyleCounter = 0;
        private static float pendingFloat;
        private static float pendingFloat2;
        private static Vector3 pendingVector3;
        private static bool isPending;
        private static int minMaxSliderControlerId = -1;
        private static string horizontalSliderId = "";

        public static void CreateSection(string title)
        {
            GUILayout.BeginVertical();
            CreateHorizontalBlackLine();
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSection);
            CreateLabelField($"<b>{title}</b>", LibraryGUIStyle.textSection, true);
            GUILayout.EndHorizontal();
            CreateHorizontalBlackLine();
            GUILayout.EndVertical();
        }

        public static void CreateSubSection(string title, bool minimized, 
                                                          Action actionOnPress)
        {
            CreateHorizontalBlackLine();
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubSection);
            CreateLabelField($"<b>{title}</b>", LibraryGUIStyle.textSubSection, true);
            GUILayout.Space(100);
            CreateButton(ButtonType.SUB_MENU2, minimized ?
                                               LibraryGUIContent.iconMaximizeBlack :
                                               LibraryGUIContent.iconMinimizeBlack, 20, 14, () =>
            {
                actionOnPress.Invoke();
            });
            GUILayout.EndHorizontal();
            CreateHorizontalBlackLine();
        }

        public static void CreateSubSection(string title)
        {
            CreateHorizontalBlackLine();
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubSection);
            CreateLabelField($"<b>{title}</b>", LibraryGUIStyle.textSubSection, true);
            GUILayout.EndHorizontal();
            CreateHorizontalBlackLine();
        }

        public static void CreateLayerSection(string title, bool minimized, 
                                                            float spaceLeft, 
                                                            float labelWidth, 
                                                            float spaceRight, 
                                                            Action actionOnPress)
        {
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundLayerSection);
            GUILayout.Space(spaceLeft);
            CreateLabelField($"<b>{title}</b>", LibraryGUIStyle.textLayerSection, true, labelWidth);
            GUILayout.Space(spaceRight);
            CreateButton(ButtonType.DEFAULT_WHITE, minimized ? LibraryGUIContent.iconMaximizeBlack :
                                                                LibraryGUIContent.iconMinimizeBlack, 18, 12, () =>
                                                                {
                                                                    actionOnPress.Invoke();
                                                                });
            GUILayout.EndHorizontal();
            CreateHorizontalSubHeader2Line();
        }

        public static void CreateLayerSection(string title, float spaceLeft, float labelWidth)
        {
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundLayerSection);
            GUILayout.Space(spaceLeft);
            CreateLabelField($"<b>{title}</b>", LibraryGUIStyle.textLayerSection, true, labelWidth);
            GUILayout.EndHorizontal();
            CreateHorizontalSubHeader2Line();
        }

        public static void CreateColorField(string label, 
                                            Color color, 
                                            Action<Color> onChange, 
                                            float width = -1, 
                                            float labelWidth = -1, 
                                            bool skipGroup = false)
        {
            Color oldColor = color;

            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());

            if (labelWidth == -1) GUILayout.Label(label, LibraryGUIStyle.textDefault);
            else GUILayout.Label(label, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));

            if (width == -1) color = EditorGUILayout.ColorField(color);
            else color = EditorGUILayout.ColorField(color, GUILayout.Width(width));

            if (!skipGroup) GUILayout.EndHorizontal();

            if (!GeneralUtility.IsEqual(oldColor, color))
            {
                onChange.Invoke(color);
            }
        }

        public static void CreateCheckbox(bool value, Action<bool> onChange, float width = 0)
        {
            bool oldValue = value;
            if(width == 0)
                value = EditorGUILayout.Toggle(value);
            else
                value = EditorGUILayout.Toggle(value, GUILayout.Width(width));

            if (oldValue != value)
                onChange.Invoke(value);
        }

        public static void CreateButtonToggle(ButtonType buttonType, 
                                              GUIContent icon, 
                                              GUIContent iconActive, 
                                              float width, 
                                              float height, 
                                              Action onPress, 
                                              bool active, 
                                              bool enable = true)
        {
            GUIStyle buttonStyle = null;
            GUIStyle buttonActiveStyle = null;

            if(buttonType == ButtonType.SUB_MENU)
            {
                buttonStyle = LibraryGUIStyle.buttonSubMenu;
                buttonActiveStyle = LibraryGUIStyle.buttonSubMenuActive;
            }
            else if (buttonType == ButtonType.DEFAULT_GREEN)
            {
                buttonStyle = LibraryGUIStyle.buttonDefaultGreen;
                buttonActiveStyle = LibraryGUIStyle.buttonDefaultActive;
            }
            else if (buttonType == ButtonType.DEFAULT_RED)
            {
                buttonStyle = LibraryGUIStyle.buttonDefaultRed;
                buttonActiveStyle = LibraryGUIStyle.buttonDefaultActive;
            }
            else if(buttonType == ButtonType.DEFAULT_MIDDLE_LEFT)
            {
                buttonStyle = LibraryGUIStyle.buttonDefaultMiddleLeft;
                buttonActiveStyle = LibraryGUIStyle.buttonDefaultMiddleLeftActive;
            }
            else
            {
                buttonStyle = LibraryGUIStyle.buttonDefault;
                buttonActiveStyle = LibraryGUIStyle.buttonDefaultActive;
            }

            GUI.enabled = enable;
            if (GUILayout.Button(active ? iconActive : icon, active ? buttonActiveStyle : buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
                onPress.Invoke();
            GUI.enabled = true;
        }

        public static void CreateButton(ButtonType buttonType, 
                                        GUIContent icon, 
                                        float width, 
                                        float height, 
                                        Action onPress, 
                                        bool enable = true)
        {
            GUIStyle buttonStyle = null;

            if (buttonType == ButtonType.SUB_MENU)
                buttonStyle = LibraryGUIStyle.buttonSubMenu;
            else if (buttonType == ButtonType.DEFAULT_RED)
                buttonStyle = LibraryGUIStyle.buttonDefaultRed;
            else if (buttonType == ButtonType.DEFAULT_WHITE)
                buttonStyle = LibraryGUIStyle.buttonDefaultWhite;
            else if (buttonType == ButtonType.SUB_MENU2)
                buttonStyle = LibraryGUIStyle.buttonSubMenu2;
            else if (buttonType == ButtonType.DEFAULT_GREEN)
                buttonStyle = LibraryGUIStyle.buttonDefaultGreen;
            else if (buttonType == ButtonType.DEFAULT_MIDDLE_LEFT)
                buttonStyle = LibraryGUIStyle.buttonDefaultMiddleLeft;
            else if (buttonType == ButtonType.DEFAULT_ACTIVE)
                buttonStyle = LibraryGUIStyle.buttonDefaultActive;
            else
                buttonStyle = LibraryGUIStyle.buttonDefault;

            GUI.enabled = enable;
            if (GUILayout.Button(icon, buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
            {
                onPress.Invoke();
            }
            GUI.enabled = true;
        }

        public static void CreateObjectField(string label, 
                                             UnityEngine.Object obj, 
                                             Type typeOf, 
                                             Action<UnityEngine.Object> actionOnChange, 
                                             float width = 0, 
                                             float labelWidth = -1, 
                                             bool skipGroup = false, 
                                             bool blackText = false)
        {
            if(!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            if (labelWidth == -1)
                GUILayout.Label(label, blackText ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault);
            else
                GUILayout.Label(label, blackText ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            GUI.SetNextControlName(label);
            UnityEngine.Object newO;

            if (width == 0)
                newO = EditorGUILayout.ObjectField(obj, typeOf, true);
            else
                newO = EditorGUILayout.ObjectField(obj, typeOf, true, GUILayout.Width(width));

            if (!skipGroup) GUILayout.EndHorizontal();

            if (obj != newO) actionOnChange.Invoke(newO);

            if (GUI.GetNameOfFocusedControl() == label && Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Delete)
                Event.current.Use();
        }

        public static void CreateXYZInputFields(string label, 
                                                Vector3 currentValue, 
                                                Action<Vector3, Vector3> actionOnValueChange, 
                                                float labelWidth, 
                                                float xyzLabelWidth, 
                                                float inputFieldWidth, 
                                                bool disable = false, 
                                                bool skipGroup = false)
        {
            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUILayout.Label($"<b>{label}</b>", LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            Vector3 oldValue = currentValue;

            currentValue.x = Mathf.Round(currentValue.x * 100) / 100;
            currentValue.y = Mathf.Round(currentValue.y * 100) / 100;
            currentValue.z = Mathf.Round(currentValue.z * 100) / 100;

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            Color oldNormal = EditorStyles.label.normal.textColor;
            Color oldHover = EditorStyles.label.hover.textColor;
            Color oldFocused = EditorStyles.label.focused.textColor;
            EditorGUIUtility.labelWidth = xyzLabelWidth;
            EditorStyles.label.normal.textColor = Color.white;
            EditorStyles.label.hover.textColor = Color.white;
            EditorStyles.label.focused.textColor = Color.white;
            GUI.enabled = !disable;
            GUI.SetNextControlName("field_x");
            currentValue.x = EditorGUILayout.FloatField("X", currentValue.x, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(inputFieldWidth));
            GUILayout.Space(2);
            GUI.SetNextControlName("field_y");
            currentValue.y = EditorGUILayout.FloatField("Y", currentValue.y, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(inputFieldWidth));
            GUILayout.Space(2);
            GUI.SetNextControlName("field_z");
            currentValue.z = EditorGUILayout.FloatField("Z", currentValue.z, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(inputFieldWidth));
            GUI.enabled = true;

            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorStyles.label.normal.textColor = oldNormal;
            EditorStyles.label.hover.textColor = oldHover;
            EditorStyles.label.focused.textColor = oldFocused;

            if (!skipGroup) GUILayout.EndHorizontal();

            currentValue.x = Mathf.Round(currentValue.x * 100) / 100;
            currentValue.y = Mathf.Round(currentValue.y * 100) / 100;
            currentValue.z = Mathf.Round(currentValue.z * 100) / 100;

            oldValue.x = Mathf.Round(oldValue.x * 100) / 100;
            oldValue.y = Mathf.Round(oldValue.y * 100) / 100;
            oldValue.z = Mathf.Round(oldValue.z * 100) / 100;

            if (PositionTool.activePart == PositionTool.ActivePart.NONE && (GUI.GetNameOfFocusedControl() == "field_x" ||
                                                                            GUI.GetNameOfFocusedControl() == "field_y" ||
                                                                            GUI.GetNameOfFocusedControl() == "field_z"))
            {
                if (!GeneralUtility.IsEqual(oldValue, currentValue))
                {
                    actionOnValueChange.Invoke(currentValue, oldValue - currentValue);
                }
            }
        }

        public static void CreateXYZInputDelayedFields(string label,
                                        Vector3 currentValue,
                                        Action<Vector3, Vector3> actionOnValueChange,
                                        float labelWidth,
                                        float xyzLabelWidth,
                                        float inputFieldWidth,
                                        bool leftMouseUp,
                                        bool disable = false,
                                        bool skipGroup = false)
        {
            Vector3 newValue;
            Vector3 fieldValue = currentValue;
            bool pendigDif = false;
            Rect lastRect = new Rect();
            GUIStyle guiStyleX = LibraryGUIStyle.textFieldDelayedNoWidth;
            GUIStyle guiStyleY = LibraryGUIStyle.textFieldDelayedNoWidth;
            GUIStyle guiStyleZ = LibraryGUIStyle.textFieldDelayedNoWidth;

            if (isPending && GUI.GetNameOfFocusedControl().Contains(label) && !GeneralUtility.IsEqual(pendingVector3, currentValue))
            {
                if (GUI.GetNameOfFocusedControl() == $"{label} field_x")
                    guiStyleX = LibraryGUIStyle.textFieldPendingNoWidth;

                if (GUI.GetNameOfFocusedControl() == $"{label} field_y")
                    guiStyleY = LibraryGUIStyle.textFieldPendingNoWidth;

                if (GUI.GetNameOfFocusedControl() == $"{label} field_z")
                    guiStyleZ = LibraryGUIStyle.textFieldPendingNoWidth;

                fieldValue = pendingVector3;
                pendigDif = true;
            }

            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUILayout.Label($"<b>{label}</b>", LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            Vector3 oldValue = currentValue;

            currentValue.x = Mathf.Round(currentValue.x * 100) / 100;
            currentValue.y = Mathf.Round(currentValue.y * 100) / 100;
            currentValue.z = Mathf.Round(currentValue.z * 100) / 100;

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            Color oldNormal = EditorStyles.label.normal.textColor;
            Color oldHover = EditorStyles.label.hover.textColor;
            Color oldFocused = EditorStyles.label.focused.textColor;
            EditorGUIUtility.labelWidth = xyzLabelWidth;
            EditorStyles.label.normal.textColor = Color.white;
            EditorStyles.label.hover.textColor = Color.white;
            EditorStyles.label.focused.textColor = Color.white;
            GUI.enabled = !disable;
            GUI.SetNextControlName($"{label} field_x");
            newValue.x = EditorGUILayout.FloatField("X", fieldValue.x, guiStyleX, GUILayout.Width(inputFieldWidth));
            if(GUI.GetNameOfFocusedControl() == $"{label} field_x") lastRect = GUILayoutUtility.GetLastRect();
            GUILayout.Space(2);
            GUI.SetNextControlName($"{label} field_y");
            newValue.y = EditorGUILayout.FloatField("Y", fieldValue.y, guiStyleY, GUILayout.Width(inputFieldWidth));
            if (GUI.GetNameOfFocusedControl() == $"{label} field_y") lastRect = GUILayoutUtility.GetLastRect();
            GUILayout.Space(2);
            GUI.SetNextControlName($"{label} field_z");
            newValue.z = EditorGUILayout.FloatField("Z", fieldValue.z, guiStyleZ, GUILayout.Width(inputFieldWidth));
            if (GUI.GetNameOfFocusedControl() == $"{label} field_z") lastRect = GUILayoutUtility.GetLastRect();

            if (pendigDif && EditorGUIUtility.editingTextField)
            {
                lastRect = new Rect(new Vector2(lastRect.position.x + Mathf.RoundToInt(lastRect.size.x / 2) - 8, lastRect.position.y - lastRect.size.y + 10), new Vector2(45, 30));
                GUI.Label(lastRect, "Enter", LibraryGUIStyle.pressEnter);
            }

            GUI.enabled = true;

            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorStyles.label.normal.textColor = oldNormal;
            EditorStyles.label.hover.textColor = oldHover;
            EditorStyles.label.focused.textColor = oldFocused;

            if (!skipGroup) GUILayout.EndHorizontal();

            newValue.x = Mathf.Round(newValue.x * 100) / 100;
            newValue.y = Mathf.Round(newValue.y * 100) / 100;
            newValue.z = Mathf.Round(newValue.z * 100) / 100;

            oldValue.x = Mathf.Round(oldValue.x * 100) / 100;
            oldValue.y = Mathf.Round(oldValue.y * 100) / 100;
            oldValue.z = Mathf.Round(oldValue.z * 100) / 100;

            if (PositionTool.activePart == PositionTool.ActivePart.NONE && GUI.GetNameOfFocusedControl().Contains(label))
            {
                Event e = Event.current;

                if (!GeneralUtility.IsEqual(oldValue, newValue) && 
                    (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || (!EditorGUIUtility.editingTextField && leftMouseUp)))
                {
                    actionOnValueChange.Invoke(newValue, oldValue - newValue);
                    GUI.FocusControl(null);
                    WindowBase.RepaintAll();
                }
                else
                {
                    isPending = true;
                    pendingVector3 = newValue;
                }
            }
        }

        public static void CreateFromToInputField(string label, 
                                                  float currentValueFrom, 
                                                  float currentValueTo, 
                                                  Action<float, float> actionOnValueChange, 
                                                  float width = 58, 
                                                  float paddingLeft = 6, 
                                                  float labelWidth = 58, 
                                                  bool skipGroup = false, 
                                                  bool verySmall = false)
        {
            if(!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUILayout.Space(paddingLeft);
            GUILayout.Label(label, LibraryGUIStyle.textNoWdith, GUILayout.Width(labelWidth));
            float newValueFrom = EditorGUILayout.FloatField(currentValueFrom, verySmall ? LibraryGUIStyle.textFieldVerySmall : LibraryGUIStyle.textFieldSmall, GUILayout.Width(width));
            GUILayout.Label("-", LibraryGUIStyle.textNoWdith, GUILayout.Width(6));
            float newValueTo = EditorGUILayout.FloatField(currentValueTo, verySmall ? LibraryGUIStyle.textFieldVerySmall : LibraryGUIStyle.textFieldSmall, GUILayout.Width(width));
            if (!skipGroup) GUILayout.EndHorizontal();

            if (GeneralUtility.IsEqual(newValueFrom, currentValueFrom) && GeneralUtility.IsEqual(newValueTo, currentValueTo))
                return;
            
            actionOnValueChange.Invoke(newValueFrom, newValueTo);
        }

        public static void CreateSliderAndInputField(string label, 
                                                     float currentValue, 
                                                     Action<float, bool> actionOnValueChange, 
                                                     float sliderLeftValue, 
                                                     float sliderRightValue, 
                                                     float sliderWidth, 
                                                     float textFieldWidth, 
                                                     float labelWidth = 0, 
                                                     bool skipGroup = false, 
                                                     bool enable = true)
        {
            bool changeBySlider = false;

            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = enable;
            if (labelWidth == 0)
                GUILayout.Label(label, LibraryGUIStyle.textDefault);
            else
                GUILayout.Label(label, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            float oldValue = currentValue;
            float newValue = GUILayout.HorizontalSlider(currentValue, sliderLeftValue, sliderRightValue, GUILayout.Width(sliderWidth));

            //If slider changed value, stop focusing field
            if (!GeneralUtility.IsEqual(oldValue, newValue))
            {
                changeBySlider = true;
                GUI.FocusControl(null);
            }

            else newValue = EditorGUILayout.FloatField(newValue, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(textFieldWidth));

            GUI.enabled = true;
            if (!skipGroup) GUILayout.EndHorizontal();

            newValue = Mathf.Round(newValue * 100) / 100;
            if (GeneralUtility.IsEqual(newValue, oldValue))
                return;

            actionOnValueChange.Invoke(newValue, changeBySlider);
        }

        public static void CreateSliderAndDelayedInputField(string label,
                                                             float currentValue,
                                                             Action<float, bool> actionOnValueChange,
                                                             float sliderLeftValue,
                                                             float sliderRightValue,
                                                             bool leftMouseUp,
                                                             float textFieldWidth,
                                                             float labelWidth = 0,
                                                             bool skipGroup = false,
                                                             bool enable = true,
                                                             bool roundToInt = false)
        {
            float fieldValue = currentValue;
            bool pendingDif = false;
            GUIStyle guiStyle = LibraryGUIStyle.textFieldDelayedNoWidth;
            bool changeBySlider = false;

            // Update pendding values
            if (isPending && !GeneralUtility.IsEqual(pendingFloat, currentValue) 
                && (GUI.GetNameOfFocusedControl() == label || horizontalSliderId == label))
            {
                guiStyle = LibraryGUIStyle.textFieldPendingNoWidth;
                fieldValue = pendingFloat;
                pendingDif = true;
            }

            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = enable;

            // Label
            if (labelWidth == 0) GUILayout.Label(label, LibraryGUIStyle.textDefault);
            else GUILayout.Label(label, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));

            // Slider
            float newValue = GUILayout.HorizontalSlider(fieldValue, sliderLeftValue, sliderRightValue);

            //If slider changed value, stop focusing field
            if (!GeneralUtility.IsEqual(fieldValue, newValue))
            {
                changeBySlider = true;
                horizontalSliderId = label;
                GUI.FocusControl(null);
            }

            GUI.SetNextControlName(label);
            newValue = EditorGUILayout.FloatField(newValue, guiStyle, GUILayout.Width(textFieldWidth));

            // Enter info box
            if (pendingDif && EditorGUIUtility.editingTextField)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect = new Rect(new Vector2(rect.position.x + rect.size.x - (textFieldWidth / 2) - 14, rect.position.y - rect.size.y + 10), new Vector2(45, 30));
                GUI.Label(rect, "Enter", LibraryGUIStyle.pressEnter);
            }

            GUI.enabled = true;
            if (!skipGroup) GUILayout.EndHorizontal();

            Event e = Event.current;
            newValue = Mathf.Round(newValue * 100) / 100;
            if(roundToInt) newValue = Mathf.Round(newValue);

            if (!GeneralUtility.IsEqual(newValue, currentValue) && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || (leftMouseUp && !EditorGUIUtility.editingTextField)))
            {
                isPending = false;
                actionOnValueChange.Invoke(newValue, changeBySlider);
                GUI.FocusControl(null);
                WindowBase.RepaintAll();
                horizontalSliderId = "";
            }
            else if (GUI.GetNameOfFocusedControl() == label || horizontalSliderId == label)
            {
                isPending = true;
                pendingFloat = newValue;
            }
        }

        public static void CreateLabelField(string label, GUIStyle guiStyle, 
                                                          bool skipGroup = false, 
                                                          float labelWidth = -1)
        {
            if(!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            if(labelWidth == -1) GUILayout.Label(label, guiStyle);
            else GUILayout.Label(label, guiStyle, GUILayout.Width(labelWidth));
            if (!skipGroup) GUILayout.EndHorizontal();
        }

        public static void CreateMaskField(string label,
                                                   LayerMask currentValue,
                                                   string[] options,
                                                   Action<LayerMask> actionOnValueChange,
                                                   bool enable = true,
                                                   bool skipGroup = false,
                                                   float labelWidth = 0,
                                                   float width = 0)
        {
            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = enable;

            if (labelWidth == 0) GUILayout.Label(label, LibraryGUIStyle.textDefault);
            else GUILayout.Label(label, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            int newMask = EditorGUILayout.MaskField(currentValue.value, options, LibraryGUIStyle.popUpFieldSmallText, GUILayout.Width(width));

            GUI.enabled = true;
            if (!skipGroup) GUILayout.EndHorizontal();

            if (newMask != currentValue.value)
            {
                actionOnValueChange.Invoke(newMask);
            }
        }

        public static void CreateToggleField(string label, 
                                             bool currentValue, 
                                             Action<bool> actionOnValueChange, 
                                             bool enable = true, 
                                             bool skipGroup = false, 
                                             float labelWidth = 0, 
                                             float width = 0)
        {
            bool oldValue = currentValue;
            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = enable;
            if(label.Length > 0)
            {
                if (labelWidth == 0) GUILayout.Label(label, LibraryGUIStyle.textDefault);
                else GUILayout.Label(label, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            }
            if(width == 0) currentValue = EditorGUILayout.Toggle(currentValue);
            else currentValue = EditorGUILayout.Toggle(currentValue, GUILayout.Width(width));

            GUI.enabled = true;
            if (!skipGroup) GUILayout.EndHorizontal();

            if (currentValue == oldValue)
                return;

            actionOnValueChange.Invoke(currentValue);
        }

        public static void CreateToggleXYZField(string label, 
                                                Vector3Int currentValue, 
                                                Action<Vector3Int> actionOnValueChange, 
                                                float paddingLeft = 6, 
                                                bool skipGroup = false)
        {
            Vector3Int oldValue = currentValue;

            //Follow axels
            if(!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUILayout.Space(paddingLeft);
            GUILayout.Label(label, LibraryGUIStyle.textNoWdith);
            GUILayout.Label("X", LibraryGUIStyle.specificX);
            currentValue.x = EditorGUILayout.Toggle(currentValue.x != 0) ? 1 : 0;

            GUILayout.Label("Y", LibraryGUIStyle.specificYZ);
            currentValue.y = EditorGUILayout.Toggle(currentValue.y != 0) ? 1 : 0;

            GUILayout.Label("Z", LibraryGUIStyle.specificYZ);
            currentValue.z = EditorGUILayout.Toggle(currentValue.z != 0) ? 1 : 0;
            if (!skipGroup) GUILayout.EndHorizontal();

            if (currentValue == oldValue)
                return;

            actionOnValueChange.Invoke(currentValue);
        }

        public static void CreateDelayedFloatFieldWithLabel(string label,
                                                            float currentValue,
                                                            Action<float> actionOnValueChange,
                                                            float width = -1,
                                                            float labelWidth = -1,
                                                            bool skipGroup = false,
                                                            bool enable = true,
                                                            string controllerName = null)
        {
            float newValue;
            float fieldValue = currentValue;
            bool pendigDif = false;
            GUIStyle guiStyle = LibraryGUIStyle.textFieldDelayedNoWidth;

            string cName = controllerName == null ? label : controllerName;

            if (isPending && GUI.GetNameOfFocusedControl() == cName && !GeneralUtility.IsEqual(pendingFloat, currentValue))
            {
                guiStyle = LibraryGUIStyle.textFieldPendingNoWidth;
                fieldValue = pendingFloat;
                pendigDif = true;
            }

            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = enable;

            if (labelWidth == -1) EditorGUILayout.LabelField(label, LibraryGUIStyle.textDefault);
            else EditorGUILayout.LabelField(label, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));

            GUI.SetNextControlName(cName);
            if (width == -1) newValue = EditorGUILayout.FloatField(fieldValue, guiStyle);
            else newValue = EditorGUILayout.FloatField(fieldValue, guiStyle, GUILayout.Width(width));

            if (pendigDif)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect = new Rect(new Vector2(rect.position.x + Mathf.RoundToInt(rect.size.x / 2) - 14, rect.position.y - rect.size.y + 10), new Vector2(45, 30));
                GUI.Label(rect, "Enter", LibraryGUIStyle.pressEnter);
            }

            GUI.enabled = true;
            if (!skipGroup) GUILayout.EndHorizontal();

            Event e = Event.current;
            if (!GeneralUtility.IsEqual(newValue, currentValue) && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                isPending = false;
                actionOnValueChange.Invoke(newValue);
                GUI.FocusControl(null);
                WindowBase.RepaintAll();
            }
            else if (GUI.GetNameOfFocusedControl() == cName)
            {
                isPending = true;
                pendingFloat = newValue;
            }
        }

        public static void CreateFloatFieldWithLabel(string label,
                                             float currentValue,
                                             Action<float> actionOnValueChange,
                                             float width = -1,
                                             float labelWidth = -1,
                                             bool skipGroup = false,
                                             bool enable = true)
        {
            float newValue;

            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = enable;
            if (labelWidth == -1) EditorGUILayout.LabelField(label, LibraryGUIStyle.textDefault);
            else EditorGUILayout.LabelField(label, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));

            if (width == -1) newValue = EditorGUILayout.FloatField(currentValue, LibraryGUIStyle.textFieldNoWidth);
            else newValue = EditorGUILayout.FloatField(currentValue, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(width));
            GUI.enabled = true;
            if (!skipGroup) GUILayout.EndHorizontal();

            if (GeneralUtility.IsEqual(newValue, currentValue))
                return;

            actionOnValueChange.Invoke(newValue);
        }

        public static void CreateFloatField(float currentValue, Action<float> actionOnValueChange, 
                                                                float width = -1, 
                                                                bool skipGroup = false)
        {
            float newValue;
            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            if (width == -1) newValue = EditorGUILayout.FloatField(currentValue, LibraryGUIStyle.textFieldNoWidth);
            else newValue = EditorGUILayout.FloatField(currentValue, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(width));
            if (!skipGroup) GUILayout.EndHorizontal();

            if (GeneralUtility.IsEqual(newValue, currentValue))
                return;

            actionOnValueChange.Invoke(newValue);
        }

        public static void CreateDelayedFloatField(string controlName, float currentValue, 
                                                                       Action<float> actionOnValueChange, 
                                                                       float width = -1, 
                                                                       bool skipGroup = false)
        {
            float newValue;
            float fieldValue = currentValue;
            bool pendigDif = false;
            GUIStyle guiStyle = LibraryGUIStyle.textFieldDelayedNoWidth;

            if (isPending && GUI.GetNameOfFocusedControl() == controlName && !GeneralUtility.IsEqual(pendingFloat, currentValue))
            {
                guiStyle = LibraryGUIStyle.textFieldPendingNoWidth;
                fieldValue = pendingFloat;
                pendigDif = true;
            }

            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.SetNextControlName(controlName);
            if (width == -1) newValue = EditorGUILayout.FloatField(fieldValue, guiStyle);
            else newValue = EditorGUILayout.FloatField(fieldValue, guiStyle, GUILayout.Width(width));
            if (!skipGroup) GUILayout.EndHorizontal();

            if (pendigDif)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect = new Rect(new Vector2(rect.position.x + Mathf.RoundToInt(rect.size.x / 2) - 14, rect.position.y - rect.size.y + 10), new Vector2(45, 30));
                GUI.Label(rect, "Enter", LibraryGUIStyle.pressEnter);
            }

            Event e = Event.current;
            if (!GeneralUtility.IsEqual(newValue, currentValue) && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                isPending = false;
                actionOnValueChange.Invoke(newValue);
                GUI.FocusControl(null);
                WindowBase.RepaintAll();
            }
            else if (GUI.GetNameOfFocusedControl() == controlName)
            {
                isPending = true;
                pendingFloat = newValue;
            }
        }

        public static void CreatePopupField(string label, 
                                            float width, 
                                            int currentType, 
                                            string[] options, 
                                            Action<int> actionOnValueChange, 
                                            float labelWidth = -1, 
                                            bool skipGroup = false, 
                                            bool enable = true, 
                                            bool skipLabel = false, 
                                            bool blackText = false)
        {
            int oldType = currentType;

            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = enable;
            if(!skipLabel)
            {
                if(labelWidth == -1)
                    GUILayout.Label(label, blackText ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault);
                else
                    GUILayout.Label(label, blackText ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            }
            currentType = EditorGUILayout.Popup(currentType, options, LibraryGUIStyle.popUpFieldSmallText, GUILayout.Width(width));
            GUI.enabled = true;
            if (!skipGroup) GUILayout.EndHorizontal();

            if (currentType == oldType)
                return;

            actionOnValueChange.Invoke(currentType);
        }

        public static void CreateMinMaxSlider(string label, 
                                              ref float minValue, 
                                              ref float maxValue, 
                                              float minLimit, 
                                              float maxLimit, 
                                              float labelWidth, 
                                              float sliderWidth = -1, 
                                              bool skipGroup = false)
        {
            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            string startValue = Mathf.Round(minValue * 100).ToString();
            string endValue = Mathf.Round(maxValue * 100).ToString();

            GUILayout.Label($"{label} {startValue}", LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            if(sliderWidth == -1)
                EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, minLimit, maxLimit);
            else
                EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, minLimit, maxLimit, GUILayout.Width(sliderWidth));
            GUILayout.Label(endValue, LibraryGUIStyle.textDefault, GUILayout.Width(30));
            if (!skipGroup) GUILayout.EndHorizontal();
        }

        public static void CreateMinMaxSliderDelayedFields(string label,
                                                           int index,
                                                      ref float minValue,
                                                      ref float maxValue,
                                                      float minLimit,
                                                      float maxLimit,
                                                      Action<bool> actionOnValueChange,
                                                      float fieldWidth,
                                                      bool leftMouseUp,
                                                      bool skipGroup = false)
        {
            float fieldMinValue = minValue * 100;
            float fieldMaxValue = maxValue * 100;
            GUIStyle guiStyleMin = LibraryGUIStyle.textFieldDelayedNoWidth;
            GUIStyle guiStyleMax = LibraryGUIStyle.textFieldDelayedNoWidth;
            bool changedBySlider = false;
            bool pendingDif = false;
            Rect lastRect = new Rect();

            string minFieldControler = $"{label} {index} min field";
            string maxFieldControler = $"{label} {index} max field";

            // Update pendding values
            if (isPending)
            {
                if (!GeneralUtility.IsEqual(pendingFloat, minValue) && (GUI.GetNameOfFocusedControl() == minFieldControler || minMaxSliderControlerId == index))
                {
                    guiStyleMin = LibraryGUIStyle.textFieldPendingNoWidth;
                    fieldMinValue = pendingFloat * 100;
                    pendingDif = true;
                }

                if (!GeneralUtility.IsEqual(pendingFloat2, maxValue) && (GUI.GetNameOfFocusedControl() == maxFieldControler || minMaxSliderControlerId == index))
                {
                    guiStyleMax = LibraryGUIStyle.textFieldPendingNoWidth;
                    fieldMaxValue = pendingFloat2 * 100;
                    pendingDif = true;
                }
            }


            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());

            GUI.SetNextControlName(minFieldControler);
            fieldMinValue = EditorGUILayout.FloatField(fieldMinValue, guiStyleMin, GUILayout.Width(fieldWidth));
            fieldMinValue = fieldMinValue / 100;
            if (!GeneralUtility.IsEqual(fieldMinValue, minValue)) lastRect = GUILayoutUtility.GetLastRect();

            fieldMaxValue = fieldMaxValue / 100;
            float oldMax = fieldMaxValue;
            float oldMin = fieldMinValue;
            EditorGUILayout.MinMaxSlider(ref fieldMinValue, ref fieldMaxValue, minLimit, maxLimit);


            if (!GeneralUtility.IsEqual(oldMin, fieldMinValue) || !GeneralUtility.IsEqual(oldMax, fieldMaxValue))
            {
                changedBySlider = true;
                GUI.FocusControl(null);
                minMaxSliderControlerId = index;
            }

            GUI.SetNextControlName(maxFieldControler);
            fieldMaxValue = fieldMaxValue * 100;
            fieldMaxValue = EditorGUILayout.FloatField(fieldMaxValue, guiStyleMax, GUILayout.Width(fieldWidth));
            fieldMaxValue = fieldMaxValue / 100;

            if (!GeneralUtility.IsEqual(fieldMaxValue, maxValue)) lastRect = GUILayoutUtility.GetLastRect();

            // Enter info box
            if (pendingDif && EditorGUIUtility.editingTextField)
            {
                lastRect = new Rect(new Vector2(lastRect.position.x + lastRect.size.x - (fieldWidth / 2) - 14, lastRect.position.y - lastRect.size.y + 10), new Vector2(45, 30));
                GUI.Label(lastRect, "Enter", LibraryGUIStyle.pressEnter);
            }

            if (!skipGroup) GUILayout.EndHorizontal();

            Event e = Event.current;
            bool foundChange = !GeneralUtility.IsEqual(fieldMinValue, minValue) || !GeneralUtility.IsEqual(fieldMaxValue, maxValue);
            if (foundChange && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || (leftMouseUp && !EditorGUIUtility.editingTextField)))
            {
                isPending = false;
                minValue = Mathf.Round(fieldMinValue * 10000) / 10000;
                maxValue = Mathf.Round(fieldMaxValue * 10000) / 10000;
                actionOnValueChange.Invoke(changedBySlider);
                GUI.FocusControl(null);
                WindowBase.RepaintAll();
                minMaxSliderControlerId = -1;
            }
            else if (GUI.GetNameOfFocusedControl() == minFieldControler || GUI.GetNameOfFocusedControl() == maxFieldControler || changedBySlider)
            {
                isPending = true;
                pendingFloat = fieldMinValue;
                pendingFloat2 = fieldMaxValue;
            }
        }

        public static void CreateCurveField(string label, AnimationCurve curve, AnimationCurve curveContainer, Action<AnimationCurve> actionOnValueChange, float width, bool skipGroup = false, float labelWidth = 0)
        {
            if (!skipGroup)
                GUILayout.BeginHorizontal(GetBackgroundStyle());

            if (labelWidth == 0)
                GUILayout.Label(label, LibraryGUIStyle.textDefault);
            else
                GUILayout.Label(label, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));

            EditorGUI.BeginChangeCheck();

            if (EHandleUndo.UndoTriggered() || curveContainer.length == 0)
            {
                curveContainer.CopyFrom(curve);
                RefreshOpenCurveEditorWindow();
            }

            AnimationCurve editedCurve = EditorGUILayout.CurveField(curveContainer, GUILayout.Width(width));

            if (!skipGroup)
                GUILayout.EndHorizontal();

            if (!EditorGUI.EndChangeCheck())
                return;

            actionOnValueChange.Invoke(curveContainer);
            RefreshOpenCurveEditorWindow();

            void RefreshOpenCurveEditorWindow()
            {
                EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();

                for (int i = 0; i < windows.Length; i++)
                {
                    EditorWindow window = windows[i];

                    if (window == null)
                        continue;

                    Type type = window.GetType();

                    if (type.FullName != "UnityEditor.CurveEditorWindow")
                        continue;

                    MethodInfo refreshShownCurvesMethod = GetMethodNoParameters(type, "RefreshShownCurves");

                    if (refreshShownCurvesMethod == null)
                    {
                        Debug.LogError("[Spline Architect] Could not find RefreshShownCurves method, API has changed!");
                        return;
                    }

                    refreshShownCurvesMethod.Invoke(window, null);
                    window.Repaint();
                }

                MethodInfo GetMethodNoParameters(Type type, string methodName)
                {
                    MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    for (int i = 0; i < methods.Length; i++)
                    {
                        if (methods[i].Name != methodName)
                            continue;

                        if (methods[i].GetParameters().Length == 0)
                            return methods[i];
                    }

                    return null;
                }
            }
        }

        public static void CreateErrorWarningMessageIcon(GUIContent guiContent, bool moveBack = false)
        {
            GUILayout.Label(guiContent, LibraryGUIStyle.infoIcon);
            if (moveBack) GUILayout.Space(-6);
        }

        public static void CreateInfoMessageIcon(GUIContent guiContent, bool moveBack = false)
        {
            if(EGlobalSettings.GetInfoIconsVisibility())
            {
                GUILayout.Label(guiContent, LibraryGUIStyle.infoIcon);
                if(moveBack) GUILayout.Space(-6);
            }
        }

        public static void CreateHorizontalYellowLine()
        {
            GUILayout.Box("", LibraryGUIStyle.lineYellow, GUILayout.Height(1), GUILayout.ExpandWidth(true));
        }

        public static void CreateHorizontalBlackLine()
        {
            GUILayout.Box("", LibraryGUIStyle.lineBlack, GUILayout.Height(1), GUILayout.ExpandWidth(true));
        }

        public static void CreateHorizontalWhiteLine()
        {
            GUILayout.Box("", LibraryGUIStyle.lineWhite, GUILayout.Height(1), GUILayout.ExpandWidth(true));
        }

        public static void CreateHorizontalGreyLine80()
        {
            GUILayout.Box("", LibraryGUIStyle.lineGrey80, GUILayout.Height(1), GUILayout.ExpandWidth(true));
        }

        public static void CreateHorizontalGreyLine30()
        {
            GUILayout.Box("", LibraryGUIStyle.lineGrey30, GUILayout.Height(1), GUILayout.ExpandWidth(true));
        }

        public static void CreateHorizontalGreyLine20()
        {
            GUILayout.Box("", LibraryGUIStyle.lineGrey20, GUILayout.Height(1), GUILayout.ExpandWidth(true));
        }

        public static void CreateHorizontalGreyLine40()
        {
            GUILayout.Box("", LibraryGUIStyle.lineGrey40, GUILayout.Height(1), GUILayout.ExpandWidth(true));
        }

        public static void CreateHorizontalSubHeader2Line()
        {
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundLayerSection);
            GUILayout.Space(10);
            GUILayout.Box("", LibraryGUIStyle.lineGrey30, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }

        public static void CreateVerticalYellowLine()
        {
            GUILayout.Box("", LibraryGUIStyle.lineYellow, GUILayout.Width(1), GUILayout.ExpandHeight(true));
        }

        public static void CreateVerticalBlackLine()
        {
            GUILayout.Box("", LibraryGUIStyle.lineBlack, GUILayout.Width(1), GUILayout.ExpandHeight(true));
        }

        public static void CreateVerticalWhiteLine()
        {
            GUILayout.Box("", LibraryGUIStyle.lineWhite, GUILayout.Width(1), GUILayout.ExpandHeight(true));
        }

        public static void CreateVerticalGreyLine80()
        {
            GUILayout.Box("", LibraryGUIStyle.lineGrey80, GUILayout.Width(1), GUILayout.ExpandHeight(true));
        }

        public static void CreateSeparator()
        {
            GUILayout.Box("", LibraryGUIStyle.separatorWhite);
        }

        public static void CreateSpaceWidth(float width)
        {
            GUILayout.Label(LibraryTexture.empty, GUILayout.Width(width));
        }

        public static GUIStyle GetBackgroundStyle(bool keepOld = false)
        {
            if(!keepOld)
                backgroundStyleCounter++;

            if (backgroundStyleCounter % 2 == 0)
                return LibraryGUIStyle.backgroundItem1;
            else
                return LibraryGUIStyle.backgroundItem2;
        }

        public static void ResetGetBackgroundStyleId()
        {
            backgroundStyleCounter = 1;
        }

        public static Vector2 GetWindowAnchorPosition(SceneView sceneView, 
                                                      Rect buttonWorldBound, 
                                                      bool isRow)
        {
            Rect win = sceneView.position;
            Rect wb = buttonWorldBound;
            if (isRow) return new Vector2(win.x + wb.x, win.y + wb.yMax + 5);
            else return new Vector2(win.x + wb.xMax + 5, win.y + wb.y);
        }
    }
}
