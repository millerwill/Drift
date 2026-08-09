// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EMouseUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 03-02-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

namespace SplineArchitect.Utility
{
    public class EMouseUtility
    {
        public static Ray GetMouseRay(Vector2 mousePosition)
        {
            if (EHandleSceneView.GetCurrent().orthographic)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
                Vector3 cameraPos = EHandleSceneView.GetCurrent().camera.transform.position;
                Vector3 direction = (cameraPos - ray.origin).normalized;
                float distance = Vector3.Distance(ray.origin, cameraPos);
                return new Ray(cameraPos - (direction * distance), EHandleSceneView.GetCurrent().camera.transform.forward);
            }
            else
                return HandleUtility.GUIPointToWorldRay(mousePosition);
        }

        public static float MouseDistanceToPoint(Vector3 point, Ray mouseRay)
        {
            float distanceToPoint = Vector3.Distance(point, mouseRay.origin);
            return Vector3.Distance(point, mouseRay.GetPoint(distanceToPoint));
        }
    }
}
