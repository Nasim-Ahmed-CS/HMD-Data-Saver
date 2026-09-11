using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections;

public class MemoryTaskController : MonoBehaviour
{
    public static MemoryTaskController instance = null;
    private string id = "Error";

    [Header("Task Settings")]
    [SerializeField] public int shapeID;
    [SerializeField] public GameObject popup;
    [SerializeField] public GameObject shapePopup;

    private float popupStartTime;
    private bool popupActive;
    private static string csvFilePath;
    private static bool sessionInitialized;

    [Header("Camera Follow Settings")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float forwardOffset = 0.8f;
    [SerializeField] private float heightOffset = -0.15f;
    [SerializeField] private bool followCamera = true;

    private bool answerRecorded;

    private void Awake()
    {
        instance = this;
        id = ParticipantContext.GetParticipantId();
        InitializeSessionCSV();
        popup.SetActive(false);
        StartCoroutine(DisablePopup());
    }

    private void Update()
    {
        if (popup.activeSelf || shapePopup.activeSelf)
        {
            FollowCamera();
        }
    }

    private void InitializeSessionCSV()
    {
        if (sessionInitialized)
            return;

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string subfolder = $"{id}/MEMORY_logs";
        string folderPath = Path.Combine(Application.persistentDataPath, subfolder);

        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        string fileName = $"MemoryTaskResult_{timestamp}.csv";
        csvFilePath = Path.Combine(folderPath, fileName);

        if (!File.Exists(csvFilePath))
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("scene_name,DateTimeLocal,UnscaledTime,correct_answer,selected_answer,is_correct,duration");
            File.WriteAllText(csvFilePath, sb.ToString());
        }

        sessionInitialized = true;
    }

    IEnumerator DisablePopup()
    {
        yield return new WaitForSeconds(5f);
        shapePopup.SetActive(false);
    }

    public void SchedulePopup()
    {
        popupStartTime = Time.unscaledTime;
        answerRecorded = false;
        popupActive = true;
        popup.SetActive(true);
    }

    public void OnAnswerSelected(int selectedID)
    {
        if (answerRecorded)
            return;

        answerRecorded = true;
        float answerUnscaledTime = Time.unscaledTime;

        if (!popupActive)
            popupStartTime = answerUnscaledTime;

        string sceneName = ParticipantContext.GetSceneName();
        string dateTimeLocal = CSVLogger.GetDateTimeLocal();
        float duration = answerUnscaledTime - popupStartTime;
        bool isCorrect = selectedID == shapeID;

        popupActive = false;
        popup.SetActive(false);

        WriteResultToCSV(selectedID, isCorrect, duration, dateTimeLocal, answerUnscaledTime);

        if (SceneAudioIntro.Instance != null)
        {
            SceneAudioIntro.Instance.finalInstructions();
        }
    }

    private void WriteResultToCSV(
        int selectedID,
        bool isCorrect,
        float duration,
        string dateTimeLocal,
        float unscaledTime)
    {
        string sceneName = ParticipantContext.GetSceneName();
        string line =
            $"{sceneName},{dateTimeLocal},{unscaledTime:F6},{shapeID},{selectedID},{isCorrect},{duration:F3}";

        File.AppendAllText(csvFilePath, line + Environment.NewLine);
    }

    private void FollowCamera()
    {
        if (!followCamera || playerCamera == null)
            return;

        Vector3 targetPosition = playerCamera.position +
            playerCamera.forward * forwardOffset +
            playerCamera.up * heightOffset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);

        Vector3 lookDir = transform.position - playerCamera.position;
        lookDir.y = 0f;
        transform.rotation = Quaternion.LookRotation(lookDir);
    }
}
