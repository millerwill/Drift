using UnityEngine;
using UnityEngine.InputSystem;

public class ResourceGathering : MonoBehaviour
{
    [SerializeField] private ResourcePOI resourcePOI;
    [SerializeField] private ConvoyResources convoyResources;

    [SerializeField] private int gatherAmount = 10;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            Gather();
        }
    }

    private void Gather()
    {
        if (resourcePOI == null)
            return;

        int gatheredAmount = resourcePOI.Gather(gatherAmount);

        convoyResources.AddScrap(gatheredAmount);

        Debug.Log(
            $"Gathered {gatheredAmount} scrap. " +
            $"{resourcePOI.ResourceAmount} remaining."
        );
    }
}