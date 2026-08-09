using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/InstanceSystem/TornadoEffect")]
    public class TornadoEffect : MonoBehaviour
    {
        public float effectSpeed;

        private Spline spline;
        private Segment activeSegment;
        private float segmentScale;

        private float timer;

        void Start()
        {
            spline = GetComponent<Spline>();
            UpdateActiveSegment();
        }

        void Update()
        {
            if (timer >= 1)
            {
                activeSegment.Scale = new Vector2(1, 1);
                timer = 0;
                UpdateActiveSegment();
            }
            else
            {
                if (timer <= 0.5f)
                {
                    float lerpTime = timer / 0.5f;
                    lerpTime = EasingUtility.EvaluateEasing(lerpTime, Easing.EASE_IN_OUT_SINE);
                    float lerpedScale = Mathf.Lerp(1, segmentScale, lerpTime);
                    activeSegment.Scale = new Vector2(lerpedScale, lerpedScale);
                }
                else
                {
                    float lerpTime = (timer - 0.5f) * 2f;
                    lerpTime = EasingUtility.EvaluateEasing(lerpTime, Easing.EASE_IN_OUT_SINE);
                    float lerpedScale = Mathf.Lerp(segmentScale, 1, lerpTime);
                    activeSegment.Scale = new Vector2(lerpedScale, lerpedScale);
                }
            }

            timer += Time.deltaTime * effectSpeed;
        }

        private void UpdateActiveSegment()
        {
            int index = Random.Range(1, spline.SegmentCount - 1);
            activeSegment = spline.GetSegmentAtIndex(index);
            segmentScale = Random.Range(0.1f, 0.2f);
        }
    }
}
