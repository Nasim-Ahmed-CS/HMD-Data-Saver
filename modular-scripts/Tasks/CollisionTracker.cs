using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class CollisionTracker : MonoBehaviour
{
    public int collisionCount = 0;

    private string GetCSVFilePath()
    {
        string id = ParticipantContext.GetParticipantId();
        string sceneName = ParticipantContext.GetSceneName();

        string folderPath = CSVLogger.BuildFolderPath(id, "COLLISION_Logs");
        return Path.Combine(folderPath, $"{sceneName}_Collisions.csv");
    }

    public void LogCollision(string objectName)
    {
        string sceneName = ParticipantContext.GetSceneName();
        string dateTimeLocal = CSVLogger.GetDateTimeLocal();
        float unscaledTime = CSVLogger.GetUnscaledTime();

        string filePath = GetCSVFilePath();
        bool fileExists = CSVLogger.FileExists(filePath);

        using (StreamWriter writer = new StreamWriter(filePath, append: true))
        {
            if (!fileExists)
            {
                writer.WriteLine("Scene,Object,DateTimeLocal,UnscaledTime,CollisionCount");
            }

            writer.WriteLine(
                $"{sceneName},{objectName},{dateTimeLocal},{unscaledTime:F6},{collisionCount}"
            );
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Hazard"))
            return;

        collisionCount++;
        Debug.Log($"Collision #{collisionCount} with {collision.collider.name}");
        LogCollision(collision.collider.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Hazard"))
            return;

        collisionCount++;
        Debug.Log($"Trigger collision #{collisionCount} with {other.gameObject.name}");
        LogCollision(other.gameObject.name);
    }
}
