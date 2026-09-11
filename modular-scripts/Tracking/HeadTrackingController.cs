using UnityEngine;

public class HeadTrackingController : MonoBehaviour
{
    [Header("Head Tracking")]
    public Transform headTransform;
    public bool trackHeadMovement = true;

    private Vector3 previousPosition;
    private Vector3 previousRotation;

    private void Start()
    {
        if (headTransform != null)
        {
            previousPosition = headTransform.position;
            previousRotation = headTransform.eulerAngles;
        }
    }

    private void Update()
    {
        if (!trackHeadMovement || headTransform == null)
            return;

        Vector3 currentPosition = headTransform.position;
        Vector3 currentRotation = headTransform.eulerAngles;

        float movementDelta = Vector3.Distance(currentPosition, previousPosition);
        float rotationDelta = Vector3.Distance(currentRotation, previousRotation);

        if (movementDelta > 0.0001f || rotationDelta > 0.0001f)
        {
            Debug.Log($"Head moved: pos={currentPosition}, rot={currentRotation}");
        }

        previousPosition = currentPosition;
        previousRotation = currentRotation;
    }

    public Vector3 GetHeadPosition()
    {
        if (headTransform == null)
            return Vector3.zero;

        return headTransform.position;
    }

    public Vector3 GetHeadRotation()
    {
        if (headTransform == null)
            return Vector3.zero;

        return headTransform.eulerAngles;
    }
}
