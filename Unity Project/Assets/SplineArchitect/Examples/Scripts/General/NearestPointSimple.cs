using UnityEngine;

namespace SplineArchitect.Examples
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("SplineArchitect/Examples/General/NearestPointSimple")]
    public class NearestPointSimple : MonoBehaviour
    {
        public Spline spline;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.5f);

            if (spline == null)
                return;

            Gizmos.color = Color.green;
            Vector3 nearestPoint = spline.GetNearestPoint(transform.position);
            Gizmos.DrawSphere(nearestPoint, 0.5f);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(transform.position, nearestPoint);
        }
    }
}
