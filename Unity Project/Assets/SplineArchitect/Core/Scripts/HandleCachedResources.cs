// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: HandleCachedResources.cs
//
// Author: Mikael Danielsson
// Date Created: 14-05-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using SplineArchitect.Utility;
using Unity.Collections;

namespace SplineArchitect
{
    public class HandleCachedResources
    {
        private static Dictionary<string, (Mesh, string)> instanceMeshes = new Dictionary<string, (Mesh, string)>();
        private static Dictionary<string, (Mesh, string, float, int)> modifiedOriginalMeshes = new Dictionary<string, (Mesh, string, float, int)>();
        private static Dictionary<string, (Vector3[], string)> originMeshVertices = new Dictionary<string, (Vector3[], string)>();
        private static Dictionary<string, (Vector3[], string)> originMeshNormals = new Dictionary<string, (Vector3[], string)>();
        private static Dictionary<string, (Vector4[], string)> originMeshTangents = new Dictionary<string, (Vector4[], string)>();
        private static Dictionary<string, (float[], string)> originMeshProjectionDeltaData = new Dictionary<string, (float[], string)>();
        private static Dictionary<ulong, (string, string)> stringKeys = new Dictionary<ulong, (string, string)>();

        private static List<string> clearContainer = new List<string>();
        private static List<ulong> clearKeysContainer = new List<ulong>();

#if UNITY_EDITOR
        public static bool canRunMaxResolutionMsg = false;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
#if UNITY_EDITOR
            if (EHandleEvents.playModeStateChange != UnityEditor.PlayModeStateChange.ExitingPlayMode &&
                EHandleEvents.playModeStateChange != UnityEditor.PlayModeStateChange.ExitingEditMode)
#endif
                ClearScene(scene);
        }

        internal static bool IsInstanceMeshCached(Mesh instanceMesh)
        {
            return instanceMeshes.ContainsKey(instanceMesh.name);
        }

        internal static bool IsInstanceMeshCached(string meshKey)
        {
            return instanceMeshes.ContainsKey(meshKey);
        }

        internal static Mesh FetchInstanceMesh(MeshContainer mc)
        {
            Mesh originMesh = mc.GetOriginMesh();

            if (originMesh == null)
                return null;

            string meshKey = mc.GetMeshKey();

            if (instanceMeshes.ContainsKey(meshKey))
            {
                if (instanceMeshes[meshKey].Item1 != null)
                {
#if UNITY_EDITOR
                    if (instanceMeshes[meshKey].Item1.name != meshKey)
                    {
                        instanceMeshes[meshKey].Item1.name = meshKey;
                        Debug.LogWarning("[Spline Architect] Fixed instance mesh with invalid name.");
                    }
#endif
                    return instanceMeshes[meshKey].Item1;
                }

                instanceMeshes.Remove(meshKey);
            }

            Mesh instanceMesh = null;

            if (mc.Resolution > 0)
            {
#if UNITY_EDITOR
                canRunMaxResolutionMsg = true;
#endif
                instanceMesh = UnityEngine.Object.Instantiate(FetchModifiedOriginalMesh(mc));
#if UNITY_EDITOR
                canRunMaxResolutionMsg = false;
#endif
            }
            else instanceMesh = UnityEngine.Object.Instantiate(originMesh);
            instanceMesh.name = meshKey;
            instanceMesh.MarkDynamic();
            instanceMeshes.Add(meshKey, (instanceMesh, mc.GetCachedSceneName()));

            return instanceMesh;
        }

        internal static Vector3[] FetchOriginVertices(MeshContainer mc)
        {
            Mesh originMesh = mc.GetOriginMesh();
            string key = mc.GetDataKey();

            if (originMeshVertices.ContainsKey(key))
                return originMeshVertices[key].Item1;

            Vector3[] vertices = null;
            if (mc.Resolution > 0) vertices = FetchModifiedOriginalMesh(mc).vertices;
            else vertices = originMesh.vertices;

            originMeshVertices.Add(key, (vertices, mc.GetCachedSceneName()));

            return vertices;
        }

