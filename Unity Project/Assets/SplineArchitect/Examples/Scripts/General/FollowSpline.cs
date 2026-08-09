using UnityEngine;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/General/FollowSpline")]
    public class FollowSpline : MonoBehaviour
    {
        [Header("Settings")]
        public float speed;

        private SplineObject splineObject;

        void Start()
        {
            splineObject = GetComponent<SplineObject>();
        }

        void Update()
        {
            splineObject.localSplinePosition.z += Time.deltaTime * speed;
        }
    }
}
