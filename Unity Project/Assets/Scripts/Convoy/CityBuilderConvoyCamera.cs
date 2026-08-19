using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Camera))]
public class CityBuilderConvoyCamera : MonoBehaviour
{
    [Header("Convoy")]
    [SerializeField] private ConvoySplineController convoy;

    [Header("Starting View")]
    [SerializeField] private bool forcePerspective = true;

    [SerializeField, Range(0f, 360f)]
    private float startingYaw = 45f;

    [SerializeField, Range(15f, 85f)]
    private float startingPitch = 50f;

    [SerializeField, Min(1f)]
    private float startingDistance = 35f;

    [SerializeField]
    private float focusHeight = 0f;

    [Header("Keyboard and Edge Movement")]
    [SerializeField, Min(0f)]
    private float panSpeed = 18f;

    [SerializeField, Min(1f)]
    private float shiftPanMultiplier = 2.5f;

    [SerializeField]
    private bool enableEdgePanning = true;

    [SerializeField, Min(1f)]
    private float edgeSize = 12f;

    [SerializeField, Min(0f)]
    private float edgePanSpeed = 18f;

    [SerializeField]
    private bool scalePanSpeedWithZoom = true;

    [Header("Mouse Panning")]
    [Tooltip("Middle-mouse drag sensitivity.")]
    [SerializeField, Min(0f)]
    private float mousePanSensitivity = 0.0025f;

    [Header("Rotation")]
    [Tooltip("Right-mouse horizontal rotation sensitivity.")]
    [SerializeField, Min(0f)]
    private float yawSensitivity = 0.2f;

    [Tooltip("Right-mouse vertical tilt sensitivity.")]
    [SerializeField, Min(0f)]
    private float pitchSensitivity = 0.15f;

    [SerializeField, Range(10f, 85f)]
    private float minimumPitch = 30f;

    [SerializeField, Range(10f, 85f)]
    private float maximumPitch = 70f;

    [Header("Zoom")]
    [SerializeField, Min(1f)]
    private float minimumDistance = 8f;

    [SerializeField, Min(1f)]
    private float maximumDistance = 80f;

    [SerializeField, Min(0f)]
    private float zoomSpeed = 6f;

    [Tooltip(
        "Moves the camera toward the point beneath the mouse while zooming in."
    )]
    [SerializeField]
    private bool zoomTowardCursor = true;

    [SerializeField, Range(0f, 1f)]
    private float cursorZoomInfluence = 0.65f;

    [Header("Ground Detection")]
    [Tooltip(
        "Use a dedicated Ground or Terrain layer here."
    )]
    [SerializeField]
    private LayerMask groundLayerMask;

    [SerializeField, Min(1f)]
    private float groundRayDistance = 2000f;

    [Tooltip(
        "Used as a fallback when the mouse ray does not hit terrain."
    )]
    [SerializeField]
    private float fallbackGroundHeight = 0f;

    [Header("Dynamic Convoy Movement Limit")]
    [Tooltip(
        "Keeps the camera near the convoy while still allowing free movement."
    )]
    [SerializeField]
    private bool limitMovementToConvoy = true;

    [SerializeField, Min(0f)]
    private float convoyLengthMultiplier = 0.75f;

    [SerializeField, Min(0f)]
    private float movementLimitPadding = 10f;

    [SerializeField, Min(0f)]
    private float minimumMovementRadius = 12f;

    [Header("Carrier Focus")]
    [SerializeField]
    private LayerMask carrierLayerMask;

    [SerializeField, Min(0.05f)]
    private float doubleClickTime = 0.3f;

    [SerializeField, Min(1f)]
    private float selectionRayDistance = 2000f;

    [SerializeField, Min(1f)]
    private float focusedCarrierDistance = 15f;

    [Header("Smoothing")]
    [SerializeField, Min(0.001f)]
    private float positionSmoothTime = 0.12f;

    [SerializeField, Min(0.001f)]
    private float rotationSmoothTime = 0.08f;

    [SerializeField, Min(0.001f)]
    private float zoomSmoothTime = 0.1f;

    private Camera controlledCamera;

    public CarrierCameraTarget focusedCarrier;
    public CarrierCameraTarget lastClickedCarrier;

    // Offset from the convoy's center while in overview mode.
    private Vector3 overviewPanOffset;

