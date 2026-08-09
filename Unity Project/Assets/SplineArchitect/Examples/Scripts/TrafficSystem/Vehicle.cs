using System.Collections.Generic;

using UnityEngine;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/TrafficSystem/Vehicle")]
    [ExecuteAlways]
    public class Vehicle : MonoBehaviour
    {
        [Header("Wheels")]
        public float wheelRadius = 0.5f;
        public List<Transform> wheels;

        private float lastPositionZ;
        SplineObject so;

        protected void Start()
        {
            so = GetComponent<SplineObject>();

            if (so == null)
                return;

            lastPositionZ = so.localSplinePosition.z;
        }

        protected void Update()
        {
#if UNITY_EDITOR
            if (wheels == null || wheels.Count == 0)
                return;

            for (int i = 0; i < gameObject.GetComponentCount(); i++)
            {
                Component c = gameObject.GetComponentAtIndex(i);

                if (c is SplineObject)
                {
                    so = c as SplineObject;
                    break;
                }
            }

            if (so != null)
            {
#endif
                float delta = so.localSplinePosition.z - lastPositionZ;
                UpdateWheels(delta);
#if UNITY_EDITOR
            }
#endif

            if(so == null)
                return;

            lastPositionZ = so.localSplinePosition.z;
        }

        private void UpdateWheels(float delta)
        {
            float circumference = 2f * Mathf.PI * wheelRadius;
            float degreesThisFrame = delta / circumference * 360f;

            foreach (var w in wheels)
            {
                if (w == null)
                    continue;

                w.Rotate(Vector3.right, degreesThisFrame, Space.Self);
            }
        }
    }
}

