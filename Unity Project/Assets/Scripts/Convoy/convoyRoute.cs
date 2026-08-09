using UnityEngine;
using UnityEngine.Splines;

public class convoyRoute : MonoBehaviour
{
    [SerializeField] private string routeName = "Route";
    [SerializeField] private SplineContainer spline;

    public string RouteName => routeName;
    public SplineContainer Spline => spline;

    private void Reset()
    {
        spline = GetComponent<SplineContainer>();
    }
}