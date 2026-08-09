using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class ConvoyController : MonoBehaviour
{
    [Header("Convoy Members")]
    [SerializeField]
    private SplineAnimate[] convoyVehicles;

    [Tooltip("Automatically find SplineAnimate components under this object.")]
    [SerializeField]
    private bool findVehiclesInChildren = true;

    [Header("Stopping")]
    [SerializeField, Min(0f)]
    private float stopDuration = 1.5f;

    [SerializeField]
    private AnimationCurve slowdownCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isStopping;
    private bool isStopped;

    private float[] originalSpeeds;

    private void Awake()
    {
        if (findVehiclesInChildren)
        {
            convoyVehicles =
                GetComponentsInChildren<SplineAnimate>(true);
        }
    }

    public bool ContainsVehicle(SplineAnimate vehicle)
    {
        if (vehicle == null || convoyVehicles == null)
            return false;

        foreach (SplineAnimate convoyVehicle in convoyVehicles)
        {
            if (convoyVehicle == vehicle)
                return true;
        }

        return false;
    }

    public void StopConvoy()
    {
        if (isStopping || isStopped)
            return;

        StartCoroutine(SmoothStopConvoy());
    }

    private IEnumerator SmoothStopConvoy()
    {
        isStopping = true;

        originalSpeeds = new float[convoyVehicles.Length];

        // Save each vehicle's normal speed.
        for (int i = 0; i < convoyVehicles.Length; i++)
        {
            if (convoyVehicles[i] != null)
            {
                originalSpeeds[i] =
                    convoyVehicles[i].MaxSpeed;
            }
        }

        if (stopDuration <= 0f)
        {
            PauseAllVehicles();
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < stopDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / stopDuration
            );

            float slowdownAmount =
                slowdownCurve.Evaluate(progress);

            for (int i = 0; i < convoyVehicles.Length; i++)
            {
                SplineAnimate vehicle = convoyVehicles[i];

                if (vehicle == null)
                    continue;

                // Save the vehicle's current spline position.
                float currentPosition =
                    vehicle.NormalizedTime;

                // Gradually reduce its speed.
                vehicle.MaxSpeed = Mathf.Lerp(
                    originalSpeeds[i],
                    0.01f,
                    slowdownAmount
                );

                /*
                 * Changing MaxSpeed recalculates the duration.
                 * Restore the position so the vehicle does not
                 * jump to another part of the spline.
                 */
                vehicle.NormalizedTime =
                    currentPosition;
            }

            yield return null;
        }

        PauseAllVehicles();
    }

    private void PauseAllVehicles()
    {
        for (int i = 0; i < convoyVehicles.Length; i++)
        {
            SplineAnimate vehicle = convoyVehicles[i];

            if (vehicle == null)
                continue;

            float stoppedPosition =
                vehicle.NormalizedTime;

            vehicle.Pause();

            // Restore its normal speed while paused.
            if (originalSpeeds != null &&
                i < originalSpeeds.Length)
            {
                vehicle.MaxSpeed =
                    originalSpeeds[i];
            }

            // Make sure changing speed did not move it.
            vehicle.NormalizedTime =
                stoppedPosition;

            StopRigidbody(vehicle);
        }

        isStopping = false;
        isStopped = true;
    }

    private void StopRigidbody(SplineAnimate vehicle)
    {
        Rigidbody rb =
            vehicle.GetComponentInParent<Rigidbody>();

        if (rb == null || rb.isKinematic)
            return;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif

        rb.angularVelocity = Vector3.zero;
    }

    public void ResumeConvoy()
    {
        if (isStopping)
            return;

        foreach (SplineAnimate vehicle in convoyVehicles)
        {
            if (vehicle != null)
                vehicle.Play();
        }

        isStopped = false;
    }
}