    private Vector3 currentPivot;
    private Vector3 pivotVelocity;

    private float targetYaw;
    private float currentYaw;
    private float yawVelocity;

    private float targetPitch;
    private float currentPitch;
    private float pitchVelocity;

    private float targetDistance;
    private float currentDistance;
    private float distanceVelocity;

    private float lastClickTime = -100f;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();

        if (forcePerspective)
            controlledCamera.orthographic = false;

        minimumPitch = Mathf.Clamp(
            minimumPitch,
            1f,
            89f
        );

        maximumPitch = Mathf.Clamp(
            maximumPitch,
            minimumPitch,
            89f
        );

        startingPitch = Mathf.Clamp(
            startingPitch,
            minimumPitch,
            maximumPitch
        );

        minimumDistance = Mathf.Max(
            1f,
            minimumDistance
        );

        maximumDistance = Mathf.Max(
            minimumDistance,
            maximumDistance
        );

        startingDistance = Mathf.Clamp(
            startingDistance,
            minimumDistance,
            maximumDistance
        );

        targetYaw = startingYaw;
        currentYaw = startingYaw;

        targetPitch = startingPitch;
        currentPitch = startingPitch;

        targetDistance = startingDistance;
        currentDistance = startingDistance;
    }

    private void Start()
    {
        currentPivot = GetDesiredPivot();

        ApplyCameraPositionImmediately();
    }

    private void LateUpdate()
    {
        float deltaTime = Time.unscaledDeltaTime;

        if (deltaTime <= 0f)
            return;

        HandleCarrierSelection();
        HandleReturnToOverview();

        HandleRotation();
        HandlePanning(deltaTime);
        HandleZoom();

        ClampOverviewPanOffset();
        UpdateCamera(deltaTime);
    }

    private void HandlePanning(float deltaTime)
    {
        Vector2 keyboardInput = GetKeyboardPanInput();
        Vector2 edgeInput = Vector2.zero;

        bool draggingMouse =
            IsMiddleMouseHeld() ||
            IsRightMouseHeld();

        if (enableEdgePanning &&
            Application.isFocused &&
            !draggingMouse &&
            !IsPointerOverUI())
        {
            edgeInput = GetEdgePanInput();
        }

        Vector3 groundForward;
        Vector3 groundRight;

        GetGroundDirections(
            out groundForward,
            out groundRight
        );

        if (keyboardInput.sqrMagnitude > 0.001f)
        {
            BreakCarrierFocusForManualMovement();

            keyboardInput = Vector2.ClampMagnitude(
                keyboardInput,
                1f
            );

            Vector3 direction =
                groundRight * keyboardInput.x +
                groundForward * keyboardInput.y;

            float keyboardSpeed =
                GetScaledPanSpeed(panSpeed);

            if (IsShiftHeld())
                keyboardSpeed *= shiftPanMultiplier;

            overviewPanOffset +=
                direction.normalized *
                keyboardSpeed *
                deltaTime;
        }

        if (edgeInput.sqrMagnitude > 0.001f)
        {
            BreakCarrierFocusForManualMovement();

            edgeInput = Vector2.ClampMagnitude(
                edgeInput,
                1f
            );

            Vector3 direction =
                groundRight * edgeInput.x +
                groundForward * edgeInput.y;

            overviewPanOffset +=
                direction.normalized *
                GetScaledPanSpeed(edgePanSpeed) *
                deltaTime;
        }

        if (IsMiddleMouseHeld() &&
            !IsPointerOverUI())
        {
            Vector2 mouseDelta = GetMouseDelta();

            if (mouseDelta.sqrMagnitude > 0.001f)
            {
                BreakCarrierFocusForManualMovement();

                Vector3 movement =
                    -groundRight * mouseDelta.x -
                    groundForward * mouseDelta.y;

                overviewPanOffset +=
                    movement *
                    mousePanSensitivity *
                    currentDistance;
            }
        }
    }

    private void HandleRotation()
    {
        if (!IsRightMouseHeld() ||
            IsPointerOverUI())
        {
            return;
        }

        Vector2 mouseDelta = GetMouseDelta();

        targetYaw +=
            mouseDelta.x *
            yawSensitivity;

        targetPitch -=
            mouseDelta.y *
            pitchSensitivity;

        targetPitch = Mathf.Clamp(
            targetPitch,
            minimumPitch,
            maximumPitch
        );
    }

    private void HandleZoom()
    {
        float scrollInput = GetScrollInput();

        if (Mathf.Abs(scrollInput) < 0.001f)
            return;

        float previousDistance = targetDistance;

        targetDistance -=
            scrollInput *
            zoomSpeed;

        targetDistance = Mathf.Clamp(
            targetDistance,
            minimumDistance,
            maximumDistance
        );

        bool zoomingIn =
            targetDistance < previousDistance;

        if (!zoomTowardCursor ||
            !zoomingIn ||
            focusedCarrier != null ||
            IsPointerOverUI())
        {
            return;
        }

        if (!TryGetGroundPoint(
                GetMousePosition(),
                out Vector3 groundPoint))
        {
            return;
        }

        Vector3 currentOverviewPivot =
            GetConvoyCenter() +
            overviewPanOffset;

        Vector3 towardCursor =
            groundPoint -
            currentOverviewPivot;

        towardCursor.y = 0f;

        float zoomPercentage =
            Mathf.Abs(
                previousDistance -
                targetDistance
            ) /
            Mathf.Max(previousDistance, 0.01f);

        float influence = Mathf.Clamp01(
            zoomPercentage *
            2f *
            cursorZoomInfluence
        );

        overviewPanOffset +=
            towardCursor *
            influence;
    }

    private void HandleCarrierSelection()
    {
        if (!WasLeftMousePressedThisFrame())
            return;

        if (IsPointerOverUI())
            return;

        Ray ray = controlledCamera.ScreenPointToRay(
            GetMousePosition()
        );

        if (!Physics.Raycast(
                ray,
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
            hit.collider.GetComponentInParent
                <CarrierCameraTarget>();

        if (clickedCarrier == null)
        {
            lastClickedCarrier = null;
            lastClickTime = -100f;
            return;
        }

        float currentTime = Time.unscaledTime;

        bool doubleClicked =
            clickedCarrier == lastClickedCarrier &&
            currentTime - lastClickTime <=
            doubleClickTime;

        if (doubleClicked)
        {
            FocusCarrier(clickedCarrier);
            //Enable module selection/swapping

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

    public void FocusCarrier(
        CarrierCameraTarget carrier)
    {
        if (carrier == null)
            return;

        focusedCarrier = carrier;

        targetDistance = Mathf.Clamp(
            focusedCarrierDistance,
            minimumDistance,
            maximumDistance
        );
    }

    public void ReturnToConvoyOverview()
    {
        focusedCarrier = null;
        overviewPanOffset = Vector3.zero;
    }

    public void RecenterOnConvoy()
    {
        focusedCarrier = null;
        overviewPanOffset = Vector3.zero;
    }

    private void BreakCarrierFocusForManualMovement()
    {
        if (focusedCarrier == null)
            return;

        // Preserve approximately the same screen location when
        // switching from carrier focus to overview movement.
        overviewPanOffset =
            currentPivot -
            GetConvoyCenter();

        overviewPanOffset.y = 0f;

        focusedCarrier = null;
    }

    private void ClampOverviewPanOffset()
    {
        overviewPanOffset.y = 0f;

        if (!limitMovementToConvoy ||
            convoy == null)
        {
            return;
        }

        float maximumRadius = Mathf.Max(
            minimumMovementRadius,
            convoy.ConvoyLength *
            convoyLengthMultiplier +
            movementLimitPadding
        );

        Vector2 planarOffset = new Vector2(
            overviewPanOffset.x,
            overviewPanOffset.z
        );

        planarOffset = Vector2.ClampMagnitude(
            planarOffset,
            maximumRadius
        );

        overviewPanOffset.x = planarOffset.x;
        overviewPanOffset.z = planarOffset.y;
    }

    private void UpdateCamera(float deltaTime)
    {
        currentYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref yawVelocity,
            rotationSmoothTime,
            Mathf.Infinity,
            deltaTime
        );

        currentPitch = Mathf.SmoothDampAngle(
            currentPitch,
            targetPitch,
            ref pitchVelocity,
            rotationSmoothTime,
            Mathf.Infinity,
            deltaTime
        );

        currentDistance = Mathf.SmoothDamp(
            currentDistance,
            targetDistance,
            ref distanceVelocity,
            zoomSmoothTime,
            Mathf.Infinity,
            deltaTime
        );

        Vector3 desiredPivot = GetDesiredPivot();

        currentPivot = Vector3.SmoothDamp(
            currentPivot,
            desiredPivot,
            ref pivotVelocity,
            positionSmoothTime,
            Mathf.Infinity,
            deltaTime
        );

        Quaternion cameraRotation =
            Quaternion.Euler(
                currentPitch,
                currentYaw,
                0f
            );

        Vector3 cameraPosition =
            currentPivot -
            cameraRotation *
            Vector3.forward *
            currentDistance;

        transform.SetPositionAndRotation(
            cameraPosition,
            cameraRotation
        );
    }

    private void ApplyCameraPositionImmediately()
    {
        Quaternion cameraRotation =
            Quaternion.Euler(
                currentPitch,
                currentYaw,
                0f
            );

        Vector3 cameraPosition =
            currentPivot -
            cameraRotation *
            Vector3.forward *
            currentDistance;

        transform.SetPositionAndRotation(
            cameraPosition,
            cameraRotation
        );
    }

    private Vector3 GetDesiredPivot()
    {
        Vector3 pivot;

        if (focusedCarrier != null &&
            focusedCarrier.FocusPoint != null)
        {
            pivot =
                focusedCarrier.FocusPoint.position;
        }
        else
        {
            pivot =
                GetConvoyCenter() +
                overviewPanOffset;
        }

        pivot.y += focusHeight;

        return pivot;
    }

    private Vector3 GetConvoyCenter()
    {
        if (convoy != null)
            return convoy.GetConvoyCenter();

        return Vector3.zero;
    }

    private void GetGroundDirections(
        out Vector3 forward,
        out Vector3 right)
    {
        Quaternion yawRotation =
            Quaternion.Euler(
                0f,
                currentYaw,
                0f
            );

        forward =
            yawRotation *
            Vector3.forward;

        right =
            yawRotation *
            Vector3.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();
    }

    private float GetScaledPanSpeed(
        float baseSpeed)
    {
        if (!scalePanSpeedWithZoom)
            return baseSpeed;

        float zoomPercentage =
            Mathf.InverseLerp(
                minimumDistance,
                maximumDistance,
                currentDistance
            );

        return baseSpeed *
            Mathf.Lerp(
                0.6f,
                2f,
                zoomPercentage
            );
    }

    private bool TryGetGroundPoint(
        Vector2 screenPosition,
        out Vector3 point)
    {
        Ray ray = controlledCamera.ScreenPointToRay(
            screenPosition
        );

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                groundRayDistance,
                groundLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }

        Plane fallbackPlane = new Plane(
            Vector3.up,
            new Vector3(
                0f,
                fallbackGroundHeight,
                0f
            )
        );

        if (fallbackPlane.Raycast(
                ray,
                out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    private Vector2 GetEdgePanInput()
    {
        Vector2 mousePosition =
            GetMousePosition();

        Vector2 input = Vector2.zero;

        if (mousePosition.x <= edgeSize)
            input.x -= 1f;
        else if (
            mousePosition.x >=
            Screen.width - edgeSize)
            input.x += 1f;

        if (mousePosition.y <= edgeSize)
            input.y -= 1f;
        else if (
            mousePosition.y >=
            Screen.height - edgeSize)
            input.y += 1f;

        return input;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current
                   .IsPointerOverGameObject();
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
            Input.GetAxisRaw("Mouse Y")
        );

#else
        return Vector2.zero;
#endif
    }

    private float GetScrollInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
            return 0f;

        return Mouse.current.scroll.ReadValue().y /
               120f;

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
               Mouse.current.leftButton
                   .wasPressedThisFrame;

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
               Keyboard.current.escapeKey
                   .wasPressedThisFrame;

#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(
            KeyCode.Escape
        );

#else
        return false;
#endif
    }

    private bool IsShiftHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
               (keyboard.leftShiftKey.isPressed ||
                keyboard.rightShiftKey.isPressed);

#elif ENABLE_LEGACY_INPUT_MANAGER
    return Input.GetKey(KeyCode.LeftShift) ||
           Input.GetKey(KeyCode.RightShift);

#else
    return false;
#endif
    }
}