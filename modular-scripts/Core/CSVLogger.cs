using System;
using System.IO;
using UnityEngine;

public static class CSVLogger
{
    public static string BuildFolderPath(string participantId, string folderName)
    {
        string folderPath = Path.Combine(
            Application.persistentDataPath,
            $"{participantId}/{folderName}"
        );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        return folderPath;
    }

    public static string GetTimestampedFilePath(
        string participantId,
        string folderName,
        string filePrefix)
    {
        string folderPath = BuildFolderPath(participantId, folderName);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return Path.Combine(folderPath, $"{filePrefix}_{timestamp}.csv");
    }

    public static void AppendLine(string filePath, string line)
    {
        File.AppendAllText(filePath, line + Environment.NewLine);
    }

    public static bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public static string GetDateTimeLocal()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }

    public static float GetUnscaledTime()
    {
        return Time.unscaledTime;
    }
}
