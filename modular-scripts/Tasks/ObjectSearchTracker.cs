using UnityEngine;
using System.IO;
using System;

public class ObjectSearchTracker : MonoBehaviour
{
    static string objectCSVTimestamp = "";

    private static string GetCSVFilePath()
    {
        if (string.IsNullOrEmpty(objectCSVTimestamp))
            objectCSVTimestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        string participantId = ParticipantContext.GetParticipantId();
        string sceneName = ParticipantContext.GetSceneName();
        string folderPath = CSVLogger.BuildFolderPath(participantId, "OBJECT_Logs");

        return Path.Combine(folderPath, $"{sceneName}_SearchTask_{objectCSVTimestamp}.csv");
    }

    public static void LogAllObjectsFound()
    {
        string sceneName = ParticipantContext.GetSceneName();
        string filePath = GetCSVFilePath();
        bool fileExists = File.Exists(filePath);

        using (StreamWriter writer = new StreamWriter(filePath, append: true))
        {
            if (!fileExists)
                writer.WriteLine("Scene,DateTimeLocal,UnscaledTime");

            writer.WriteLine($"{sceneName},{CSVLogger.GetDateTimeLocal()},{CSVLogger.GetUnscaledTime():F6}");
        }
    }

    public static void LogFinalObjectCount(int foundCount, int totalCount)
    {
        string participantId = ParticipantContext.GetParticipantId();
        string currentScene = ParticipantContext.GetSceneName();
        string folderPath = CSVLogger.BuildFolderPath(participantId, "OBJECT_Logs");
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filePath = Path.Combine(folderPath, $"{currentScene}_SearchTaskFinal_{timestamp}.csv");

        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine("Scene,DateTimeLocal,UnscaledTime,ObjectsFound,TotalObjects");
            writer.WriteLine($"{currentScene},{CSVLogger.GetDateTimeLocal()},{CSVLogger.GetUnscaledTime():F6},{foundCount},{totalCount}");
        }
    }
}
