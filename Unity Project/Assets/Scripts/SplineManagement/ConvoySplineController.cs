using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ConvoySplineController : MonoBehaviour
{
    [Header("Route")]
    [SerializeField] private SplineRouteSegment startingSegment;

    [Header("Convoy")]
    [Tooltip("Place the lead object first, followed by the remaining objects in order.")]
    [SerializeField]
    private List<Transform> convoyMembers =
        new List<Transform>();

    [SerializeField, Min(0f)] private float memberSpacing = 4f;

    [Tooltip("Spreads the convoy along the first spline when the game starts.")]
    [SerializeField] private bool spreadConvoyAtStart = true;


    [Header("Movement")]
    [SerializeField, Min(0f)] private float movementSpeed = 5f;

    [Tooltip("How quickly the convoy reaches full speed.")]
    [SerializeField, Min(0.01f)] private float acceleration = 1.5f;

    [Tooltip("How quickly the convoy slows to a stop.")]
    [SerializeField, Min(0.01f)] private float deceleration = 2f;

    [Header("Movement Control")]
    [SerializeField] private bool movementEnabled = false;

    private float currentSpeed;

    public float CurrentSpeed => currentSpeed;

    public bool MovementEnabled => movementEnabled;

    public bool IsActuallyMoving =>
        currentSpeed > 0.01f &&
        !waitingForChoice &&
        !routeFinished;

    [Tooltip("Higher values make rotation follow the spline more quickly.")]
    [SerializeField, Min(0f)] private float rotationSharpness = 12f;

    private readonly List<RouteStep> activeRoute =
        new List<RouteStep>();

    private float leaderRouteDistance;

    private bool waitingForChoice;
    private bool routeFinished;

    public bool WaitingForChoice => waitingForChoice;
    public bool RouteFinished => routeFinished;

    private class RouteStep
    {
        public SplineRouteSegment Segment;
        public float StartDistance;

        public float EndDistance =>
            StartDistance + Segment.Length;

        public RouteStep(
            SplineRouteSegment segment,
            float startDistance)
        {
            Segment = segment;
            StartDistance = startDistance;
        }
    }

    private void Start()
    {
        convoyMembers.RemoveAll(member => member == null);

        if (startingSegment == null)
        {
            Debug.LogError(
                "ConvoySplineController needs a starting segment.",
                this);

            enabled = false;
            return;
        }

        if (convoyMembers.Count == 0)
        {
            Debug.LogError(
                "ConvoySplineController has no convoy members.",
                this);

            enabled = false;
            return;
        }

        if (!AppendSegment(startingSegment))
        {
            enabled = false;
            return;
        }

        if (spreadConvoyAtStart)
        {
            float requiredStartingDistance =
                (convoyMembers.Count - 1) * memberSpacing;

            leaderRouteDistance = Mathf.Min(
                requiredStartingDistance,
                startingSegment.Length);

            if (requiredStartingDistance > startingSegment.Length)
            {
                Debug.LogWarning(
                    "The starting spline is too short to spread the " +
                    "entire convoy using the selected spacing.",
                    this);
            }
        }
        else
        {
            leaderRouteDistance = 0f;
        }

        UpdateMemberTransforms(true);
    }

    private void Update()
    {
        // Route input can still be selected while the convoy is stopped.
        if (waitingForChoice)
        {
            int selectedChoice = ReadRouteChoice();

            if (selectedChoice > 0)
                SelectRoute(selectedChoice);
        }

        bool shouldMove =
            movementEnabled &&
            !waitingForChoice &&
            !routeFinished;

        float targetSpeed = shouldMove
            ? movementSpeed
            : 0f;

        float speedChangeRate = targetSpeed > currentSpeed
            ? acceleration
            : deceleration;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            speedChangeRate * Time.deltaTime);

        // Continue moving while slowing down after the Stop button is pressed.
        if (currentSpeed > 0.001f &&
            !waitingForChoice &&
            !routeFinished)
        {
            AdvanceLeader(currentSpeed * Time.deltaTime);
        }

        UpdateMemberTransforms(false);
    }

    [ContextMenu("Start Convoy")]
    public void StartConvoy()
    {
        Debug.Log(
            $"StartConvoy called. " +
            $"Route finished: {routeFinished}, " +
            $"Waiting for choice: {waitingForChoice}",
            this);

        if (routeFinished)
        {
            Debug.LogWarning(
                "The convoy cannot start because it has reached the end of its route.",
                this);

            return;
        }

        movementEnabled = true;

        Debug.Log("Convoy movement enabled.", this);
    }

    [ContextMenu("Stop Convoy")]
    public void StopConvoy()
    {
        movementEnabled = false;

        Debug.Log("Convoy movement disabled.", this);
    }

    [ContextMenu("Toggle Convoy Movement")]
    public void ToggleConvoyMovement()
    {
        if (movementEnabled)
            StopConvoy();
        else
            StartConvoy();
    }

    private void AdvanceLeader(float distance)
    {
        leaderRouteDistance += Mathf.Max(0f, distance);

        // This permits crossing multiple automatic segments in one frame.
        int safetyCounter = 0;

        while (safetyCounter < 32)
        {
            safetyCounter++;

            RouteStep currentStep =
                activeRoute[activeRoute.Count - 1];

            if (leaderRouteDistance < currentStep.EndDistance)
                return;

            int choiceCount = currentStep.Segment.ChoiceCount;

            if (choiceCount == 0)
            {
                leaderRouteDistance = currentStep.EndDistance;
                routeFinished = true;
                return;
            }

            if (choiceCount == 1)
            {
                SplineRouteSegment nextSegment =
                    currentStep.Segment.GetChoice(0);

                if (!AppendSegment(nextSegment))
                {
                    leaderRouteDistance = currentStep.EndDistance;
                    routeFinished = true;
                    return;
                }

                continue;
            }

            // Multiple choices exist, so stop the convoy at the end.
            leaderRouteDistance = currentStep.EndDistance;
            waitingForChoice = true;

            Debug.Log(
                $"Route choice available: press 1-{choiceCount}.",
                this);

            return;
        }

        Debug.LogWarning(
            "The convoy crossed too many route segments in one frame. " +
            "Check for zero-length or circular automatic routes.",
            this);
    }

    public void SelectRoute(int choiceNumber)
    {
        if (!waitingForChoice)
            return;

        RouteStep currentStep =
            activeRoute[activeRoute.Count - 1];

        int choiceIndex = choiceNumber - 1;

        if (choiceIndex < 0 ||
            choiceIndex >= currentStep.Segment.ChoiceCount)
        {
            Debug.LogWarning(
                $"Route choice {choiceNumber} is not available.",
                this);

            return;
        }

        SplineRouteSegment selectedSegment =
            currentStep.Segment.GetChoice(choiceIndex);

        if (!AppendSegment(selectedSegment))
            return;

        waitingForChoice = false;
        routeFinished = false;

        Debug.Log(
            $"Selected route {choiceNumber}.",
            this);
    }

    private bool AppendSegment(SplineRouteSegment segment)
    {
        if (segment == null)
        {
            Debug.LogError(
                "A route choice contains a missing segment.",
                this);

            return false;
        }

        segment.RebuildDistanceCache();

        if (segment.Length <= Mathf.Epsilon)
        {
            Debug.LogError(
                $"Spline segment '{segment.name}' has no usable length.",
                segment);

            return false;
        }

        float routeStartDistance = 0f;

        if (activeRoute.Count > 0)
        {
            RouteStep previousStep =
                activeRoute[activeRoute.Count - 1];

            routeStartDistance = previousStep.EndDistance;

            WarnIfSegmentsDoNotConnect(
                previousStep.Segment,
                segment);
        }

        activeRoute.Add(
            new RouteStep(segment, routeStartDistance));

        return true;
    }

    private void WarnIfSegmentsDoNotConnect(
        SplineRouteSegment previous,
        SplineRouteSegment next)
    {
        if (!previous.TryGetPose(
                previous.Length,
                out Vector3 previousEnd,
                out _))
        {
            return;
        }

        if (!next.TryGetPose(
                0f,
                out Vector3 nextStart,
                out _))
        {
            return;
        }

        float gap = Vector3.Distance(previousEnd, nextStart);

        if (gap > 0.1f)
        {
            Debug.LogWarning(
                $"There is a {gap:F2} unit gap between " +
                $"'{previous.name}' and '{next.name}'. " +
                "The convoy may jump when changing splines.",
                next);
        }
    }

    private void UpdateMemberTransforms(bool snapRotation)
    {
        for (int i = 0; i < convoyMembers.Count; i++)
        {
            Transform member = convoyMembers[i];

            if (member == null)
                continue;

            float memberRouteDistance =
                leaderRouteDistance - i * memberSpacing;

            memberRouteDistance =
                Mathf.Max(0f, memberRouteDistance);

            if (!TryEvaluateRoute(
                    memberRouteDistance,
                    out Vector3 position,
                    out Quaternion rotation))
            {
                continue;
            }

            member.position = position;

            if (snapRotation || rotationSharpness <= 0f)
            {
                member.rotation = rotation;
            }
            else
            {
                float rotationAmount =
                    1f - Mathf.Exp(
                        -rotationSharpness * Time.deltaTime);

                member.rotation = Quaternion.Slerp(
                    member.rotation,
                    rotation,
                    rotationAmount);
            }
        }
    }

    private bool TryEvaluateRoute(
        float routeDistance,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (activeRoute.Count == 0)
            return false;

        // Search backward because most members will usually be near
        // the newest route segment.
        for (int i = activeRoute.Count - 1; i >= 0; i--)
        {
            RouteStep step = activeRoute[i];

            if (routeDistance >= step.StartDistance || i == 0)
            {
                float distanceOnSegment =
                    routeDistance - step.StartDistance;

                distanceOnSegment = Mathf.Clamp(
                    distanceOnSegment,
                    0f,
                    step.Segment.Length);

                return step.Segment.TryGetPose(
                    distanceOnSegment,
                    out position,
                    out rotation);
            }
        }

        return false;
    }

    private int ReadRouteChoice()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame ||
                keyboard.numpad1Key.wasPressedThisFrame)
            {
                return 1;
            }

            if (keyboard.digit2Key.wasPressedThisFrame ||
                keyboard.numpad2Key.wasPressedThisFrame)
            {
                return 2;
            }

            if (keyboard.digit3Key.wasPressedThisFrame ||
                keyboard.numpad3Key.wasPressedThisFrame)
            {
                return 3;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha1) ||
            Input.GetKeyDown(KeyCode.Keypad1))
        {
            return 1;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) ||
            Input.GetKeyDown(KeyCode.Keypad2))
        {
            return 2;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) ||
            Input.GetKeyDown(KeyCode.Keypad3))
        {
            return 3;
        }
#endif

        return 0;
    }

    public void AddMember(Transform newMember)
    {
        if (newMember == null ||
            convoyMembers.Contains(newMember))
        {
            return;
        }

        convoyMembers.Add(newMember);
    }

    public void RemoveMember(Transform member)
    {
        convoyMembers.Remove(member);
    }

    public IReadOnlyList<Transform> ConvoyMembers => convoyMembers;

    public float ConvoyLength
    {
        get
        {
            if (convoyMembers == null || convoyMembers.Count <= 1)
                return 0f;

            return (convoyMembers.Count - 1) * memberSpacing;
        }
    }

    public Vector3 GetConvoyCenter()
    {
        if (convoyMembers == null || convoyMembers.Count == 0)
            return transform.position;

        Vector3 totalPosition = Vector3.zero;
        int validMembers = 0;

        foreach (Transform member in convoyMembers)
        {
            if (member == null)
                continue;

            totalPosition += member.position;
            validMembers++;
        }

        if (validMembers == 0)
            return transform.position;

        return totalPosition / validMembers;
    }
}