        internal static Vector3[] FetchOriginNormals(MeshContainer mc)
        {
            Mesh originMesh = mc.GetOriginMesh();
            string dataKey = mc.GetDataKey();

            if (originMeshNormals.ContainsKey(dataKey))
                return originMeshNormals[dataKey].Item1;

            Vector3[] normals = null;
            if (mc.Resolution > 0) normals = FetchModifiedOriginalMesh(mc).normals;
            else normals = originMesh.normals;

            originMeshNormals.Add(dataKey, (normals, mc.GetCachedSceneName()));

            return normals;
        }

        internal static Vector4[] FetchOriginTangents(MeshContainer mc)
        {
            Mesh originMesh = mc.GetOriginMesh();
            string dataKey = mc.GetDataKey();

            if (originMeshTangents.ContainsKey(dataKey))
                return originMeshTangents[dataKey].Item1;

            Vector4[] normals = null;
            if(mc.Resolution > 0) normals = FetchModifiedOriginalMesh(mc).tangents;
            else normals = originMesh.tangents;

            originMeshTangents.Add(dataKey, (normals, mc.GetCachedSceneName()));

            return normals;
        }

        internal static Mesh FetchModifiedOriginalMesh(MeshContainer mc)
        {
            string dataKey = mc.GetDataKey();

            if (modifiedOriginalMeshes.ContainsKey(dataKey))
            {
                if(modifiedOriginalMeshes[dataKey].Item1 != null)
                {
#if UNITY_EDITOR
                    if(mc.GetOriginMesh() != null)
                        TryRunMaxResolutionMsg(mc.GetOriginMesh().name, mc.Resolution, modifiedOriginalMeshes[dataKey].Item4);
#endif
                    return modifiedOriginalMeshes[dataKey].Item1;
                }

                modifiedOriginalMeshes.Remove(dataKey);
            }

            Mesh originMesh = mc.GetOriginMesh();
            Mesh modifiedOriginalMesh = Utility.MeshUtility.GetSubdividedOriginalMesh(originMesh, mc.Resolution, mc.OnlyZResolution, out float longestEdge, out int maxResolution);
            modifiedOriginalMeshes.Add(dataKey, (modifiedOriginalMesh, mc.GetCachedSceneName(), longestEdge, maxResolution));

#if UNITY_EDITOR
            if (originMesh != null)
                TryRunMaxResolutionMsg(originMesh.name, mc.Resolution, maxResolution);
#endif
            return modifiedOriginalMesh;

#if UNITY_EDITOR
            void TryRunMaxResolutionMsg(string meshName, int resolution, int maxResolution)
            {
                if(maxResolution == -1)
                    return;

                if (!canRunMaxResolutionMsg)
                    return;

                if(maxResolution < resolution)
                    Debug.Log($"[Spline Architect] Mesh '{meshName}' reached its maximum resolution ({maxResolution}). " +
                              $"Increasing the resolution beyond this value will have no effect.");
            }
#endif
        }

        internal static float GetLongestEdgeFromModifiedOriginalMesh(MeshContainer mc)
        {
            string dataKey = mc.GetDataKey();
            if (modifiedOriginalMeshes.ContainsKey(dataKey))
                return modifiedOriginalMeshes[dataKey].Item3;

            FetchModifiedOriginalMesh(mc);
            return modifiedOriginalMeshes[dataKey].Item3;
        }

