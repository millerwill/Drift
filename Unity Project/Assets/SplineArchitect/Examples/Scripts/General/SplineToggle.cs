using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/General/SplineToggle")]
    public class SplineToggle : MonoBehaviour
    {
        public Spline spline;
        public float snapToRailDistance = 10;

        private SplineObject activeSplineObject;

        private void Update()
        {
            Rect r = Camera.main.pixelRect;
            Vector3 mousePos = HandleInput.GetMousePosition();

            if (!r.Contains(mousePos))
                return;

            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (HandleInput.IsMouseLeftDown())
            {
                if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
                {
                    activeSplineObject = SplineObjectUtility.GetRootSplineObject(hit.transform);
                }
            }

            if (HandleInput.IsMouseLeftUp())
            {
                activeSplineObject = null;
            }

            if (activeSplineObject != null)
            {
                float mouseDistanceToSpline = 99999;

                if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, 1 << 6))
                {
                    Vector3 hitPoint = hit.point;
                    Vector3 nearestPoint = spline.GetNearestPointFixedTime(hitPoint, out float fixedTime, 8, 5, true);
                    Vector3 flatHitPoint = new Vector3(hitPoint.x, 0, hitPoint.z);
                    Vector3 flatNearestPoint = new Vector3(nearestPoint.x, 0, nearestPoint.z);

                    mouseDistanceToSpline = Vector3.Distance(flatHitPoint, flatNearestPoint);
                    if (mouseDistanceToSpline > snapToRailDistance)
                    {
                        activeSplineObject.transform.parent = null;
                        activeSplineObject.transform.position = hitPoint + new Vector3(0, 2.5f, 0);
                    }
                    else
                    {
                        activeSplineObject.localSplinePosition.z = spline.Length * fixedTime;
                        activeSplineObject.transform.parent = spline.transform;
                    }
                }
            }
        }
    }
}
