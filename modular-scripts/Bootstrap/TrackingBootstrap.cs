using UnityEngine;

public class TrackingBootstrap : MonoBehaviour
{
    [Header("Tracking Components")]
    public EyeTrackingController eyeTrackingController;
    public HeadTrackingController headTrackingController;
    public CollisionTracker collisionTracker;

    private void Awake()
    {
        if (eyeTrackingController == null)
            eyeTrackingController = FindAnyObjectByType<EyeTrackingController>();

        if (headTrackingController == null)
            headTrackingController = FindAnyObjectByType<HeadTrackingController>();

        if (collisionTracker == null)
            collisionTracker = FindAnyObjectByType<CollisionTracker>();
    }
}
