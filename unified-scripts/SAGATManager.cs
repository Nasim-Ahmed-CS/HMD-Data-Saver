using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SAGATManager : MonoBehaviour
{
    public static SAGATManager instance;
    private string id = "Error";

    [Header("Reference to the Scriptable Object")]
    public SAGATQuestions sagatData;

    private Dictionary<HazardName, QuestionSet> questionDict = new Dictionary<HazardName, QuestionSet>();

    public GameObject[] collisionObjects = {};

    private int numberOfCollisions = 0;
    private int numberOfNearCollisions = 0;
    private string pastName = "";


    [Header("UI References")]
    public SceneAudioIntro introduction;
    public TMP_Text questionText;
    public TMP_Text answer1, answer2, answer3, answer4, answer5, answer6;
    public GameObject ansBtn1, ansBtn2, ansBtn3, ansBtn4, ansBtn5, ansBtn6;
    public GameObject[] sliderBtns;
    public GameObject slider;

    [Header("Tracking")]
    public HazardName currentHazard;
    public int currentQuestionIndex = 0;
    public List<bool> answerResults = new List<bool>();

    [Header("Session Settings")]
    public string sessionID = "Session_01";
    private float questionShownTime = 0f;
    private string initialCSVTimestamp = "";
    public bool isQuestionActive = false;
    public GameObject changedObject;
    public bool canBeCollided = false;

    [Header("L3 Proximity Thresholds")]
    [Tooltip("Hazard considered to have reached user at or below this distance (m)")]
    public float reachedThreshold = 0.5f;
    [Tooltip("Hazard considered a near-miss at or below this distance (m)")]
    public float nearMissThreshold = 1.2f;

    private bool didCollide = false;
    private bool almostCollide = false;

    [Header("Spawn Delay Settings")]
    [Tooltip("Pairs each hazard with a sort-order value. Lower = spawns first. Values are only used for ordering — actual timing is driven by interHazardDelay.")]
    public List<ChangeDelay> changeDelayList = new List<ChangeDelay>();
    public Dictionary<HazardName, int> changesDict = new Dictionary<HazardName, int>();

    private float capturedMinDistance = 0f;

    [Header("Answer Background Panels")]
    public GameObject bgSetOf2;
    public GameObject bgSetOf4;
    public GameObject bgSetOf5;
    public GameObject bgSetOf6;

    [Header("Button Colors")]
    public Color buttonDefaultColor  = Color.gray;
    public Color buttonSelectedColor = new Color(0.12549f, 0.58824f, 0.95294f, 1f);

    // ----------------------------------------------------------------
    // Sequence control
    // ----------------------------------------------------------------

    [Header("Sequence Control")]
    [Tooltip("Reference to the HazardManager in the scene")]
    public HazardManager hazardManager;
    [Tooltip("Reference to the NonHazardManager in the scene")]
    public NonHazardManager nonHazardManager;
    [Tooltip("Seconds to wait after a questionnaire completes before the next hazard spawns")]
    public float interHazardDelay = 30f;
    [Tooltip("Seconds to wait before the very first hazard spawns after the sequence starts")]
    public float initialDelay = 5f;

    private List<HazardName> _hazardQueue = new List<HazardName>();
    private int _hazardIndex = 0;
    private Coroutine _spawnNextRoutine;

    public bool IsSessionEnding { get; private set; } = false;

    private const string SagatCsvHeader =
        "SessionID,Scene,DateTimeLocal,UnscaledTime," +
        "Hazard,QuestionIndex,SALevel," +
        "QuestionText,SelectedAnswerID,SelectedAnswerText," +
        "CorrectAnswerID,CorrectAnswerText," +
        "Correct?,ResponseTime,DistanceFromCamera," +
        "MinDistanceDuringMovement," +
        "Collision,NearCollision," +
        "NumCollisionsSoFar,NumNearCollisionsSoFar," +
        "ObjectPosX,ObjectPosY,ObjectPosZ," +
        "UserPosX,UserPosY,UserPosZ";

    private const string FmsCsvHeader =
        "SessionID,Scene,DateTimeLocal,UnscaledTime," +
        "SelectedAnswer,ResponseTime," +
        "DistanceFromCamera,MinDistanceDuringMovement";
    // ----------------------------------------------------------------

    private Vector3? capturedObjectPosition;
    private Vector3? capturedUserPosition;

    private void Awake()
    {
        instance = this;
        id = ParticipantManager.Instance.ParticipantId;

        if (string.IsNullOrEmpty(initialCSVTimestamp))
            initialCSVTimestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        BuildQuestionDictionary();
        BuildChangeDelayDictionaries();
        BuildSortedQueue();

        Debug.Log("SAGAT logs: " + Path.Combine(Application.persistentDataPath, "SAGAT_Logs"));
    }

    private void Start()
    {
        Debug.Log("SAGAT: start.");
        //restart for every new scene
        numberOfCollisions = 0;
        numberOfNearCollisions = 0;
        pastName = "";
        StartSequence();
    }

    private void Update()
    {
        if (IsSessionEnding)
            return;

        if ((changedObject != null) && (canBeCollided == true))
        {
            //Debug.Log("Distance from " + changedObject.name + " = " + CheckProximityOrCollision(changedObject));
            if (CheckProximityOrCollision(changedObject) <= reachedThreshold)
            {
                didCollide = true;
                Debug.Log("COLLISION WITH " + changedObject.name);
            }
            else if (CheckProximityOrCollision(changedObject) <= nearMissThreshold)
            {
                almostCollide = true;
                Debug.Log("NEAR COLLISION WITH " + changedObject.name);
            }

            //don't set them to false here; only sets AFTER questionnaire
        }
    }

    private void BuildQuestionDictionary()
    {
        questionDict.Clear();
        foreach (var qs in sagatData.questionSet)
            if (!questionDict.ContainsKey(qs.hazardName))
                questionDict.Add(qs.hazardName, qs);

        Debug.Log($"SAGAT: Loaded {questionDict.Count} hazard question sets.");
    }

    // ----------------------------------------------------------------
    // Sequence orchestration
    // ----------------------------------------------------------------

    /// <summary>
    /// Sorts changeDelayList by delayTime to determine spawn order, then kicks off the first hazard.
    /// </summary>
    private void BuildSortedQueue()
    {
        _hazardQueue = changeDelayList
            .OrderBy(c => c.delayTime)
            .Select(c => c.hazardName)
            .ToList();

        Debug.Log($"SAGAT: Hazard queue built — {_hazardQueue.Count} events in order: " +
                  string.Join(", ", _hazardQueue));
    }

    /// <summary>Call this once to begin the sequence (called automatically from Start).</summary>

    public void StartSequence()
    {
        if (IsSessionEnding)
            return;

        _hazardIndex = 0;

        Debug.Log("SAGAT: StartSequence.");

        if (_spawnNextRoutine != null)
        {
            StopCoroutine(_spawnNextRoutine);
        }

        _spawnNextRoutine = StartCoroutine(
            SpawnNext(initialDelay)
        );
    }

    /// <summary>Called internally after every questionnaire finishes to schedule the next hazard.</summary>
    private void ScheduleNext()
    {
        if (IsSessionEnding)
        {
            Debug.Log(
                "SAGAT: ScheduleNext ignored because session is ending."
            );
            return;
        }

        Debug.Log("SAGAT: ScheduleNext called.");

        if (_hazardIndex < _hazardQueue.Count)
        {
            if (_spawnNextRoutine != null)
            {
                StopCoroutine(_spawnNextRoutine);
            }

            _spawnNextRoutine = StartCoroutine(
                SpawnNext(interHazardDelay)
            );
        }
        else
        {
            // Do not end the session here anymore.
            // The countdown timer is now responsible for ending it.
            Debug.Log(
                "SAGAT: All hazards completed. Waiting for countdown."
            );
        }
    }

    private IEnumerator SpawnNext(float delay)
    {
        Debug.Log(
            $"SAGAT: Waiting {delay} seconds before next event."
        );

        yield return new WaitForSeconds(delay);

        _spawnNextRoutine = null;

        if (IsSessionEnding)
        {
            yield break;
        }

        if (_hazardIndex < _hazardQueue.Count)
        {
            HazardName next = _hazardQueue[_hazardIndex];
            _hazardIndex++;

            Debug.Log(
                $"SAGAT: Triggering {next} " +
                $"({_hazardIndex}/{_hazardQueue.Count})"
            );

            TriggerHazard(next);
        }
    }

    private void AskFinalQuestion()
    {
        if (IsSessionEnding)
            return;

        Debug.Log("asking final question");

        //clearing previous
        answerResults.Clear();

        //setUI
        QuestionnairesTimedAppearInFrontOfCamera.instance.ActivateQuestionnaire();
        questionText.text = "Rate your discomfort on a scale from 1-10.";

        //make it have no time limit
        questionShownTime = Time.unscaledTime;

        GameObject[] answerBtns = { ansBtn1, ansBtn2, ansBtn3, ansBtn4, ansBtn5, ansBtn6 };


        //turn off other buttons
        for (int i = 0; i < 6; i++)
        {
            answerBtns[i].SetActive(false);
        }

        for (int i = 0; i < 12; i++)
        {
            slider.SetActive(true);
            sliderBtns[i].SetActive(true);

            int captured = i;
            var btn = sliderBtns[i].GetComponent<UnityEngine.UI.Button>();

            btn.onClick.RemoveAllListeners();

            bool isDisabled = (i == 0 || i == 11);

            if (isDisabled)
            {
                // Make button unclickable
                btn.interactable = false;

                // Gray out the button
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.gray;
                cb.highlightedColor = Color.gray;
                cb.pressedColor = Color.gray;
                cb.selectedColor = Color.gray;
                cb.disabledColor = Color.gray;
                btn.colors = cb;
            }
            else
            {
                // Ensure other buttons are enabled
                btn.interactable = true;

                btn.onClick.AddListener(() =>
                {
                    Debug.Log("CLICKED " + captured);
                    LogFinalAnswer(captured);
                    SceneMovement.Instance.puzzle.SetActive(true);
                });

                Navigation nav = btn.navigation;
                nav.mode = Navigation.Mode.None;
                btn.navigation = nav;

                ColorBlock cb = btn.colors;
                cb.normalColor = buttonSelectedColor;
                btn.colors = cb;
            }
        }
    }

    private void TriggerHazard(HazardName name)
    {
        if (IsSessionEnding)
            return;

        switch (name)
        {
            case HazardName.cat: { hazardManager.SpawnCat(); canBeCollided = true; } break;
            case HazardName.ball: { hazardManager.SpawnBall(); canBeCollided = true; } break;
            case HazardName.roomba: { hazardManager.SpawnRoomba(); canBeCollided = true; } break;
            case HazardName.human: { hazardManager.SpawnHuman(); canBeCollided = true; } break;
            case HazardName.coffeePot: { hazardManager.SpawnCoffeePot(); canBeCollided = true; } break;
            case HazardName.mop: { nonHazardManager.TriggerMop(); canBeCollided = false; } break;
            case HazardName.painting: { nonHazardManager.TriggerPainting(); canBeCollided = true; } break;
            case HazardName.lightChange: { nonHazardManager.TriggerLight(); canBeCollided = true; } break;
            case HazardName.falsePositive: { nonHazardManager.TriggerFalsePositive(); canBeCollided = true; } break;
            case HazardName.falsePositive2: { nonHazardManager.TriggerFalsePositive2(); canBeCollided = true; } break;
            case HazardName.falsePositive3: { nonHazardManager.TriggerFalsePositive3(); canBeCollided = true; } break;
            case HazardName.tv: { nonHazardManager.TriggerTV(); canBeCollided = true; } break;
            default:
                Debug.LogWarning($"SAGAT: No spawn handler for HazardName.{name}");
                break;
        }
    }

    // ----------------------------------------------------------------
    // Scene control
    // ----------------------------------------------------------------

    public void PauseScene()
    {
        if (IsSessionEnding)
            return;

        if (!isQuestionActive)
        {
            isQuestionActive = true;
            Time.timeScale = 0f;
        }
    }

    public void ResumeScene()
    {
        if (IsSessionEnding)
        {
            isQuestionActive = false;
            Time.timeScale = 1f;
            return;
        }

        if (isQuestionActive)
        {
            isQuestionActive = false;
            Time.timeScale = 1f;
        }
    }

    // ----------------------------------------------------------------
    // Entry point — called by HazardManager / NonHazardManager
    // ----------------------------------------------------------------

    public void StartSAGATForHazard(HazardName hazard)
    {
        if (IsSessionEnding)
            return;

        currentHazard = hazard;
        currentQuestionIndex = 0;
        answerResults.Clear();

        capturedObjectPosition = changedObject != null
        ? changedObject.transform.position
        : (Vector3?)null;

        capturedUserPosition = Camera.main != null
            ? Camera.main.transform.position
            : (Vector3?)null;

        capturedMinDistance = 0f;
        if (changedObject != null)
        {
            HazardProximityTracker tracker = changedObject.GetComponent<HazardProximityTracker>();
            if (tracker != null)
            {
                capturedMinDistance = tracker.minDistance == float.MaxValue ? 0f : tracker.minDistance;
                tracker.StopTracking();
            }
            else if (Camera.main != null)
            {
                capturedMinDistance = Vector3.Distance(
                    Camera.main.transform.position, changedObject.transform.position);
            }
        }

        SetUI(hazard, 0);
    }

    // ----------------------------------------------------------------
    // UI population
    // ----------------------------------------------------------------

    private void SetUI(HazardName hazard, int questionIndex)
    {
        if (IsSessionEnding)
            return;

        SceneMovement.Instance.puzzle.SetActive(false);

        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        if (!questionDict.ContainsKey(hazard)) return;

        var qSet = questionDict[hazard];
        if (questionIndex >= qSet.MultipleChoiceQs.Count)
        {
            FinishQuestionnaire();
            return;
        }

        currentHazard = hazard;
        currentQuestionIndex = questionIndex;

        MultipleQuestion mq = qSet.MultipleChoiceQs[questionIndex];
        questionText.text = mq.question;

        TMP_Text[]   answerTexts = { answer1, answer2, answer3, answer4, answer5, answer6 };
        GameObject[] answerBtns  = { ansBtn1, ansBtn2, ansBtn3, ansBtn4, ansBtn5, ansBtn6 };

        for (int i = 0; i < 6; i++) answerBtns[i].SetActive(false);

        int count = Mathf.Min(mq.answersList.Count, 6);

        if (bgSetOf2 != null) bgSetOf2.SetActive(count == 2);
        if (bgSetOf4 != null) bgSetOf4.SetActive(count == 4);
        if (bgSetOf5 != null) bgSetOf5.SetActive(count == 5);
        if (bgSetOf6 != null) bgSetOf6.SetActive(count == 6);

        for (int i = 0; i < count; i++)
        {
            answerBtns[i].SetActive(true);
            answerTexts[i].text = mq.answersList[i].answer;

            int captured = i;
            var btn = answerBtns[i].GetComponent<UnityEngine.UI.Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnAnswerSelected(captured));

            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            ColorBlock cb = btn.colors;
            cb.normalColor = buttonSelectedColor;
            btn.colors = cb;
        }

        questionShownTime = Time.unscaledTime;

        if (questionIndex > 0)
            QuestionnairesTimedAppearInFrontOfCamera.instance.RestartTimeout();
    }

    // ----------------------------------------------------------------
    // Answer handling
    // ----------------------------------------------------------------

    public void OnAnswerSelected(int selectedIndex)
    {
        if (IsSessionEnding)
            return;

        if (!questionDict.ContainsKey(currentHazard)) return;

        var qSet = questionDict[currentHazard];
        var mq   = qSet.MultipleChoiceQs[currentQuestionIndex];

        int runtimeCorrect = GetRuntimeCorrectAnswer(mq);
        bool isCorrect     = (selectedIndex == runtimeCorrect);

        answerResults.Add(isCorrect);
        LogSAGATAnswer(selectedIndex, isCorrect, runtimeCorrect);

        int nextIndex = currentQuestionIndex + 1;
        if (nextIndex < qSet.MultipleChoiceQs.Count)
            SetUI(currentHazard, nextIndex);
        else
            FinishQuestionnaire();
    }

    public void OnAutoTimeout()
    {
        if (IsSessionEnding)
            return;

        answerResults.Add(false);

        if (questionDict.ContainsKey(currentHazard))
        {
            var qSet = questionDict[currentHazard];
            var mq = qSet.MultipleChoiceQs[currentQuestionIndex];
            int runtimeCorrect = GetRuntimeCorrectAnswer(mq);

            // Log the current timed-out SAGAT question
            LogSAGATAnswer(-1, false, runtimeCorrect);

            // Log remaining SAGAT questions as skipped
            int savedIndex = currentQuestionIndex;
            for (int i = currentQuestionIndex + 1; i < qSet.MultipleChoiceQs.Count; i++)
            {
                currentQuestionIndex = i;
                LogSkippedQuestion();
            }
            currentQuestionIndex = savedIndex;
        }

        // Reset SAGAT state, but DO NOT schedule next yet
        didCollide = false;
        almostCollide = false;
        changedObject = null;

        // Instead of skipping onward, show the FMS final question
        AskFinalQuestion();
    }

    private void FinishQuestionnaire()
    {
        if (IsSessionEnding)
            return;

        //reset collision data after answering a question, before scheduling next
        didCollide = false;
        almostCollide = false;
        changedObject = null;
        AskFinalQuestion();
    }

    // ----------------------------------------------------------------
    // L3 Runtime Correct Answer
    // ----------------------------------------------------------------

    private int GetRuntimeCorrectAnswer(MultipleQuestion mq)
    {
        if (mq.questionLevel == category.L3
            && mq.correctAnswerFar != -1
            && changedObject != null)
        {

                bool hazardReachedUser = didCollide;

                return hazardReachedUser ? mq.correctAnswer : mq.correctAnswerFar;
        }

        return mq.correctAnswer;
    }

    // ----------------------------------------------------------------
    // Logging
    // ----------------------------------------------------------------

    private string GetCSVFilePath(bool isSAGAT)
    {
        id = ParticipantManager.Instance.ParticipantId;
        string sceneName  = SceneManager.GetActiveScene().name;
        string folderPath = (isSAGAT ? Path.Combine(Application.persistentDataPath, $"{id}/SAGAT_Logs") : Path.Combine(Application.persistentDataPath, $"{id}/FMS_Logs"));
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        return (isSAGAT ? Path.Combine(folderPath, $"SAGAT_{sessionID}_{sceneName}_{initialCSVTimestamp}.csv") : Path.Combine(folderPath, $"FMS_{sessionID}_{sceneName}_{initialCSVTimestamp}.csv"));
    }

    private void LogFinalAnswer(int selectedIndex)
    {
        if (IsSessionEnding)
            return;

        string filePath = GetCSVFilePath(false);
        bool fileExists = File.Exists(filePath);

        using (StreamWriter writer = new StreamWriter(filePath, append: true))
        {
            if (!fileExists)
            {
                writer.WriteLine(FmsCsvHeader);
            }

            string sceneName = SceneManager.GetActiveScene().name;

            string dateTimeLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            float answerUnscaledTime = Time.unscaledTime;

            string selectedAnswer = "none";
            if (selectedIndex==0)
            {
                selectedAnswer = "Very low";
            } else if (selectedIndex==11)
            {
                selectedAnswer = "Very high";
            } else
            {
                selectedAnswer = $"{selectedIndex}";
            }

            float responseTime = answerUnscaledTime - questionShownTime;
            float distanceFromCamera = Camera.main != null
                ? Vector3.Distance(Camera.main.transform.position,
                    QuestionnairesTimedAppearInFrontOfCamera.instance.transform.position)
                : 0f;

            writer.WriteLine(
            $"{sessionID}," +
            $"{sceneName}," +
            $"{dateTimeLocal}," +
            $"{answerUnscaledTime:F6}," +
            $"{selectedAnswer}," +
            $"{responseTime:F3}," +
            $"{distanceFromCamera:F3}," +
            $"{capturedMinDistance:F3}");

            slider.SetActive(false);
            for (int i = 0; i < 12; i++)
            {
                sliderBtns[i].SetActive(false);
            }

                Debug.Log("Saved FMS Log to: " + GetCSVFilePath(false));

            //after saving final answer, schedules next
            QuestionnairesTimedAppearInFrontOfCamera.instance.AnswerCallback();
            ScheduleNext();
        }
    }

    private void LogSAGATAnswer(int selectedIndex, bool isCorrect, int runtimeCorrectAnswer)
    {
        string filePath  = GetCSVFilePath(true);
        bool   fileExists = File.Exists(filePath);

        using (StreamWriter writer = new StreamWriter(filePath, append: true))
        {
            if (!fileExists)
            {
                writer.WriteLine(SagatCsvHeader);
            }

            var qSet = questionDict[currentHazard];
            var mq   = qSet.MultipleChoiceQs[currentQuestionIndex];

            string sceneName          = SceneManager.GetActiveScene().name;
            string level              = mq.questionLevel.ToString();
            string selectedAnswerID   = selectedIndex == -1 ? "timeout" : selectedIndex.ToString();
            string selectedAnswerText = selectedIndex == -1 ? "timeout" :
                (selectedIndex < mq.answersList.Count ? mq.answersList[selectedIndex].answer : "null");
            string correctAnswerID    = runtimeCorrectAnswer.ToString();
            string correctAnswerText  = runtimeCorrectAnswer >= 0 && runtimeCorrectAnswer < mq.answersList.Count
                ? mq.answersList[runtimeCorrectAnswer].answer : "N/A";

            string dateTimeLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            float answerUnscaledTime = Time.unscaledTime;

            float responseTime       = answerUnscaledTime - questionShownTime;
            float distanceFromCamera = Camera.main != null
                ? Vector3.Distance(Camera.main.transform.position,
                    QuestionnairesTimedAppearInFrontOfCamera.instance.transform.position)
                : 0f;
            bool collision = false;
            bool nearCollision = false;

            if (didCollide)
            {
                collision = true;
                //if new object being collided, increase number
                if (!(pastName.Equals(currentHazard.ToString())))
                {
                    numberOfCollisions++;
                    pastName = currentHazard.ToString();
                }
            } else if (almostCollide)
            {
                if (!(pastName.Equals(currentHazard.ToString())))
                {
                    numberOfNearCollisions++;
                    pastName = currentHazard.ToString();
                }
                nearCollision = true;
            }

            string objectPosX = capturedObjectPosition.HasValue
               ? capturedObjectPosition.Value.x.ToString("F6")
               : "";

            string objectPosY = capturedObjectPosition.HasValue
                ? capturedObjectPosition.Value.y.ToString("F6")
                : "";

            string objectPosZ = capturedObjectPosition.HasValue
                ? capturedObjectPosition.Value.z.ToString("F6")
                : "";

            string userPosX = capturedUserPosition.HasValue
                ? capturedUserPosition.Value.x.ToString("F6")
                : "";

            string userPosY = capturedUserPosition.HasValue
                ? capturedUserPosition.Value.y.ToString("F6")
                : "";

            string userPosZ = capturedUserPosition.HasValue
                ? capturedUserPosition.Value.z.ToString("F6")
                : "";

            writer.WriteLine(
                $"{sessionID}," +
                $"{sceneName}," +
                $"{dateTimeLocal}," +
                $"{answerUnscaledTime:F6}," +
                $"{currentHazard}," +
                $"{currentQuestionIndex}," +
                $"{level}," +
                $"\"{mq.question}\",\"{selectedAnswerID}\",\"{selectedAnswerText}\"," +
                $"\"{correctAnswerID}\",\"{correctAnswerText}\"," +
                $"{isCorrect},{responseTime:F3},{distanceFromCamera:F3},{capturedMinDistance:F3},"+
                $"{collision},{nearCollision},{numberOfCollisions},{numberOfNearCollisions}," +
                $"{objectPosX},{objectPosY},{objectPosZ}," +
                $"{userPosX},{userPosY},{userPosZ}"
            );
        }

        Debug.Log("Saved SAGAT Log to: " + GetCSVFilePath(true));
    }

    private void LogSkippedQuestion()
    {
        string filePath  = GetCSVFilePath(true);
        bool   fileExists = File.Exists(filePath);

        using (StreamWriter writer = new StreamWriter(filePath, append: true))
        {
            if (!fileExists)
            {
                writer.WriteLine(SagatCsvHeader);
            }

            var qSet = questionDict[currentHazard];
            var mq   = qSet.MultipleChoiceQs[currentQuestionIndex];
            string sceneName       = SceneManager.GetActiveScene().name;

            string dateTimeLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            float skippedUnscaledTime = Time.unscaledTime;

            string level           = mq.questionLevel.ToString();
            int runtimeCorrect     = GetRuntimeCorrectAnswer(mq);
            string correctAnswerID = runtimeCorrect.ToString();
            string correctAnswerText = runtimeCorrect >= 0 && runtimeCorrect < mq.answersList.Count
                ? mq.answersList[runtimeCorrect].answer : "N/A";

            string objectPosX = capturedObjectPosition.HasValue
                ? capturedObjectPosition.Value.x.ToString("F6")
                : "";

            string objectPosY = capturedObjectPosition.HasValue
                ? capturedObjectPosition.Value.y.ToString("F6")
                : "";

            string objectPosZ = capturedObjectPosition.HasValue
                ? capturedObjectPosition.Value.z.ToString("F6")
                : "";

            string userPosX = capturedUserPosition.HasValue
                ? capturedUserPosition.Value.x.ToString("F6")
                : "";

            string userPosY = capturedUserPosition.HasValue
                ? capturedUserPosition.Value.y.ToString("F6")
                : "";

            string userPosZ = capturedUserPosition.HasValue
                ? capturedUserPosition.Value.z.ToString("F6")
                : "";

            writer.WriteLine(
                $"{sessionID}," +
                $"{sceneName}," +
                $"{dateTimeLocal}," +
                $"{skippedUnscaledTime:F6}," +
                $"{currentHazard}," +
                $"{currentQuestionIndex}," +
                $"{level}," +
                $"\"{mq.question}\"," +
                $"\"\",\"\"," +
                $"\"{correctAnswerID}\"," +
                $"\"{correctAnswerText}\"," +
                $"skipped," +
                $"0.000," +
                $"0.000," +
                $"{capturedMinDistance:F3}," +
                $"{didCollide}," +
                $"{almostCollide}," +
                $"{numberOfCollisions}," +
                $"{numberOfNearCollisions}," + 
                $"{objectPosX},{objectPosY},{objectPosZ}," +
                $"{userPosX},{userPosY},{userPosZ}"
            );
        }
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    public void SetChangedObject(GameObject hazardObject)
    {
        changedObject = hazardObject;
        if (hazardObject == null) return;

        HazardProximityTracker tracker = hazardObject.GetComponent<HazardProximityTracker>();
        if (tracker == null)
            tracker = hazardObject.AddComponent<HazardProximityTracker>();

        tracker.enabled = true;
        tracker.ResetTracker();
    }

    public float CheckProximityOrCollision(GameObject obj)
    {
        if (Camera.main == null || obj == null) return float.MaxValue;

        float obj_x = obj.transform.position.x;
        float obj_z = obj.transform.position.z;
        float my_x = Camera.main.transform.position.x;
        float my_z = Camera.main.transform.position.z;

        double distance = Math.Sqrt((my_x - obj_x) * (my_x - obj_x) + (my_z - obj_z)*(my_z - obj_z));

        return (float)distance;
    }

    public void BuildChangeDelayDictionaries()
    {
        changesDict.Clear();
        foreach (var item in changeDelayList)
            changesDict[item.hazardName] = item.delayTime;
        Debug.Log("SAGAT: ChangeDelay dictionary filled.");
    }

    /// <summary>
    /// Still available if anything else needs to read the raw delay value, but
    /// it is no longer used for scheduling — ordering comes from BuildSortedQueue().
    /// </summary>
    public float GetSpawnDelay(HazardName hazardName) => changesDict[hazardName];

    public void RequestSessionEnd()
    {
        if (IsSessionEnding)
        {
            Debug.Log(
                "SAGAT: Duplicate session-end request ignored."
            );
            return;
        }

        IsSessionEnding = true;

        Debug.Log(
            "SAGAT: 420 active simulation seconds completed."
        );

        // Defensive protection against a delayed queued event.
        if (_spawnNextRoutine != null)
        {
            StopCoroutine(_spawnNextRoutine);
            _spawnNextRoutine = null;
        }

        canBeCollided = false;
        changedObject = null;
        didCollide = false;
        almostCollide = false;

        if (isQuestionActive)
        {
            Debug.LogWarning(
                "Timer reached zero while a questionnaire was active. " +
                "Check whether another script changed Time.timeScale."
            );
        }

        isQuestionActive = false;
        Time.timeScale = 1f;

        ObjectManagement.LogFinalObjectCount(
            ActivateOnControllerSelect.CurrentFoundCount,
            ActivateOnControllerSelect.TotalXMarksForSession
        );

        if (introduction != null)
        {
            introduction.EndScene();
        }
        else
        {
            Debug.LogError(
                "SAGATManager is missing the SceneAudioIntro reference."
            );
        }
    }
}

[System.Serializable]
public class ChangeDelay
{
    [Tooltip("Which hazard / non-hazard this entry controls")]
    public HazardName hazardName;
    [Tooltip("Used only for ordering (lower = spawns first). The actual wait between events is interHazardDelay on SAGATManager.")]
    public int delayTime;
}
