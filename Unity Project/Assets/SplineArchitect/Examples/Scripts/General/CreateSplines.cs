using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/General/CreateSplines")]
    public class CreateSplines : MonoBehaviour
    {
        [Header("Spline")]
        public List<Transform> splinePoints = new List<Transform>();
        public float delay = 2;
        public float tangentDistance = 20;

        [Header("Deformation")]
        public GameObject prefab;
        public int prefabAmounts = 5;
        public float distanceBetween = 5;

        private int startPoint = 0;
        private int endPoint = 1;
        private List<Vector3> pointContainer = new List<Vector3>();
        private Spline splineToMove = null;

        private float timer = 0;
        private int splinesCreated;

        private Vector3 splineMoveStartPoint;
        private Vector3 splineMoveEndPoint;

        private void Update()
        {
            if (splinesCreated >= splinePoints.Count - 1)
                return;

            if(timer > delay)
            {
                Spline spline = CreateSpline(splinePoints[startPoint], splinePoints[endPoint]);

                //Set spline move points
                if(startPoint > 0)
                {
                    splineToMove = spline;
                    splineMoveStartPoint = splinePoints[startPoint].transform.position;
                    splineMoveEndPoint = splinePoints[startPoint].transform.position + splinePoints[startPoint].transform.forward * 5;
                }

                //Go to next points
                startPoint++;
                endPoint++;

                //Start over if end is reached
                if (endPoint >= splinePoints.Count)
                {
                    startPoint = 0;
                    endPoint = 1;
                }

                timer = 0;
            }

            //Move spline thats linked to another spline
            if(splineToMove != null)
            {
                float time = EasingUtility.EvaluateEasing(Mathf.Max(timer, 0.0001f) / delay, Easing.EASE_IN_OUT_QUINT);
                Vector3 newPoint = Vector3.Lerp(splineMoveStartPoint, splineMoveEndPoint, time);

                splineToMove.GetSegmentAtIndex(0).SetAnchorPosition(newPoint);

                if (GeneralUtility.IsEqual(time, 1, 0.001f))
                    splineToMove = null;
            }

            timer += Time.deltaTime;
        }

        private Spline CreateSpline(Transform startPoint, Transform endPoint)
        {
            //Create GameObject
            GameObject splineGo = new GameObject($"Spline nr {splinesCreated}");

            //Create anchors and tangents for new spline
            pointContainer.Clear();
            pointContainer.Add(startPoint.position);
            pointContainer.Add(startPoint.position - startPoint.forward * tangentDistance);
            pointContainer.Add(startPoint.position + startPoint.forward * tangentDistance);
            pointContainer.Add(endPoint.position);
            pointContainer.Add(endPoint.position - endPoint.forward * tangentDistance);
            pointContainer.Add(endPoint.position + endPoint.forward * tangentDistance);

            //Create spline
            Spline spline = SplineUtility.Create(splineGo, pointContainer);
            splinesCreated++;

            //Create deformations for spline
            CreateDeformations(spline);

            //Link spline
            spline.GetSegmentAtIndex(0).LinkToSegment(spline.GetSegmentAtIndex(0).GetPosition(ControlHandle.ANCHOR));

            return spline;
        }

        public void CreateDeformations(Spline spline)
        {
            for(int i = 0; i < prefabAmounts; i++)
            {
                GameObject deformationGo = Instantiate(prefab);
                SplineObject splineObject = spline.CreateDeformation(deformationGo, new Vector3(0, 0, 4 + distanceBetween * i), Quaternion.identity);

                //Snap last deformation to control point
                if(i == prefabAmounts - 1)
                {
                    SnapSettings snapSettings = new SnapSettings();
                    snapSettings.snapMode = SnapMode.CONTROL_POINTS;
                    snapSettings.endSnapDistance = 50;
                    splineObject.SnapSettings = snapSettings;
                }
            }
        }
    }
}
