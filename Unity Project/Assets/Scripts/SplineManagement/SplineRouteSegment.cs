using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class SplineRouteSegment : MonoBehaviour
{
    [Header("Possible routes from the end of this spline")]
    [Tooltip("Element 0 = key 1, element 1 = key 2, element 2 = key 3")]
    [SerializeField] private SplineRouteSegment[] nextSegments;

    [Header("Distance Accuracy")]
    [Tooltip("Higher values produce more accurate vehicle spacing.")]
    [SerializeField, Min(8)] private int distanceSamples = 100;

    [Header("Rotation")]
    [SerializeField] private bool useSplineUpDirection = false;

    private SplineContainer splineContainer;

    private float[] sampleTimes;
    private float[] sampleDistances;

    public float Length { get; private set; }

    public int ChoiceCount
    {
        get
        {
            if (nextSegments == null)
                return 0;

            return Mathf.Min(nextSegments.Length, 3);
        }
    }

    private void Awake()
    {
        RebuildDistanceCache();
    }

    private void OnValidate()
    {
        distanceSamples = Mathf.Max(8, distanceSamples);

        splineContainer = GetComponent<SplineContainer>();

        if (splineContainer != null)
            RebuildDistanceCache();
    }

    public SplineRouteSegment GetChoice(int index)
    {
        if (nextSegments == null)
            return null;

        if (index < 0 || index >= ChoiceCount)
            return null;

        return nextSegments[index];
    }

    [ContextMenu("Rebuild Distance Cache")]
    public void RebuildDistanceCache()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();

        if (splineContainer == null)
        {
            Length = 0f;
            return;
        }

        int numberOfSamples = Mathf.Max(8, distanceSamples);

        sampleTimes = new float[numberOfSamples + 1];
        sampleDistances = new float[numberOfSamples + 1];

        Vector3 previousPosition =
            (Vector3)splineContainer.EvaluatePosition(0f);

        sampleTimes[0] = 0f;
        sampleDistances[0] = 0f;

        float accumulatedDistance = 0f;

        for (int i = 1; i <= numberOfSamples; i++)
        {
            float t = i / (float)numberOfSamples;

            Vector3 currentPosition =
                (Vector3)splineContainer.EvaluatePosition(t);

            accumulatedDistance +=
                Vector3.Distance(previousPosition, currentPosition);

            sampleTimes[i] = t;
            sampleDistances[i] = accumulatedDistance;

            previousPosition = currentPosition;
        }

        Length = accumulatedDistance;
    }

    public bool TryGetPose(
        float distance,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = transform.position;
        rotation = transform.rotation;

        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();

        if (sampleDistances == null ||
            sampleDistances.Length < 2 ||
            Length <= Mathf.Epsilon)
        {
            RebuildDistanceCache();
        }

        if (Length <= Mathf.Epsilon)
            return false;

        distance = Mathf.Clamp(distance, 0f, Length);

        float t = ConvertDistanceToSplineTime(distance);

        bool result = splineContainer.Evaluate(
            t,
            out float3 splinePosition,
            out float3 splineTangent,
            out float3 splineUp);

        if (!result)
            return false;

        position = (Vector3)splinePosition;

        Vector3 forward = (Vector3)splineTangent;
        Vector3 up = useSplineUpDirection
            ? (Vector3)splineUp
            : Vector3.up;

        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;

        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.up;

        forward.Normalize();
        up.Normalize();

        // Prevent LookRotation errors if forward and up are parallel.
        if (Vector3.Cross(forward, up).sqrMagnitude < 0.0001f)
        {
            up = transform.up;

            if (Vector3.Cross(forward, up).sqrMagnitude < 0.0001f)
                up = Vector3.forward;
        }

        rotation = Quaternion.LookRotation(forward, up);

        return true;
    }

    private float ConvertDistanceToSplineTime(float distance)
    {
        int low = 0;
        int high = sampleDistances.Length - 1;

        // Binary search through the distance lookup table.
        while (low + 1 < high)
        {
            int middle = (low + high) / 2;

            if (sampleDistances[middle] <= distance)
                low = middle;
            else
                high = middle;
        }

        float lowerDistance = sampleDistances[low];
        float upperDistance = sampleDistances[high];

        float range = upperDistance - lowerDistance;

        if (range <= Mathf.Epsilon)
            return sampleTimes[low];

        float interpolation =
            (distance - lowerDistance) / range;

        return Mathf.Lerp(
            sampleTimes[low],
            sampleTimes[high],
            interpolation);
    }
}