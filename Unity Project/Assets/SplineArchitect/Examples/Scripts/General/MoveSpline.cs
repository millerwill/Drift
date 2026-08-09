using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/General/MoveSpline")]
    public class MoveSpline : MonoBehaviour
    {
        public int segment;
        public float speed;
        public float distanceToTangents;
        public List<Transform> points;

        private Spline spline;
        private int currentPoint;
        private float time;
        private Vector3 fromAnchor;
        private Vector3 fromTangentA;
        private Vector3 fromTangentB;
        private Vector3 toPosition;
        private Vector3 toForward;

        void Start()
        {
            spline = GetComponent<Spline>();
            fromAnchor = spline.GetSegmentAtIndex(segment).GetPosition(ControlHandle.ANCHOR);
            fromTangentA = spline.GetSegmentAtIndex(segment).GetPosition(ControlHandle.TANGENT_A);
            fromTangentB = spline.GetSegmentAtIndex(segment).GetPosition(ControlHandle.TANGENT_B);
            toPosition = points[currentPoint].transform.position;
            toForward = points[currentPoint].transform.forward;
        }

        void Update()
        {
            Vector3 anchor = spline.GetSegmentAtIndex(segment).GetPosition(ControlHandle.ANCHOR);

            if (time < 1.0f)
            {
                float easing = EasingUtility.EvaluateEasing(Mathf.Clamp01(time), Easing.EASE_IN_OUT_SINE);

                Vector3 newAnchor = Vector3.Lerp(fromAnchor, toPosition, easing);
                Vector3 newTangentA = Vector3.Lerp(fromTangentA, toPosition - toForward * distanceToTangents, easing);
                Vector3 newTangentB = Vector3.Lerp(fromTangentB, toPosition + toForward * distanceToTangents, easing);
                spline.GetSegmentAtIndex(segment).SetPosition(ControlHandle.ANCHOR, newAnchor);
                spline.GetSegmentAtIndex(segment).SetPosition(ControlHandle.TANGENT_A, newTangentA);
                spline.GetSegmentAtIndex(segment).SetPosition(ControlHandle.TANGENT_B, newTangentB);
            }
            else
            {
                currentPoint++;
                if(currentPoint >= points.Count)currentPoint = 0;

                fromAnchor = spline.GetSegmentAtIndex(segment).GetPosition(ControlHandle.ANCHOR);
                fromTangentA = spline.GetSegmentAtIndex(segment).GetPosition(ControlHandle.TANGENT_A);
                fromTangentB = spline.GetSegmentAtIndex(segment).GetPosition(ControlHandle.TANGENT_B);
                toPosition = points[currentPoint].transform.position;
                toForward = points[currentPoint].transform.forward;
                time = 0;
            }

            time += Time.deltaTime * speed;
        }
    }
}
