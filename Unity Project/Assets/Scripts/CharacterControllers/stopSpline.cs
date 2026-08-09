using UnityEngine;
using UnityEngine.Splines;

public class stopSpline : MonoBehaviour
{
    [SerializeField]
    private ConvoyController convoyController;

    private void OnTriggerEnter(Collider other)
    {
        if (convoyController == null)
            return;

        // Find the spline vehicle associated with this collider.
        SplineAnimate enteringVehicle =
            other.GetComponentInParent<SplineAnimate>();

        if (enteringVehicle == null)
            return;

        // Only react to vehicles belonging to this convoy.
        if (!convoyController.ContainsVehicle(enteringVehicle))
            return;

        convoyController.StopConvoy();
    }
}