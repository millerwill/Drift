// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EDebugUtilities.cs
//
// Author: Mikael Danielsson
// Date Created: 01-04-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SplineArchitect.Utility
{
    public class EDebugUtility
    {
        private static Dictionary<int, Stopwatch> stopWatches = new Dictionary<int, Stopwatch>();

        public static void StartTimer(int id = 0)
        {
            if(!stopWatches.ContainsKey(id))
                stopWatches.Add(id, new Stopwatch());

            stopWatches[id].Restart();
        }

        public static void LogElapsed(string message, int id = 0, bool stop = false)
        {
            Debug.Log($"[Spline Architect] {message}: {stopWatches[id].ElapsedMilliseconds} ms");
            if (stop) stopWatches[id].Stop();
        }

        public static void DrawBounds(Transform transform, Bounds bounds, Color color, float duration)
        {
            DrawBounds(GeneralUtility.TransformBoundsToWorldSpace(bounds, transform), color, duration);
        }

        public static void DrawBounds(Bounds bounds, Color color, float duration)
        {
            Vector3 v3FrontTopLeft = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z); // Front top left corner
            Vector3 v3FrontTopRight = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z); // Front top right corner
            Vector3 v3FrontBottomLeft = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z); // Front bottom left corner
            Vector3 v3FrontBottomRight = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z); // Front bottom right corner

            Vector3 v3BackTopLeft = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z); // Back top left corner
            Vector3 v3BackTopRight = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z); // Back top right corner
            Vector3 v3BackBottomLeft = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z); // Back bottom left corner
            Vector3 v3BackBottomRight = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z); // Back bottom right corner

            // Front
            Debug.DrawLine(v3FrontTopLeft, v3FrontTopRight, color, duration);
            Debug.DrawLine(v3FrontTopRight, v3FrontBottomRight, color, duration);
            Debug.DrawLine(v3FrontBottomRight, v3FrontBottomLeft, color, duration);
            Debug.DrawLine(v3FrontBottomLeft, v3FrontTopLeft, color, duration);

            // Back
            Debug.DrawLine(v3BackTopLeft, v3BackTopRight, color, duration);
            Debug.DrawLine(v3BackTopRight, v3BackBottomRight, color, duration);
            Debug.DrawLine(v3BackBottomRight, v3BackBottomLeft, color, duration);
            Debug.DrawLine(v3BackBottomLeft, v3BackTopLeft, color, duration);

            // Sides
            Debug.DrawLine(v3FrontTopLeft, v3BackTopLeft, color, duration);
            Debug.DrawLine(v3FrontTopRight, v3BackTopRight, color, duration);
            Debug.DrawLine(v3FrontBottomRight, v3BackBottomRight, color, duration);
            Debug.DrawLine(v3FrontBottomLeft, v3BackBottomLeft, color, duration);
        }

        public static void DrawNormal(Vector3 point, Vector3 x, Vector3 y, Vector3 z, float duration, float length = 3)
        {
            Debug.DrawLine(point, point + x * length, Color.red, duration);
            Debug.DrawLine(point, point + y * length, Color.green, duration);
            Debug.DrawLine(point, point + z * length, Color.blue, duration);
        }

        public static void DrawDirection(Vector3 point, Vector3 direction, float duration, float length = 3)
        {
            Debug.DrawLine(point, point + direction * length, Color.red, duration);
        }
    }
}
#endif