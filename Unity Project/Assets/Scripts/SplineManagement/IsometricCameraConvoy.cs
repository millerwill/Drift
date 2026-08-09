using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Camera))]
public class IsometricConvoyCamera : MonoBehaviour
{
    [Header("Convoy")]
    [SerializeField] private ConvoySplineController convoy;

    [Header("Isometric View")]
    [Tooltip("Automatically changes the Camera component to Orthographic.")]
    [SerializeField] private bool forceOrthographic = true;

    [Tooltip(
        "Keeps the camera at a fixed isometric pitch. " +
        "The player can still rotate horizontally."
    )]
    [SerializeField] private bool lockIsometricPitch = true;

    [SerializeField, Range(20f, 80f)]
    private float isometricPitch = 50f;

    [SerializeField] private float startingYaw = 45f;

    [SerializeField, Range(20f, 80f)]
    private float minimumPitch = 35f;

    [SerializeField, Range(20f, 80f)]
    private float maximumPitch = 70f;

    [Tooltip(
        "Physical distance between an orthographic camera and its focus point. " +
        "This does not control zoom."
    )]
    [SerializeField, Min(1f)]
    private float orthographicStandOffDistance = 60f;

    [Header("WASD and Edge Panning")]
    [SerializeField, Min(0f)]
    private float keyboardPanSpeed = 14f;

    [SerializeField] private bool enableEdgePanning = true;

    [SerializeField, Min(0f)]
    private float edgePanSpeed = 14f;

    [Tooltip("Distance from the screen edge that activates edge panning.")]
    [SerializeField, Min(1f)]
    private float edgeSizeInPixels = 15f;

    [Header("Mouse Panning")]
    [Tooltip("Middle-mouse drag sensitivity.")]
    [SerializeField, Min(0f)]
    private float mousePanSensitivity = 0.025f;

    [Header("Dynamic Pan Limit")]
    [Tooltip(
        "The camera can pan approximately half of the convoy's total length " +
        "away from its center."
    )]
    [SerializeField, Min(0f)]
    private float convoyLengthMultiplier = 0.5f;

    [SerializeField, Min(0f)]
    private float panPadding = 5f;

    [SerializeField, Min(0f)]
    private float minimumPanRadius = 5f;

    [Tooltip(
        "Maximum distance the camera can pan away from an individually " +
        "focused carrier."
    )]
    [SerializeField, Min(0f)]
    private float focusedCarrierPanRadius = 3f;

    [Header("Rotation")]
    [Tooltip("Right-mouse drag sensitivity.")]
    [SerializeField, Min(0f)]
    private float rotationSensitivity = 0.18f;

    [Header("Orthographic Zoom")]
    [SerializeField, Min(0.1f)]
    private float startingOrthographicSize = 12f;

    [SerializeField, Min(0.1f)]
    private float minimumOrthographicSize = 4f;

    [SerializeField, Min(0.1f)]
    private float maximumOrthographicSize = 30f;

    [SerializeField, Min(0f)]
    private float orthographicZoomStep = 1.5f;

    [Tooltip(
        "Orthographic size used after double-clicking a carrier."
    )]
    [SerializeField, Min(0.1f)]
    private float focusedOrthographicSize = 6f;

    [Header("Perspective Zoom")]
    [Tooltip(
        "These settings are only used when the camera is not Orthographic."
    )]
    [SerializeField, Min(0.1f)]
    private float startingPerspectiveDistance = 25f;

    [SerializeField, Min(0.1f)]
    private float minimumPerspectiveDistance = 8f;

    [SerializeField, Min(0.1f)]
    private float maximumPerspectiveDistance = 50f;

    [SerializeField, Min(0f)]
    private float perspectiveZoomStep = 2f;

    [SerializeField, Min(0.1f)]
    private float focusedPerspectiveDistance = 12f;

    [Header("Carrier Selection")]
    [SerializeField] private LayerMask carrierLayerMask = ~0;

    [SerializeField, Min(0.1f)]
    private float doubleClickTime = 0.3f;

    [SerializeField, Min(1f)]
    private float selectionRayDistance = 1000f;

