using UnityEngine;
using System.IO;
using System;

public static class ObjectSearchLogger
{
    public static void LogAllObjectsFound()
    {
        string participantId = ParticipantContext.GetParticipantId();
        string sceneName = ParticipantContext.GetSceneName();
        string folderPath = CSVLogger.BuildFolderPath(participantId, "OBJECT_Logs");
        string filePath = Path.Combine(folderPath, $"{sceneName}_SearchTask_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");

        if (!File.Exists(filePath))
            File.WriteAllText(filePath, "Scene,DateTimeLocal,UnscaledTime" + Environment.NewLine);

        string line = $"{sceneName},{CSVLogger.GetDateTimeLocal()},{CSVLogger.GetUnscaledTime():F6}";
        CSVLogger.AppendLine(filePath, line);
    }

    public static void LogFinalObjectCount(int foundCount, int totalCount)
    {
        string participantId = ParticipantContext.GetParticipantId();
        string currentScene = ParticipantContext.GetSceneName();
        string folderPath = CSVLogger.BuildFolderPath(participantId, "OBJECT_Logs");
        string filePath = Path.Combine(folderPath, $"{currentScene}_SearchTaskFinal_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");

        string line = $"{currentScene},{CSVLogger.GetDateTimeLocal()},{CSVLogger.GetUnscaledTime():F6},{foundCount},{totalCount}";
        File.WriteAllText(filePath, "Scene,DateTimeLocal,UnscaledTime,ObjectsFound,TotalObjects" + Environment.NewLine + line + Environment.NewLine);
    }
}
