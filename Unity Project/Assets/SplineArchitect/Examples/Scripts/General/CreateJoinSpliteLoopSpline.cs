using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/General/CreateJoinSpliteLoopSpline")]
    public class CreateJoinSpliteLoopSpline : MonoBehaviour
    {
        private enum State
        {
            IDLE,
            LOOP_ENABLE,
            LOOP_DISABLE,
            SPLIT,
            JOIN,
        }

        public Material material;

        [Header("General")]
        public float tangentDistance = 10;
        public float delay = 4;

        [Header("Spline")]
        public List<Transform> anchors;

        [Header("Follower")]
        public GameObject prefab;
        public float speed;
        public float maxDistance;

        State state = State.IDLE;
        private Spline spline;
        private Spline spline2;
        private SplineObject splineObject;
        private float timer;
        private List<Vector3> segmentPoints = new List<Vector3>();

        void Start()
        {
            //Spline 1
            segmentPoints.Clear();
            foreach (Transform t in anchors)
            {
                segmentPoints.Add(t.position);
                segmentPoints.Add(t.position + t.forward * tangentDistance);
                segmentPoints.Add(t.position - t.forward * tangentDistance);
            }
            GameObject go = new GameObject("Spline (1)");
            spline = SplineUtility.Create(go, segmentPoints, Color.red, Space.World);
            spline.RenderInGame = true;
            spline.jobInterval = 8;

            //Prefab
            go = Instantiate(prefab);
            splineObject = spline.CreateFollower(go, Vector3.zero, Quaternion.identity);
        }

        void Update()
        {
            timer += Time.deltaTime;

            splineObject.localSplinePosition.z += speed * Time.deltaTime;

            //If at the end of the spline go back to start and switch to the splitted spline if it exist.
            if (splineObject.localSplinePosition.z > splineObject.SplineParent.Length)
            {
                if(splineObject.transform.parent == spline.transform && spline2 != null)
                    splineObject.transform.parent = spline2.transform;
                else if (spline2 != null && splineObject.transform.parent == spline2.transform)
                    splineObject.transform.parent = spline.transform;

                splineObject.localSplinePosition.z = 0;

                //Switch between deformation and follower for testing purposes.
                if (splineObject.Type == SplineObjectType.DEFORMATION)
                    splineObject.Type = SplineObjectType.FOLLOWER;
                else
                    splineObject.Type = SplineObjectType.DEFORMATION;
            }

            if (state == State.IDLE)
            {
                if (timer > delay)
                {
                    spline.SetLoop(true);
                    state = State.LOOP_ENABLE;
                    timer = 0;
                }
            }
            if (state == State.LOOP_ENABLE)
            {
                if (timer > delay)
                {
                    spline.SetLoop(false);
                    state = State.LOOP_DISABLE;
                    timer = 0;
                }
            }
            if (state == State.LOOP_DISABLE)
            {
                if (timer > delay)
                {
                    //Create new spline
                    GameObject go = new GameObject("Spline (2)");
                    spline2 = go.AddComponent<Spline>();
                    spline.Split(spline2, 1);

                    //Move the start of the spline forward
                    spline2.GetSegmentAtIndex(0).TranslateAnchor(-Vector3.forward * 5);
                    spline2.RenderInGame = true;
                    spline2.color = Color.blue;

                    state = State.SPLIT;
                    timer = 0;
                }
            }
            if (state == State.SPLIT)
            {
                if (timer > delay)
                {
                    splineObject.transform.parent = spline.transform;
                    Segment segmentToDelete = spline2.GetSegmentAtIndex(0);
                    spline.Join(spline2, JoinType.END_TO_START);
                    state = State.JOIN;
                    timer = 0;
                    
                    spline.RemoveSegment(segmentToDelete);
                    Destroy(spline2.gameObject);
                }
            }
            if (state == State.JOIN)
            {
                state = State.IDLE;
                timer = 0;
            }
        }
    }
}
