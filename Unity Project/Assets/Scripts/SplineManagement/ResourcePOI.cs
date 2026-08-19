using UnityEngine;

public class ResourcePOI : MonoBehaviour
{
    [Header("POI Info")]
    [SerializeField] private string poiName = "Scrap Field";

    [SerializeField] private int resourceAmount = 500;

    [Header("Stopping")]
    [SerializeField] private Transform stopPoint;

    private bool discovered;
    private bool depleted;

    public string POIName => poiName;
    public int ResourceAmount => resourceAmount;
    public Transform StopPoint => stopPoint;

    public bool Discovered => discovered;
    public bool Depleted => depleted;

    public void Discover()
    {
        discovered = true;
    }

    public int Gather(int amount)
    {
        if (depleted)
            return 0;

        int gatheredAmount = Mathf.Min(amount, resourceAmount);

        resourceAmount -= gatheredAmount;

        if (resourceAmount <= 0)
        {
            resourceAmount = 0;
            depleted = true;
        }

        return gatheredAmount;
    }
}