using UnityEngine;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("")]
    public class LookAt : MonoBehaviour
    {
        public Transform lookAtPoint;

        void LateUpdate()
        {
            transform.LookAt(lookAtPoint);
        }
    }
}
