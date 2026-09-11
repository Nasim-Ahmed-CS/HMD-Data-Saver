using UnityEngine;

public static class GazeTargetTracker
{
    public static GameObject CurrentGazeTarget { get; private set; }

    public static void UpdateTarget(Ray gazeRay, Vector3 origin)
    {
        RaycastHit hit;
        if (Physics.Raycast(gazeRay, out hit, 100f))
        {
            CurrentGazeTarget = hit.collider.gameObject;
        }
        else
        {
            CurrentGazeTarget = null;
        }
    }
}
