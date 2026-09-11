using UnityEngine;

public class EyeTrackingController : MonoBehaviour
{
    [Header("Eye Tracking")]
    public Camera trackingCamera;
    public float gazeSampleRate = 30f;
    public bool enabledTracking = true;

    private float nextSampleTime;
    private Vector3 lastGazeDirection;

    private void Update()
    {
        if (!enabledTracking || trackingCamera == null)
            return;

        if (Time.unscaledTime < nextSampleTime)
            return;

        nextSampleTime = Time.unscaledTime + (1f / gazeSampleRate);

        Ray gazeRay = trackingCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        lastGazeDirection = gazeRay.direction.normalized;

        GazeTargetTracker.UpdateTarget(gazeRay, trackingCamera.transform.position);
    }

    public Vector3 GetCurrentGazeDirection()
    {
        return lastGazeDirection;
    }
}
