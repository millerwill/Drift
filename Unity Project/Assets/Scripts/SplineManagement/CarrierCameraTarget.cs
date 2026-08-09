using UnityEngine;

public class CarrierCameraTarget : MonoBehaviour
{
    [Tooltip(
        "Optional point that the camera will focus on. " +
        "If empty, this GameObject's transform is used."
    )]
    [SerializeField] private Transform focusPoint;

    public Transform FocusPoint
    {
        get
        {
            if (focusPoint != null)
                return focusPoint;

            return transform;
        }
    }
}