        internal static void AddOrUpdateInstanceMesh(MeshContainer mc)
        {
            mc.UpdateKeys();

            Mesh instanceMesh = mc.GetInstanceMesh();
            string meshKey = mc.GetMeshKey();

            if (instanceMeshes.ContainsKey(meshKey))
                instanceMeshes.Remove(meshKey);

            instanceMesh.name = meshKey;
            instanceMeshes.Add(meshKey, (instanceMesh, mc.GetCachedSceneName()));

            Mesh originMesh = mc.GetOriginMesh();
            string dataKey = mc.GetDataKey();

            if (originMeshVertices.ContainsKey(dataKey))
                originMeshVertices.Remove(dataKey);
            Vector3[] vertices = null;
            if (mc.Resolution > 0) vertices = FetchModifiedOriginalMesh(mc).vertices;
            else vertices = originMesh.vertices;
            originMeshVertices.Add(dataKey, (vertices, mc.GetCachedSceneName()));

            if (originMeshNormals.ContainsKey(dataKey))
                originMeshNormals.Remove(dataKey);
            Vector3[] normals = null;
            if (mc.Resolution > 0) normals = FetchModifiedOriginalMesh(mc).normals;
            else normals = originMesh.normals;
            originMeshNormals.Add(dataKey, (normals, mc.GetCachedSceneName()));

            if (originMeshTangents.ContainsKey(dataKey))
                originMeshTangents.Remove(dataKey);
            Vector4[] tangents = null;
            if (mc.Resolution > 0) tangents = FetchModifiedOriginalMesh(mc).tangents;
            else tangents = originMesh.tangents;
            originMeshTangents.Add(dataKey, (tangents, mc.GetCachedSceneName()));
        }

        internal static string FeatchStringKey(ulong hash64)
        {
            if (stringKeys.TryGetValue(hash64, out (string, string) value))
                return value.Item1;

            return null;
        }

        internal static void AddStringKey(ulong hash64, string value, string sceneName)
        {
            stringKeys.Add(hash64, (value, sceneName));
        }

        internal static float[] FeatchProjecetionDeltaData(MeshContainer mc)
        {
            Vector3[] originVertices = FetchOriginVertices(mc);

            string dataKey = mc.GetDataKey();
            if (originMeshProjectionDeltaData.ContainsKey(dataKey))
                return originMeshProjectionDeltaData[dataKey].Item1;

            int lowestVertex = 0;
            for (int i = 0; i < originVertices.Length; i++)
            {
                if (originVertices[i].y < originVertices[lowestVertex].y)
                    lowestVertex = i;
            }

            float[] deltaData = new float[originVertices.Length];
            for (int i = 0; i < originVertices.Length; i++)
            {
                deltaData[i] = originVertices[i].y - originVertices[lowestVertex].y;
            }

            originMeshProjectionDeltaData.Add(dataKey, (deltaData, mc.GetCachedSceneName()));
            return deltaData;
        }

