using UnityEngine;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/InstanceSystem/MoveRotateTornado")]
    public class MoveRotateTornado : MonoBehaviour
    {
        public Transform tornadoPoint;
        public float rotationSpeed;

        void LateUpdate()
        {
            transform.position = tornadoPoint.position;
            transform.Rotate(new Vector3(0, rotationSpeed * Time.deltaTime, 0));
        }
    }
}
