using UnityEngine;
using UnityEngine.Splines;

public class convoyCarrier : MonoBehaviour
{
    [SerializeField] private SplineAnimate splineAnimate;
    [SerializeField] private bool isLeadCarrier;

    public bool IsLeadCarrier => isLeadCarrier;

    private void Awake()
    {
        if (splineAnimate == null)
        {
            splineAnimate = GetComponent<SplineAnimate>();
        }
    }

    public void Pause()
    {
        splineAnimate.Pause();
    }

    public void Continue()
    {
        splineAnimate.Play();
    }

    public void ChangeRoute(convoyRoute route)
    {
        if (route == null || route.Spline == null)
        {
            Debug.LogError($"{name}: Invalid route.");
            return;
        }

        splineAnimate.Pause();
        splineAnimate.Container = route.Spline;

        // This intentionally begins at the start of the new spline.
        splineAnimate.Restart(false);
        splineAnimate.Play();
    }
}