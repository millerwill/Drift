using UnityEngine;

public class convoyRouteManager : MonoBehaviour
{
    [SerializeField] private convoyCarrier[] carriers;

    private convoyRoute[] availableRoutes;
    private bool waitingForSelection;

    public void ReachIntersection(convoyRoute[] routes)
    {
        if (routes == null || routes.Length == 0)
        {
            Debug.LogWarning("Intersection has no available routes.");
            return;
        }

        availableRoutes = routes;
        waitingForSelection = true;

        PauseConvoy();

        Debug.Log("Convoy reached an intersection.");

        for (int i = 0; i < availableRoutes.Length; i++)
        {
            Debug.Log($"{i + 1}: {availableRoutes[i].RouteName}");
        }
    }

    public void SelectRoute(int index)
    {
        if (!waitingForSelection)
        {
            return;
        }

        if (index < 0 || index >= availableRoutes.Length)
        {
            Debug.LogWarning("Invalid route index.");
            return;
        }

        convoyRoute selectedRoute = availableRoutes[index];

        waitingForSelection = false;

        foreach (convoyCarrier carrier in carriers)
        {
            if (carrier != null)
            {
                carrier.ChangeRoute(selectedRoute);
            }
        }
    }

    private void PauseConvoy()
    {
        foreach (convoyCarrier carrier in carriers)
        {
            if (carrier != null)
            {
                carrier.Pause();
            }
        }
    }

    private void Update()
    {
        if (!waitingForSelection)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectRoute(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectRoute(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectRoute(2);
        }
    }
}