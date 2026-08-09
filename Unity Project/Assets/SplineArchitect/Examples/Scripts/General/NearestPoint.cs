using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/General/NearestPoint")]
    public class NearestPoint : MonoBehaviour
    {
        public enum State
        {
            IDLE,
            MOVING_UP,
            WAIT_BEFORE_MOVE,
            MOVING,
            AT_TARGET,
            MOVING_DOWN,
        }

        public Spline spline;
        [Range(1, 5)]
        public float speed;
        [Range(1, 74)]
        public float spawnRange;
        [Range(0, 2)]
        public float groundOffset;

        private State state;
        private Vector3 fromPosition;
        private Vector3 toPosition;
        private float timer;

        void Update()
        {
            if(state == State.IDLE)
            {
                Vector3 randomPosition = new Vector3(Random.Range(-spawnRange, spawnRange), 50, Random.Range(-spawnRange, spawnRange));
                Physics.Raycast(randomPosition, Vector3.down, out RaycastHit hit, Mathf.Infinity);
                transform.position = hit.point + new Vector3(0, -4, 0);

                state = State.WAIT_BEFORE_MOVE;
                timer = 0;
            }
            else if (state == State.WAIT_BEFORE_MOVE)
            {
                timer += Time.deltaTime;

                if (timer >= 1)
                {
                    toPosition = transform.position + new Vector3(0, 4 + groundOffset, 0);
                    fromPosition = transform.position;
                    state = State.MOVING_UP;
                    timer = 0;
                }
            }
            else if (state == State.MOVING_UP)
            {
                timer += Time.deltaTime;
                gameObject.transform.position = Vector3.Lerp(fromPosition, toPosition, EasingUtility.EvaluateEasing(Mathf.Clamp01(timer), Easing.EASE_IN_OUT_SINE) * speed);

                if (timer >= 1)
                {
                    toPosition = spline.GetNearestPoint(transform.position);
                    fromPosition = transform.position;
                    state = State.MOVING;
                    timer = 0;
                }
            }
            else if (state == State.MOVING)
            {
                Vector3 targetIgnoreY = new Vector3(toPosition.x, 0, toPosition.z);
                Vector3 currentIgnoreY = new Vector3(gameObject.transform.position.x, 0, gameObject.transform.position.z);

                if (GeneralUtility.IsEqual(targetIgnoreY, currentIgnoreY))
                {
                    state = State.AT_TARGET;
                    timer = 0;
                }
                else
                {
                    timer += Time.deltaTime;
                    gameObject.transform.position = Vector3.Lerp(fromPosition, toPosition, EasingUtility.EvaluateEasing(Mathf.Clamp(timer, 0, 1), Easing.EASE_IN_OUT_SINE) * speed);
                    Physics.Raycast(transform.position + new Vector3(0, 50, 0), Vector3.down, out RaycastHit hit, Mathf.Infinity);
                        transform.position = hit.point + new Vector3(0, groundOffset, 0);
                }
            }
            else if (state == State.AT_TARGET)
            {
                timer += Time.deltaTime;
                if (timer >= 1)
                {
                    state = State.MOVING_DOWN;
                    fromPosition = transform.position;
                    toPosition = transform.position + new Vector3(0, -4, 0);
                    timer = 0;
                }
            }
            else if (state == State.MOVING_DOWN)
            {
                timer += Time.deltaTime;
                gameObject.transform.position = Vector3.Lerp(fromPosition, toPosition, EasingUtility.EvaluateEasing(Mathf.Clamp01(timer), Easing.EASE_IN_OUT_SINE) * speed);

                if (timer >= 1)
                {
                    state = State.IDLE;
                    timer = 0;
                }
            }
        }
    }
}
