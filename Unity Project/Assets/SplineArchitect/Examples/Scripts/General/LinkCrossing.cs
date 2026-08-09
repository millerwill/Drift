using System.Collections.Generic;

using UnityEngine;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/General/LinkCrossing")]
    public class LinkCrossing : MonoBehaviour
    {
        [Header("Settings")]
        public Spline startSpline;
        public GameObject go;
        public int amount;
        public float speed;
        public bool skipSelf;

        private List<SplineObject> splineObjects = new List<SplineObject>();
        private List<Segment> links = new List<Segment>();

        void Start()
        {
            //Add the first follower (car) thats allready on the spline
            SplineObject follower = go.GetComponent<SplineObject>();
            splineObjects.Add(follower);

            //Copy the first follower and create more instances from that.
            for (int i = 1; i < amount; i++)
            {
                SplineObject clone = Instantiate(follower, startSpline.transform);
                clone.localSplinePosition = new Vector3(0, clone.localSplinePosition.y, Random.Range(0, startSpline.Length));
                splineObjects.Add(clone);
            }
        }

        void Update()
        {
            foreach (SplineObject so in splineObjects)
            {
                Spline currentSpline = so.SplineParent;

                //Reset the position if it goes beyond the spline length + 5
                if (so.localSplinePosition.z > currentSpline.Length + 5)
                {
                    so.localSplinePosition.z = 0;
                    so.transform.parent = startSpline.transform;
                    continue;
                }

                //Store old position before settings a new one
                Vector3 oldPosition = so.localSplinePosition;

                //Move the object forward
                so.localSplinePosition.z += Time.deltaTime * speed;

                //Clear the list of links
                links.Clear();

                //Find all links that are crossing the current position
                currentSpline.FindLinkCrossingsNonAlloc(links, so.localSplinePosition, oldPosition, skipSelf ? LinkFlags.SKIP_SELF : LinkFlags.NONE, out Segment currentSegment);

                if (links.Count == 0)
                    continue;

                Segment fromSegment = currentSegment;
                Segment toSegment = links[Random.Range(0, links.Count)];

                //Switch to new spline
                so.transform.parent = toSegment.SplineParent.transform;

                //Calculate the new Z position on the new spline
                //You can also do this: so.localSplinePosition.z = toSegment.zPosition;
                //Will not be as exakt but works in most cases.
                so.localSplinePosition.z = currentSpline.CalculateLinkCrossingZPosition(so.localSplinePosition, fromSegment, toSegment);
            }
        }
    }
}
