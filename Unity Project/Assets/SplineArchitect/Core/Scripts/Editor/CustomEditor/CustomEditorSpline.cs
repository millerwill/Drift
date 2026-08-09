// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: CustomEditorSpline.cs
//
// Author: Mikael Danielsson
// Date Created: 07-01-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

using SplineArchitect.Libraries;

namespace SplineArchitect.CustomEditor
{
    [UnityEditor.CustomEditor(typeof(Spline))]
    [CanEditMultipleObjects]
    public class CustomEditorSpline : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            foreach (Object t in targets)
            {
                Spline spline = (Spline)t;
                if (spline.TryGetComponent<SplineObject>(out _))
                {
                    EditorGUILayout.HelpBox(
                        "Spline and SplineObject cannot be attached to the same GameObject.",
                        MessageType.Error);

                    return;
                }
            }

            LibraryGUIContent.ReassureCustomEditorData();
            LibraryGUIStyle.ReassureCustomEditorData();

            float width = EditorGUIUtility.currentViewWidth - 40f;
            float height = LibraryGUIStyle.componentInfo.CalcHeight(LibraryGUIContent.textSplineComponent, width);

            EditorGUILayout.LabelField(LibraryGUIContent.textSplineComponent, LibraryGUIStyle.componentInfo, GUILayout.Height(height));
            EditorGUILayout.Space();

            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}