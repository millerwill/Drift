using UnityEngine;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("")]
    public class Visualizer : MonoBehaviour
    {
        private void Awake()
        {
            hideFlags = HideFlags.DontSaveInBuild;
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position - transform.forward * 3, transform.position + transform.forward * 3);
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
}
