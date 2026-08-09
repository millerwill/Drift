// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EGeneralUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 26-04-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;
using UnityEditor;

namespace SplineArchitect.Utility
{
    public class EGeneralUtility
    {
        public static string GetScriptPath(Type type)
        {
            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                {
                    return path;
                }
            }

            return null;
        }

        public static string GetTempFolderPath()
        {
            return Application.dataPath.Substring(0, Application.dataPath.Length - "Asset/".Length) + "Temp";
        }
    }
}
