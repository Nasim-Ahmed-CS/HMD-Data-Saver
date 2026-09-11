using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class CollisionCounter : MonoBehaviour
{
    public int collisionCount = 0;

    private static string GetCSVFilePath()
    {
        string id = ParticipantManager.Instance.ParticipantId;
        string sceneName = SceneManager.GetActiveScene().name;

        string folderPath = Path.Combine(
            Application.persistentDataPath,
            $"{id}/COLLISION_Logs"
        );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        return Path.Combine(
            folderPath,
            $"{sceneName}_Collisions.csv"
        );
    }

    public void LogCollision(string objectName)
    {
        string sceneName = SceneManager.GetActiveScene().name; 
        string dateTimeLocal =
         System.DateTime.Now.ToString(
             "yyyy-MM-dd HH:mm:ss.fff"
         );

        float unscaledTime = Time.unscaledTime;

        string filePath = GetCSVFilePath();
        bool fileExists = File.Exists(filePath);

        using (StreamWriter writer =
               new StreamWriter(filePath, append: true))
        {
            if (!fileExists)
            {
                writer.WriteLine(
                    "Scene,Object,DateTimeLocal,UnscaledTime,CollisionCount"
                );
            }

            writer.WriteLine(
                $"{sceneName}," +
                $"{objectName}," +
                $"{dateTimeLocal}," +
                $"{unscaledTime:F6}," +
                $"{collisionCount}"
            );
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Hazard"))
        {
            return;
        }

        collisionCount++;

        Debug.Log(
            $"Collision #{collisionCount} with " +
            collision.collider.name
        );

        LogCollision(collision.collider.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Hazard"))
        {
            return;
        }

        collisionCount++;

        Debug.Log(
            $"Trigger collision #{collisionCount} with " +
            other.gameObject.name
        );

        LogCollision(other.gameObject.name);
    }
}