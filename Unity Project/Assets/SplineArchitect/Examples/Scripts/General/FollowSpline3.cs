using System.Collections.Generic;

using UnityEngine;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/General/FollowSpline3")]
    public class FollowSpline3 : MonoBehaviour
    {
        [Header("Settings")]
        public GameObject prefab;
        public float speed;
        public float spawnDelay;
        public int maxSpawns;

        private Spline spline;
        private List<SplineObject> splineObjects = new List<SplineObject>();
        private float spawnTimer;

        void Start()
        {
            spline = GetComponent<Spline>();
        }

        void Update()
        {
            spawnTimer += Time.deltaTime;

            if (spawnTimer >= spawnDelay)
            {
                spawnTimer = 0f;
                if (splineObjects.Count < maxSpawns)
                {
                    CreateFollower();
                }
            }

            foreach(SplineObject so in splineObjects)
            {
                so.localSplinePosition.z += Time.deltaTime * speed;

                if (so.localSplinePosition.z > spline.Length)
                {
                    so.localSplinePosition.z = 0f;
                }
            }
        }

        private void CreateFollower()
        {
            GameObject go = Instantiate(prefab);
            SplineObject so = spline.CreateFollower(go, Vector3.zero, Quaternion.identity);
            splineObjects.Add(so);
        }
    }
}
