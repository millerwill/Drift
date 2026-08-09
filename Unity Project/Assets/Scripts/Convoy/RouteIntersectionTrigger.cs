using UnityEngine;

public class RouteIntersectionTrigger : MonoBehaviour
{
    [SerializeField] private convoyRoute[] availableRoutes;

    private bool activated;

    private void OnTriggerEnter(Collider other)
    {
        if (activated)
        {
            return;
        }

        convoyCarrier carrier = other.GetComponentInParent<convoyCarrier>();

        if (carrier == null || !carrier.IsLeadCarrier)
        {
            return;
        }

        convoyRouteManager manager =
            carrier.GetComponentInParent<convoyRouteManager>();

        if (manager == null)
        {
            manager = FindFirstObjectByType<convoyRouteManager>();
        }

        if (manager == null)
        {
            Debug.LogError("No ConvoyRouteManager found.");
            return;
        }

        activated = true;
        manager.ReachIntersection(availableRoutes);
    }
}