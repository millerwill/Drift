using System;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

namespace SplineArchitect.Examples
{

    [AddComponentMenu("SplineArchitect/Examples/TrainSystem/TrainHandler")]
    public class TrainHandler : MonoBehaviour
    {
        [Serializable]
        public class Train
        {
            public float speed;
            public List<SplineObject> cars;
            [HideInInspector]
            public Segment toSegment = null;
            public Segment fromSegment = null;
        }

        public List<Train> trains;

        private List<Segment> links = new List<Segment>();

        void Update()
        {
            foreach(Train train in trains)
            {
                for (int i = 0; i < train.cars.Count; i++)
                {
                    SplineObject car = train.cars[i];
                    Spline spline = car.SplineParent;

                    Vector3 oldPosition = car.localSplinePosition;
                    car.localSplinePosition.z += train.speed * Time.deltaTime;

                    links.Clear();
                    spline.FindLinkCrossingsNonAlloc(links, car.localSplinePosition, oldPosition, LinkFlags.SKIP_LAST, out Segment currentSegment);

                    if (links.Count == 0)
                        continue;

                    if (i == 0)
                    {
                        //Get random link (can be itself so it has a chance to continue on the same spline)
                        train.fromSegment = currentSegment;
                        train.toSegment = links[Random.Range(0, links.Count)];
                    }

                    if(train.toSegment == null || train.fromSegment == null)
                        continue;

                    //Change to new parent.
                    car.transform.parent = train.toSegment.SplineParent.transform;

                    //CalculateLinkCrossingZPosition is needed when multiple SplineObjects needs to keep their offset to eachother.
                    //If CalculateLinkCrossingZPosition where not used the SplineObjects (cars) will move slightly away from eachother after each link crossing.
                    car.localSplinePosition.z = spline.CalculateLinkCrossingZPosition(car.localSplinePosition, train.fromSegment, train.toSegment);
                }
            }
        }
    }
}
