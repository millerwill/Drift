using UnityEngine;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/TrafficSystem/TrafficLight")]
    public class TrafficLight : MonoBehaviour
    {
        public enum State
        {
            GREEN,
            RED,
            ORANGE
        }


        public float greenLightTime = 5f;
        public float redLightTime = 5f;
        public float orangeLightTime = 2f;
        public State state = State.GREEN;

        private float stateTimer = 0f;
        private SplineObject splineObject;

        private void Start()
        {
            splineObject = GetComponent<SplineObject>();

            if(state == State.GREEN) 
                splineObject.localSplinePosition.x = 0.1f;

            if (state == State.ORANGE) stateTimer = orangeLightTime / 2;
        }

        void Update()
        {
            //GREEN
            if(state == State.GREEN)
            {
                if(stateTimer > greenLightTime)
                {
                    state = State.ORANGE;
                    stateTimer = 0f;

                    //Vehicles needs to have the same x position for detecting collision. 
                    splineObject.localSplinePosition.x = 0;
                }
            }
            //ORANGE
            else if (state == State.ORANGE)
            {
                if (stateTimer > orangeLightTime)
                {
                    state = State.RED;
                    stateTimer = 0f;
                }
            }
            //RED
            else if (state == State.RED)
            {
                if (stateTimer > redLightTime)
                {
                    state = State.GREEN;
                    stateTimer = 0f;

                    //Vehicles needs to have the same x position for detecting collision. We set it to 0.1 so vehicles can pass the traffic light.
                    splineObject.localSplinePosition.x = 0.1f;
                }
            }

            stateTimer += Time.deltaTime;
        }

        private void OnDrawGizmos()
        {
            if (state == State.GREEN)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(transform.position, 0.5f);
            }
            else if (state == State.ORANGE)
            {
                Gizmos.color = new Color(1, 0.4f, 0);
                Gizmos.DrawSphere(transform.position, 0.5f);
            }
            else if (state == State.RED)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position, 0.5f);
            }
        }
    }
}
