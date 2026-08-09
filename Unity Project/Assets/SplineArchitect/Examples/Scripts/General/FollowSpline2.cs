using System.Collections.Generic;
using System;

using UnityEngine;
using Random = UnityEngine.Random;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/General/FollowSpline2")]
    public class FollowSpline2 : MonoBehaviour
    {
        [Serializable]
        public struct ObjectData
        {
            public GameObject prefab;
            public bool deform;
        }

        public List<ObjectData> prefabs = new List<ObjectData>();
        public int amount = 0;
        public Vector3 minSpeed;
        public Vector3 maxSpeed;
        public Vector3 minScale;
        public Vector3 maxScale;

        private Spline spline;
        private List<(SplineObject, Vector3)> splineObjects = new List<(SplineObject, Vector3)>();

        void Start()
        {
            spline = GetComponent<Spline>();

            for(int i = 0; i < amount; i++)
            {
                int index = Random.Range(0, prefabs.Count);

                //Dont set the parent transform here. That's done in spline.CreateFollower.
                GameObject go = Instantiate(prefabs[index].prefab, transform.position, Quaternion.identity);
                SplineObject so;

                if (prefabs[index].deform)
                    so = spline.CreateDeformation(go, Vector3.zero, Quaternion.identity);
                else
                    so = spline.CreateFollower(go, Vector3.zero, Quaternion.identity);

                float speedX = Random.Range(minSpeed.x, maxSpeed.x);
                float speedY = Random.Range(minSpeed.y, maxSpeed.y);
                float speedZ = Random.Range(minSpeed.z, maxSpeed.z);
                splineObjects.Add((so, new Vector3(speedX, speedY, speedZ)));

                float scaleX = Random.Range(minScale.x, maxScale.x);
                float scaleY = Random.Range(minScale.y, maxScale.y);
                float scaleZ = Random.Range(minScale.z, maxScale.z);
                go.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }
        }

        void Update()
        {
            foreach ((SplineObject, Vector3) tuple in splineObjects)
            {
                tuple.Item1.localSplinePosition += tuple.Item2 * Time.deltaTime;

                if(tuple.Item1.localSplinePosition.z > spline.Length)
                    tuple.Item1.localSplinePosition = Vector3.zero;
            }
        }
    }
}
