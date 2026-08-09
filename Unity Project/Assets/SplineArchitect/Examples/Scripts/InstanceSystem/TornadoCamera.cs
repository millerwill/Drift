using UnityEngine;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/InstanceSystem/TornadoCamera")]
    public class TornadoCamera : MonoBehaviour
    {
        public Spline spline;
        public float splineLookAtTime;
        public float speed;

        void LateUpdate()
        {
            Vector3 lookPos = spline.GetPosition(splineLookAtTime);
            transform.LookAt(lookPos);
            transform.position -= transform.forward * speed * Time.deltaTime;
        }
    }
}
