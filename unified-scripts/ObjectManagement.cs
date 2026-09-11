using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ObjectManagement : MonoBehaviour
{
    static string id;
    static string sessionID;
    static string sceneName;
    private static string objectCSVTimestamp = "";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private static string GetCSVFilePath()
    {
        if (string.IsNullOrEmpty(objectCSVTimestamp))
        {
            objectCSVTimestamp =
                DateTime.Now.ToString(
                    "yyyy-MM-dd_HH-mm-ss"
                );
        }

        id =
            ParticipantManager.Instance.ParticipantId;

        sceneName =
            SceneManager.GetActiveScene().name;

        string folderPath =
            Path.Combine(
                Application.persistentDataPath,
                $"{id}/OBJECT_Logs"
            );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        return Path.Combine(
            folderPath,
            $"{sceneName}_SearchTask_{objectCSVTimestamp}.csv"
        );
    }

    public static void LogAllObjectsFound()
    {
        sceneName = SceneManager.GetActiveScene().name;
        string filePath = GetCSVFilePath();
        bool fileExists = File.Exists(filePath);

        using (StreamWriter writer =
               new StreamWriter(filePath, append: true))
        {
            if (!fileExists)
            {
                writer.WriteLine("Scene,DateTimeLocal,UnscaledTime");
            }

            string dateTimeLocal =  DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            float unscaledTime = Time.unscaledTime;

            writer.WriteLine(
                $"{sceneName}," +
                $"{dateTimeLocal}," +
                $"{unscaledTime:F6}"
            );
        }

        // Existing X-mark logic remains untouched.
        SceneAudioIntro.Instance?
            .PlayWaitInstruction();
    }

    public static void LogFinalObjectCount(int foundCount, int totalCount)
    {
        string participantId =
            ParticipantManager.Instance.ParticipantId;

        string currentScene =
            SceneManager.GetActiveScene().name;

        string folderPath = Path.Combine(
            Application.persistentDataPath,
            $"{participantId}/OBJECT_Logs"
        );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string timestamp =
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        string filePath = Path.Combine(
            folderPath,
            $"{currentScene}_SearchTaskFinal_{timestamp}.csv"
        );

        using (StreamWriter writer =
               new StreamWriter(filePath, false))
        {
            writer.WriteLine(
                "Scene,DateTimeLocal,UnscaledTime,ObjectsFound,TotalObjects"
            );

            string dateTimeLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            float unscaledTime = Time.unscaledTime;

            writer.WriteLine(
                $"{currentScene}," +
                $"{dateTimeLocal}," +
                $"{unscaledTime:F6}," +
                $"{foundCount}," +
                $"{totalCount}"
            );
        }

        Debug.Log(
            $"Final object count saved: " +
            $"{foundCount}/{totalCount} at {filePath}"
        );
    }
}