        internal static void ClearScene(Scene scene)
        {
            string sceneName = scene.name;

#if UNITY_EDITOR
            // Switch scene for instance meshes if they have changed.
            foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
            {
                for (int i = 0; i < spline.AllSplineObjectCount; i++)
                {
                    SplineObject so = spline.GetSplineObjectAtIndex(i);

                    for (int i2 = 0; i2 < so.MeshContainerCount; i2++)
                    {
                        MeshContainer mc = so.GetMeshContainerAtIndex(i2);
                        string cachedSceneName = mc.GetCachedSceneName();
                        string newSceneName = so.gameObject.scene.name;

                        if (newSceneName != cachedSceneName)
                        {
                            string key = mc.GetMeshKey();
                            if (instanceMeshes.ContainsKey(key))
                            {
                                (Mesh, string) newValue = instanceMeshes[key];
                                newValue.Item2 = newSceneName;
                                instanceMeshes[key] = newValue;
                                mc.UpdateCachedSceneName(newSceneName);
                            }

                            ulong hashKey = mc.CreateHashForMeshKey();

                            if (stringKeys.ContainsKey(hashKey))
                            {
                                (string, string) newValue = stringKeys[hashKey];
                                newValue.Item2 = newSceneName;
                                stringKeys[hashKey] = newValue;
                            }
                        }
                    }
                }
            }
#endif

            clearContainer.Clear();
            foreach (KeyValuePair<string, (Vector3[], string)> item in originMeshVertices)
            {
                if (item.Value.Item2 == sceneName)
                    clearContainer.Add(item.Key);
            }
            foreach (string s in clearContainer)
            {
                originMeshVertices.Remove(s);
                originMeshNormals.Remove(s);
                originMeshTangents.Remove(s);
                originMeshProjectionDeltaData.Remove(s);

                if (modifiedOriginalMeshes.TryGetValue(s, out (Mesh, string, float, int) modifiedEntry))
                {
                    if (modifiedEntry.Item1 != null)
                    {
#if UNITY_EDITOR
                        if (Application.isPlaying)
                        {
                            UnityEngine.Object.Destroy(modifiedEntry.Item1);
                        }
                        else
                        {
                            EActionDelayed.Add(() =>
                            {
                                UnityEngine.Object.DestroyImmediate(modifiedEntry.Item1);
                            }, 0, 2, EActionDelayed.ActionFlag.FRAMES);
                        }
#else
                        UnityEngine.Object.Destroy(modifiedEntry.Item1);
#endif
                    }

                    modifiedOriginalMeshes.Remove(s);
                }
            }

            clearContainer.Clear();
            foreach (KeyValuePair<string, (Mesh, string)> item in instanceMeshes)
            {
                if (item.Value.Item2 == sceneName)
                    clearContainer.Add(item.Key);
            }
            foreach (string s in clearContainer)
            {
                if (instanceMeshes.TryGetValue(s, out (Mesh, string) instanceEntry))
                {
                    if (instanceEntry.Item1 != null)
                    {
#if UNITY_EDITOR
                        if (Application.isPlaying)
                        {
                            UnityEngine.Object.Destroy(instanceEntry.Item1);
                        }
                        else
                        {
                            EActionDelayed.Add(() =>
                            {
                                UnityEngine.Object.DestroyImmediate(instanceEntry.Item1);
                            }, 0, 2, EActionDelayed.ActionFlag.FRAMES);
                        }
#else
                        UnityEngine.Object.Destroy(instanceEntry.Item1);
#endif
                    }

                    instanceMeshes.Remove(s);
                }
            }

            clearKeysContainer.Clear();
            foreach (KeyValuePair<ulong, (string, string)> item in stringKeys)
            {
                if (item.Value.Item2 == sceneName)
                    clearKeysContainer.Add(item.Key);
            }
            foreach (ulong value in clearKeysContainer)
            {
                stringKeys.Remove(value);
            }
        }

        /// <summary>
        /// Gets the total number of cached instance meshes. 
        /// An instance mesh is a deformed copy
        /// created from a source mesh in your asset library.
        /// </summary>
        public static int GetInstanceMeshCount()
        {
            return instanceMeshes.Count;
        }

        /// <summary>
        /// Gets the number of Vector3 arrays that store the original mesh vertices,
        /// which are used by instance meshes during the deformation process.
        /// </summary>
        public static int GetOriginMeshVerticesCount()
        {
            return originMeshVertices.Count;
        }

        /// <summary>
        /// Gets the number of Vector3 arrays that store the original mesh normals,
        /// which are used by instance meshes during the deformation process.
        /// </summary>
        public static int GetOriginMeshNormalsCount()
        {
            return originMeshNormals.Count;
        }

        /// <summary>
        /// Gets the number of Vector4 arrays that store the original mesh tangents,
        /// which are used by instance meshes during the deformation process.
        /// </summary>
        public static int GetOriginMeshTangentsCount()
        {
            return originMeshTangents.Count;
        }

        /// <summary>
        /// Gets the numbers of modified orignal meshes currently pooled.
        /// </summary>
        public static int GetModifiedOriginalMeshesCount()
        {
            return modifiedOriginalMeshes.Count;
        }
    }
}