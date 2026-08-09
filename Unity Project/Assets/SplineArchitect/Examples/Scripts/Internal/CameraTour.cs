using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("")]
    public class CameraTour : MonoBehaviour
    {
        private enum State
        {
            IDLE,
            TRANSITION,
        }

        public List<Transform> cameraPoints;
        public float speed;
        public float idleSpeed;
        public float idleTime;
        public float transitionTime;

        private State state;
        private float timer;
        private int cameraPoint;
        private Vector3 positionFrom;
        private Quaternion rotationFrom;

        private void Start()
        {
            if (cameraPoints == null || cameraPoints.Count == 0)
            {
                Debug.LogError("No camera points assigned.");
                return;
            }

            transform.position = cameraPoints[cameraPoint].position;
            transform.rotation = cameraPoints[cameraPoint].rotation;
        }

        void Update()
        {
            if (cameraPoints == null || cameraPoints.Count == 0)
                return;

            timer += Time.deltaTime;

            if (state == State.IDLE)
            {
                transform.position -= transform.forward * idleSpeed * Time.deltaTime;

                if (timer > idleTime)
                {
                    state = State.TRANSITION;
                    timer = 0f;
                    cameraPoint++;

                    if (cameraPoint >= cameraPoints.Count)
                        cameraPoint = 0;

                    positionFrom = transform.position;
                    rotationFrom = transform.rotation;
                }
            }
            if (state == State.TRANSITION)
            {
                float time = timer / transitionTime;
                time = EasingUtility.EvaluateEasing(time, Easing.EASE_IN_OUT_CUBIC);
                transform.position = Vector3.Lerp(positionFrom, cameraPoints[cameraPoint].position, time);
                transform.rotation = Quaternion.Lerp(rotationFrom, cameraPoints[cameraPoint].rotation, time);

                if (timer >= transitionTime)
                {
                    state = State.IDLE;
                    timer = 0f;
                }
            }
        }
    }
}
