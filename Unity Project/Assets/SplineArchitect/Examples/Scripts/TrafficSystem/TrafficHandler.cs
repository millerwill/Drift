using System;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/TrafficSystem/TrafficHandler")]
    public class TrafficHandler : MonoBehaviour
    {
        private struct VechicleData
        {
            public float speed;
            public float zExtendsRear;
            public float vechicleDistance;
        }

        [Header("Settings")]
        public int totalVechicles = 0;
        public float maxSpeed = 0;
        public float minSpeed = 0;
        public float minDistanceBetweenVechicles = 0;
        public float maxDistanceBetweenVechicles = 0;
        public float heightOffset;
        public bool performenceMode = false;

        [Header("Splines")]
        public List<Spline> splines;
        [Header("Vechicles")]
        public List<GameObject> prefabs;

        //General
        private Dictionary<SplineObject, VechicleData> vechicles = new Dictionary<SplineObject, VechicleData>();
        private List<SplineSearchResult> searchData = new List<SplineSearchResult>();
        private List<SplineObject> splineObjectsToSearch = new List<SplineObject>();
        private List<Segment> links = new List<Segment>();

        void Awake()
        {
            //Crate vechicles
            for (int i = 0; i < totalVechicles; i++)
            {
                GameObject prefab = Instantiate(prefabs[Random.Range(0, prefabs.Count)]);
                Spline spline = splines[Random.Range(0, splines.Count)];

                bool hasTrailer = prefab.GetComponent<VehicleAndTrailer>() != null;

                //Set speed
                float speed = Random.Range(minSpeed, maxSpeed);
                //Set vechicleDistance
                float vechicleDistance = Random.Range(minDistanceBetweenVechicles, maxDistanceBetweenVechicles);
                //Set position
                Vector3 position = new Vector3(0, heightOffset, Random.Range(0, spline.Length));
                //Create splineObject
                SplineObject splineObject;
                if ((hasTrailer || !performenceMode) && prefab.GetComponent<MeshFilter>() == null && prefab.transform.childCount > 0)
                {
                    splineObject = spline.CreateDeformation(prefab, position, Quaternion.identity);
                    for (int i2 = 0; i2 < prefab.transform.childCount; i2++)
                    {
                        Transform child = prefab.transform.GetChild(i2);
                        SplineObject soChild = spline.CreateFollower(child.gameObject, child.localPosition, child.localRotation, prefab.transform);

                        if (soChild.name.Contains("wheel_holder")) soChild.LockPosition = 1f;
                    }
                }
                else
                {
                    splineObject = spline.CreateFollower(prefab, position, Quaternion.identity);
                }

                //Stop wheels from spinning when performence mode is active
                if(performenceMode)
                {
                    for (int i2 = 0; i2 < prefab.GetComponentCount(); i2++)
                    {
                        Component c = prefab.GetComponentAtIndex(i2);

                        if (c is Vehicle)
                        {
                            if(hasTrailer)
                            {
                                Vehicle vehicle = c as Vehicle;
                                vehicle.wheels.Clear();
                            }
                            else
                                Destroy(c);

                            break;
                        }
                    }
                }

                //Calculate bounds for zExtendsRear
                MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
                Bounds bounds = new Bounds(prefab.transform.position, new Vector3(2,2,2));
                foreach (MeshFilter mf in meshFilters)
                {
                    Bounds b = new Bounds(mf.transform.TransformPoint(mf.sharedMesh.bounds.center), mf.sharedMesh.bounds.size);
                    bounds.Encapsulate(b);
                }

                //Create vechicle
                VechicleData vechicle = new VechicleData
                {
                    speed = speed,
                    zExtendsRear = bounds.extents.z,
                    vechicleDistance = vechicleDistance
                };

                splineObjectsToSearch.Add(splineObject);
                vechicles.Add(splineObject, vechicle);
            }

            //Add traffic lights to list
            foreach(Spline spline in splines)
            {
                for (int i = 0; i < spline.AllSplineObjectCount; i++)
                {
                    SplineObject so = spline.GetSplineObjectAtIndex(i);

                    if (so.name.Contains("TrafficLight"))
                        splineObjectsToSearch.Add(so);
                }
            }
        }

        void Update()
        {
            foreach(KeyValuePair<SplineObject, VechicleData> item in vechicles)
            {
                SplineSearchResultFlags searchFlags = SplineSearchResultFlags.SEARCH_FORWARD | SplineSearchResultFlags.NEED_SAME_X_POSITION | SplineSearchResultFlags.SEARCH_CLOSEST_LINK_FORWARD;
                Spline spline = item.Key.SplineParent;
                SplineObject so = item.Key;
                VechicleData vechicleData = item.Value;

                searchData.Clear();
                float speedManipulator = 1;
                spline.DistanceToClosestSplineObjectNonAlloc(searchData, so.localSplinePosition, 2, searchFlags);

                if (searchData.Count > 1)
                {
                    float distance = Mathf.Abs(searchData[1].distanceFromSplinePoint);
                    SplineObject closest = searchData[1].target;

                    //Fix for issue when two splineObjects have the same splinePositon. The issue is that they will just stop, this fixes that.
                    if (!GeneralUtility.IsZero(distance))
                    {
                        float vechicleDistance = vechicleData.vechicleDistance;
                        if(vechicles.ContainsKey(closest)) vechicleDistance += vechicles[closest].zExtendsRear;

                        speedManipulator = (distance - vechicleDistance) / vechicleDistance;
                        speedManipulator = Mathf.Clamp01(speedManipulator);
                    }
                }

                Vector3 prevSplinePosition = so.localSplinePosition;
                //To get the right speed when using different deformationIntervals we need to do this.
                float vechicleSpeed = vechicleData.speed;
                so.localSplinePosition.z += vechicleSpeed * Time.deltaTime * speedManipulator;

                //SEt link flags. Skip self if on a connection spline.
                LinkFlags linkFlags = LinkFlags.NONE;

                //Check for link crossings and cross to other splines
                links.Clear();
                spline.FindLinkCrossingsNonAlloc(links, so.localSplinePosition, prevSplinePosition, linkFlags, out Segment currentSegment);
                if (links.Count > 0)
                {
                    Segment fromSegment = currentSegment;
                    Segment toSegment = links[Random.Range(0, links.Count)];

                    //Switch to new spline
                    so.transform.parent = toSegment.SplineParent.transform;
                    so.localSplinePosition.z = spline.CalculateLinkCrossingZPosition(so.localSplinePosition, fromSegment, toSegment);

                    //Update the spline so the code below is correct.
                    spline = toSegment.SplineParent;
                }

                //Reset position to spline start if going beyond the end of the spline.
                if (so.localSplinePosition.z > spline.Length)
                    so.localSplinePosition.z = 0;
            }
        }
    }
}