    [Header("Focus")]
    [Tooltip(
        "Raises the camera's look point above the convoy's calculated center."
    )]
    [SerializeField] private float focusHeight = 0f;

    [Header("Smoothing")]
    [SerializeField, Min(0.001f)]
    private float positionSmoothTime = 0.15f;

    [SerializeField, Min(0.001f)]
    private float rotationSmoothTime = 0.08f;

    [SerializeField, Min(0.001f)]
    private float zoomSmoothTime = 0.12f;

    [SerializeField]
    private bool useUnscaledTime = false;

    private Camera controlledCamera;

    private CarrierCameraTarget focusedCarrier;
    private CarrierCameraTarget lastClickedCarrier;

    private Vector3 panOffset;
    private Vector3 smoothedFocusPosition;
    private Vector3 focusVelocity;

    private float targetYaw;
    private float currentYaw;
    private float yawVelocity;

    private float targetPitch;
    private float currentPitch;
    private float pitchVelocity;

    private float targetOrthographicSize;
    private float currentOrthographicSize;
    private float orthographicZoomVelocity;

    private float targetPerspectiveDistance;
    private float currentPerspectiveDistance;
    private float perspectiveZoomVelocity;

    private float storedOverviewOrthographicSize;
    private float storedOverviewPerspectiveDistance;

    private float lastClickTime = -100f;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();

        if (forceOrthographic)
            controlledCamera.orthographic = true;

        ValidateValues();

        targetYaw = startingYaw;
        currentYaw = startingYaw;

        targetPitch = isometricPitch;
        currentPitch = isometricPitch;

        targetOrthographicSize = startingOrthographicSize;
        currentOrthographicSize = startingOrthographicSize;

        targetPerspectiveDistance = startingPerspectiveDistance;
        currentPerspectiveDistance = startingPerspectiveDistance;

        storedOverviewOrthographicSize = startingOrthographicSize;
        storedOverviewPerspectiveDistance = startingPerspectiveDistance;

