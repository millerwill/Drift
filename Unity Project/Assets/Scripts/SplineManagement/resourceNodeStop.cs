using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class resourceNodeStop : MonoBehaviour
{
    public static resourceNodeStop Instance { get; private set; }

    [SerializeField] private ConvoySplineController convoyManager;
    [SerializeField] private ResourcePOI resource;


    private ResourcePOI currentPOI;
    public bool gathering;

    private void Awake()
    {
        Instance = this;
    }

    public void POIDetected(ResourcePOI poi)
    {
        currentPOI = poi;

        Debug.Log(
            $"POI DETECTED: {poi.POIName} | " +
            $"Resources: {poi.ResourceAmount}"
        );

        Debug.Log("Press Y to stop. Press N to continue.");
    }

    private void Update()
    {
        if (currentPOI == null)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.yKey.wasPressedThisFrame)
        {
            gathering = true;
            StopAtPOI();
            
        }

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            IgnorePOI();
        }
    }

    private void StopAtPOI()
    {
        Debug.Log($"Stopping at {currentPOI.POIName}");

        convoyManager.StopConvoy();

        currentPOI = null;
    }

    private void IgnorePOI()
    {
        Debug.Log("Continuing past POI.");

        currentPOI = null;
    }
}