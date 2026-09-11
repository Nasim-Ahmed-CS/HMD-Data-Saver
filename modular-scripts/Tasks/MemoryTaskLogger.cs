using System;
using System.IO;
using UnityEngine;

public static class MemoryTaskLogger
{
    public static void WriteResultToCSV(
        string participantId,
        int selectedID,
        int correctAnswer,
        bool isCorrect,
        float duration,
        string dateTimeLocal,
        float unscaledTime)
    {
        string sceneName = ParticipantContext.GetSceneName();
        string folderPath = CSVLogger.BuildFolderPath(participantId, "MEMORY_logs");
        string filePath = Path.Combine(folderPath, $"MemoryTaskResult_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "scene_name,DateTimeLocal,UnscaledTime,correct_answer,selected_answer,is_correct,duration" + Environment.NewLine);
        }

        string line = $"{sceneName},{dateTimeLocal},{unscaledTime:F6},{correctAnswer},{selectedID},{isCorrect},{duration:F3}";
        CSVLogger.AppendLine(filePath, line);
    }
}
