using UnityEngine;

public class POIDetectionTrigger : MonoBehaviour
{
    [SerializeField] private ResourcePOI poi;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        convoyCarrier carrier =
            other.GetComponentInParent<convoyCarrier>();

        if (carrier == null)
            return;

        if (!carrier.IsLeadCarrier)
            return;

        triggered = true;

        poi.Discover();

        resourceNodeStop.Instance.POIDetected(poi);
    }
}