        if (controlledCamera.orthographic)
            controlledCamera.orthographicSize = currentOrthographicSize;
    }

    private void Start()
    {
        smoothedFocusPosition =
            GetBaseFocusPosition() + Vector3.up * focusHeight;

        ApplyCameraTransformImmediately();
    }

    private void OnValidate()
    {
        ValidateValues();
    }

    private void LateUpdate()
    {
        float deltaTime = useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        if (deltaTime <= 0f)
            return;

        HandleCarrierSelection();
        HandleReturnToOverview();
        HandleRotationInput();
        HandlePanInput(deltaTime);
        HandleZoomInput();

        ClampPanOffset();
        UpdateCamera(deltaTime);
    }

    private void ValidateValues()
    {
        minimumPitch = Mathf.Clamp(minimumPitch, 1f, 89f);
        maximumPitch = Mathf.Clamp(maximumPitch, minimumPitch, 89f);
        isometricPitch = Mathf.Clamp(
            isometricPitch,
            minimumPitch,
            maximumPitch);

        minimumOrthographicSize =
            Mathf.Max(0.1f, minimumOrthographicSize);

        maximumOrthographicSize =
            Mathf.Max(minimumOrthographicSize, maximumOrthographicSize);

        startingOrthographicSize = Mathf.Clamp(
            startingOrthographicSize,
            minimumOrthographicSize,
            maximumOrthographicSize);

        focusedOrthographicSize = Mathf.Clamp(
            focusedOrthographicSize,
            minimumOrthographicSize,
            maximumOrthographicSize);

        minimumPerspectiveDistance =
            Mathf.Max(0.1f, minimumPerspectiveDistance);

        maximumPerspectiveDistance = Mathf.Max(
            minimumPerspectiveDistance,
            maximumPerspectiveDistance);

        startingPerspectiveDistance = Mathf.Clamp(
            startingPerspectiveDistance,
            minimumPerspectiveDistance,
            maximumPerspectiveDistance);

        focusedPerspectiveDistance = Mathf.Clamp(
            focusedPerspectiveDistance,
            minimumPerspectiveDistance,
            maximumPerspectiveDistance);
    }

    private void HandlePanInput(float deltaTime)
    {
        Vector2 directionalInput = GetKeyboardPanInput();

        if (enableEdgePanning &&
            Application.isFocused &&
            !IsMiddleMouseHeld() &&
            !IsRightMouseHeld() &&
            !IsPointerOverUI())
        {
            directionalInput += GetEdgePanInput();
        }

        directionalInput = Vector2.ClampMagnitude(
            directionalInput,
            1f);

        Vector3 planarForward;
        Vector3 planarRight;

        GetPlanarCameraDirections(
            out planarForward,
            out planarRight);

        if (directionalInput.sqrMagnitude > 0.001f)
        {
            Vector3 movementDirection =
                planarRight * directionalInput.x +
                planarForward * directionalInput.y;

            float movementSpeed = keyboardPanSpeed;

            if (GetEdgePanInput().sqrMagnitude > 0.001f)
                movementSpeed = edgePanSpeed;

            panOffset += movementDirection.normalized *
                         movementSpeed *
                         GetZoomMovementMultiplier() *
                         deltaTime;
        }

        if (IsMiddleMouseHeld() && !IsPointerOverUI())
        {
            Vector2 mouseDelta = GetMouseDelta();

            Vector3 dragMovement =
                -planarRight * mouseDelta.x -
                planarForward * mouseDelta.y;

            panOffset += dragMovement *
                         mousePanSensitivity *
                         GetZoomMovementMultiplier();
        }
    }

    private void HandleRotationInput()
    {
        if (!IsRightMouseHeld() || IsPointerOverUI())
            return;

        Vector2 mouseDelta = GetMouseDelta();

        targetYaw += mouseDelta.x * rotationSensitivity;

        if (lockIsometricPitch)
        {
            targetPitch = isometricPitch;
        }
        else
        {
            targetPitch -= mouseDelta.y * rotationSensitivity;

            targetPitch = Mathf.Clamp(
                targetPitch,
                minimumPitch,
                maximumPitch);
        }
    }

    private void HandleZoomInput()
    {
        float scrollInput = GetScrollInput();

        if (Mathf.Abs(scrollInput) < 0.001f)
            return;

        if (controlledCamera.orthographic)
        {
            targetOrthographicSize -=
                scrollInput * orthographicZoomStep;

            targetOrthographicSize = Mathf.Clamp(
                targetOrthographicSize,
                minimumOrthographicSize,
                maximumOrthographicSize);

            if (focusedCarrier == null)
            {
                storedOverviewOrthographicSize =
                    targetOrthographicSize;
            }
        }
        else
        {
            targetPerspectiveDistance -=
                scrollInput * perspectiveZoomStep;

            targetPerspectiveDistance = Mathf.Clamp(
                targetPerspectiveDistance,
                minimumPerspectiveDistance,
                maximumPerspectiveDistance);

            if (focusedCarrier == null)
            {
                storedOverviewPerspectiveDistance =
                    targetPerspectiveDistance;
            }
        }
    }

    private void HandleCarrierSelection()
    {
        if (!WasLeftMousePressedThisFrame())
            return;

        if (IsPointerOverUI())
            return;

        Vector2 pointerPosition = GetMousePosition();

        Ray selectionRay =
            controlledCamera.ScreenPointToRay(pointerPosition);

        if (!Physics.Raycast(
                selectionRay,
                out RaycastHit hit,
                selectionRayDistance,
                carrierLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            lastClickedCarrier = null;
            lastClickTime = -100f;
            return;
        }

        CarrierCameraTarget clickedCarrier =
            hit.collider.GetComponentInParent<CarrierCameraTarget>();

        if (clickedCarrier == null)
        {
            lastClickedCarrier = null;
            lastClickTime = -100f;
            return;
        }

        float currentTime = Time.unscaledTime;

        bool isDoubleClick =
            clickedCarrier == lastClickedCarrier &&
            currentTime - lastClickTime <= doubleClickTime;

        if (isDoubleClick)
        {
            FocusCarrier(clickedCarrier);

            lastClickedCarrier = null;
            lastClickTime = -100f;
        }
        else
        {
            lastClickedCarrier = clickedCarrier;
            lastClickTime = currentTime;
        }
    }

    private void HandleReturnToOverview()
    {
        if (!WasEscapePressedThisFrame())
            return;

        ReturnToConvoyOverview();
    }

    public void FocusCarrier(CarrierCameraTarget carrier)
    {
        if (carrier == null)
            return;

        if (focusedCarrier == null)
        {
            storedOverviewOrthographicSize =
                targetOrthographicSize;

            storedOverviewPerspectiveDistance =
                targetPerspectiveDistance;
        }

        focusedCarrier = carrier;
        panOffset = Vector3.zero;

        if (controlledCamera.orthographic)
        {
            targetOrthographicSize =
                focusedOrthographicSize;
        }
        else
        {
            targetPerspectiveDistance =
                focusedPerspectiveDistance;
        }
    }

    public void ReturnToConvoyOverview()
    {
        focusedCarrier = null;
        panOffset = Vector3.zero;

        targetOrthographicSize = Mathf.Clamp(
            storedOverviewOrthographicSize,
            minimumOrthographicSize,
            maximumOrthographicSize);

        targetPerspectiveDistance = Mathf.Clamp(
            storedOverviewPerspectiveDistance,
            minimumPerspectiveDistance,
            maximumPerspectiveDistance);
    }

    public void RecenterCamera()
    {
        panOffset = Vector3.zero;
    }

    private void ClampPanOffset()
    {
        panOffset.y = 0f;

        float maximumPanDistance;

        if (focusedCarrier != null)
        {
            maximumPanDistance = focusedCarrierPanRadius;
        }
        else
        {
            float convoyLength = convoy != null
                ? convoy.ConvoyLength
                : 0f;

            maximumPanDistance = Mathf.Max(
                minimumPanRadius,
                convoyLength * convoyLengthMultiplier +
                panPadding);
        }

        Vector2 planarOffset =
            new Vector2(panOffset.x, panOffset.z);

        planarOffset = Vector2.ClampMagnitude(
            planarOffset,
            maximumPanDistance);

        panOffset.x = planarOffset.x;
        panOffset.z = planarOffset.y;
    }

    private void UpdateCamera(float deltaTime)
    {
        if (lockIsometricPitch)
            targetPitch = isometricPitch;

        targetPitch = Mathf.Clamp(
            targetPitch,
            minimumPitch,
            maximumPitch);

        currentYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref yawVelocity,
            rotationSmoothTime,
            Mathf.Infinity,
            deltaTime);

        currentPitch = Mathf.SmoothDampAngle(
            currentPitch,
            targetPitch,
            ref pitchVelocity,
            rotationSmoothTime,
            Mathf.Infinity,
            deltaTime);

        Vector3 desiredFocusPosition =
            GetBaseFocusPosition() +
            panOffset +
            Vector3.up * focusHeight;

        smoothedFocusPosition = Vector3.SmoothDamp(
            smoothedFocusPosition,
            desiredFocusPosition,
            ref focusVelocity,
            positionSmoothTime,
            Mathf.Infinity,
            deltaTime);

        float cameraDistance;

        if (controlledCamera.orthographic)
        {
            currentOrthographicSize = Mathf.SmoothDamp(
                currentOrthographicSize,
                targetOrthographicSize,
                ref orthographicZoomVelocity,
                zoomSmoothTime,
                Mathf.Infinity,
                deltaTime);

            controlledCamera.orthographicSize =
                currentOrthographicSize;

            cameraDistance = orthographicStandOffDistance;
        }
        else
        {
            currentPerspectiveDistance = Mathf.SmoothDamp(
                currentPerspectiveDistance,
                targetPerspectiveDistance,
                ref perspectiveZoomVelocity,
                zoomSmoothTime,
                Mathf.Infinity,
                deltaTime);

            cameraDistance = currentPerspectiveDistance;
        }

        Quaternion cameraRotation = Quaternion.Euler(
            currentPitch,
            currentYaw,
            0f);

        Vector3 cameraPosition =
            smoothedFocusPosition -
            cameraRotation * Vector3.forward * cameraDistance;

        transform.SetPositionAndRotation(
            cameraPosition,
            cameraRotation);
    }

    private void ApplyCameraTransformImmediately()
    {
        Quaternion cameraRotation = Quaternion.Euler(
            currentPitch,
            currentYaw,
            0f);

        float cameraDistance = controlledCamera.orthographic
            ? orthographicStandOffDistance
            : currentPerspectiveDistance;

        Vector3 cameraPosition =
            smoothedFocusPosition -
            cameraRotation * Vector3.forward * cameraDistance;

        transform.SetPositionAndRotation(
            cameraPosition,
            cameraRotation);
    }

    private Vector3 GetBaseFocusPosition()
    {
        if (focusedCarrier != null)
        {
            Transform carrierFocusPoint =
                focusedCarrier.FocusPoint;

            if (carrierFocusPoint != null)
                return carrierFocusPoint.position;
        }

        if (convoy != null)
            return convoy.GetConvoyCenter();

        return Vector3.zero;
    }

    private void GetPlanarCameraDirections(
        out Vector3 forward,
        out Vector3 right)
    {
        Quaternion rotation = Quaternion.Euler(
            currentPitch,
            currentYaw,
            0f);

        forward = Vector3.ProjectOnPlane(
            rotation * Vector3.forward,
            Vector3.up);

        right = Vector3.ProjectOnPlane(
            rotation * Vector3.right,
            Vector3.up);

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;

        forward.Normalize();
        right.Normalize();
    }

    private float GetZoomMovementMultiplier()
    {
        if (controlledCamera.orthographic)
        {
            return Mathf.Max(
                0.25f,
                targetOrthographicSize /
                startingOrthographicSize);
        }

        return Mathf.Max(
            0.25f,
            targetPerspectiveDistance /
            startingPerspectiveDistance);
    }

    private Vector2 GetEdgePanInput()
    {
        if (!enableEdgePanning)
            return Vector2.zero;

        Vector2 mousePosition = GetMousePosition();
        Vector2 input = Vector2.zero;

        if (mousePosition.x <= edgeSizeInPixels)
            input.x -= 1f;
        else if (mousePosition.x >=
                 Screen.width - edgeSizeInPixels)
            input.x += 1f;

        if (mousePosition.y <= edgeSizeInPixels)
            input.y -= 1f;
        else if (mousePosition.y >=
                 Screen.height - edgeSizeInPixels)
            input.y += 1f;

        return input;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }

    private Vector2 GetKeyboardPanInput()
    {
        Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return input;

        if (keyboard.aKey.isPressed)
            input.x -= 1f;

        if (keyboard.dKey.isPressed)
            input.x += 1f;

        if (keyboard.sKey.isPressed)
            input.y -= 1f;

        if (keyboard.wKey.isPressed)
            input.y += 1f;

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.A))
            input.x -= 1f;

        if (Input.GetKey(KeyCode.D))
            input.x += 1f;

        if (Input.GetKey(KeyCode.S))
            input.y -= 1f;

        if (Input.GetKey(KeyCode.W))
            input.y += 1f;
#endif

        return input;
    }

    private Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Vector2.zero;

#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;

#else
        return Vector2.zero;
#endif
    }

    private Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.delta.ReadValue();

        return Vector2.zero;

#elif ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y"));

#else
        return Vector2.zero;
#endif
    }

    private float GetScrollInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
            return 0f;

        // On Windows, a single wheel notch is commonly reported as 120.
        return Mouse.current.scroll.ReadValue().y / 120f;

#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.mouseScrollDelta.y;

#else
        return 0f;
#endif
    }

    private bool IsMiddleMouseHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.middleButton.isPressed;

#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(2);

#else
        return false;
#endif
    }

    private bool IsRightMouseHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.rightButton.isPressed;

#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(1);

#else
        return false;
#endif
    }

    private bool WasLeftMousePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;

#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);

#else
        return false;
#endif
    }

    private bool WasEscapePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.escapeKey.wasPressedThisFrame;

#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);

#else
        return false;
#endif
    }
}