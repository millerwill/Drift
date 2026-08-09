// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: CustomEditorSplineConnector.cs
//
// Author: Mikael Danielsson
// Date Created: 09-01-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

using SplineArchitect.Libraries;

namespace SplineArchitect.CustomEditor
{
    [UnityEditor.CustomEditor(typeof(SplineConnector))]
    [CanEditMultipleObjects]
    public class CustomEditorSplineConnector : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            LibraryGUIContent.ReassureCustomEditorData();
            LibraryGUIStyle.ReassureCustomEditorData();

            float width = EditorGUIUtility.currentViewWidth - 40f;
            float height = LibraryGUIStyle.componentInfo.CalcHeight(LibraryGUIContent.textSplineConnectorComponent, width);

            EditorGUILayout.LabelField(LibraryGUIContent.textSplineConnectorComponent, LibraryGUIStyle.componentInfo, GUILayout.Height(height));
            EditorGUILayout.Space();

            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}