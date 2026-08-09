// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: CustomEditorSplineObject.cs
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
    [UnityEditor.CustomEditor(typeof(SplineObject))]
    [CanEditMultipleObjects]
    public class CustomEditorSplineObject : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            foreach (Object t in targets)
            {
                SplineObject so = (SplineObject)t;
                if (so.TryGetComponent<Spline>(out _))
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
            float height = LibraryGUIStyle.componentInfo.CalcHeight(LibraryGUIContent.textSplineObjectComponent, width);

            EditorGUILayout.LabelField(LibraryGUIContent.textSplineObjectComponent, LibraryGUIStyle.componentInfo, GUILayout.Height(height));
            EditorGUILayout.Space();

            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}