using UnityEngine;

namespace SplineArchitect.Examples
{
    [ExecuteAlways]
    [AddComponentMenu("SplineArchitect/Examples/TrafficSystem/TrailerRotation")]
    public class TrailerRotation : MonoBehaviour
    {
        public Transform trailerPoint;

        void Update()
        {
    #if UNITY_EDITOR
            SplineObject so = GetComponent<SplineObject>();
            if (so != null && transform != null)
            {
    #endif
                Vector3 newPoint = new Vector3(trailerPoint.position.x, transform.position.y, trailerPoint.position.z);
                transform.rotation = Quaternion.LookRotation(newPoint - transform.position, Vector3.up);

    #if UNITY_EDITOR
            }
    #endif
        }
    }
}
