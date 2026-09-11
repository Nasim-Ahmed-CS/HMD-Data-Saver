// UnifiedTrackingManager.cs

using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Text;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;

public class UnifiedTrackingManager : MonoBehaviour
{
    public static UnifiedTrackingManager Instance { get; private set; }

    private StreamWriter headWriter;
    private StreamWriter controllerWriter;
    private StreamWriter handWriter;
    private StreamWriter eyeWriter;
    private StreamWriter gazeObjectWriter;

    private string id = "";

    private InputDevice headDevice;
    private InputDevice leftController;
    private InputDevice rightController;
    private XRHandSubsystem handSubsystem;

    [Header("Eye Tracking")]
    public string serverURL = "http://10.101.168.191:5000/data";
    public float eyeSendInterval = 0.1f;
    private float nextEyeSend = 0f;

    public GameObject leftGazePoseObject;
    public GameObject rightGazePoseObject;

    public float gazeRayDistance = 1000f;
    public LayerMask gazeRaycastLayers = ~0;

    public GameObject cam;

    [Header("Gaze Object Logging")]
    public float gazeObjectLogInterval = 1.0f;
    private float nextGazeObjectLogTime = 0f;

    private string leftGazeObjectName = "Nothing";
    private string leftGazeObjectLayer = "None";
    private Vector3 leftGazeHitPoint = Vector3.zero;
    private float leftGazeHitDistance = -1f;
    private bool leftGazeHitValid = false;

    private string rightGazeObjectName = "Nothing";
    private string rightGazeObjectLayer = "None";
    private Vector3 rightGazeHitPoint = Vector3.zero;
    private float rightGazeHitDistance = -1f;
    private bool rightGazeHitValid = false;

    [Header("CSV Flush")]
    public float flushInterval = 1.0f;
    private float nextFlushTime = 0f;

    private string currentSceneName;
    private string sessionTimestamp;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[TrackingManager] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        id = ParticipantManager.Instance != null
            ? ParticipantManager.Instance.ParticipantId
            : "NoParticipantID";

        sessionTimestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log($"[TrackingManager] Awake complete. ID={id}, Timestamp={sessionTimestamp}");
    }

    void Start()
    {
        Debug.Log("[TrackingManager] Start called.");

        CloseAllWriters();
        currentSceneName = SceneManager.GetActiveScene().name;
        OpenAllWriters();
        InitDevices();

        RefreshGazeReferences();
    }
    private static void CaptureCsvTimestamp(out string dateTimeLocal, out float unscaledTime)
    {
        dateTimeLocal = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        unscaledTime = Time.unscaledTime;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[TrackingManager] Scene loaded: {scene.name}");

        CloseAllWriters();

        currentSceneName = scene.name;
        OpenAllWriters();
        InitDevices();
        RefreshGazeReferences();

        Debug.Log($"[TrackingManager] New CSVs opened for scene: {currentSceneName}");
    }

    private void OpenAllWriters()
    {
        Debug.Log("[TrackingManager] Opening CSV writers.");

        headWriter = CreateWriter("Head",
            "SceneName,DateTimeLocal,UnscaledTime,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,VelX,VelY,VelZ,AngVelX,AngVelY,AngVelZ");

        controllerWriter = CreateWriter("Controllers",
            "SceneName,DateTimeLocal,UnscaledTime,Hand,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,Trigger,Grip,PrimaryButton,SecondaryButton");

        handWriter = CreateWriter("Hands",
            "SceneName,DateTimeLocal,UnscaledTime,Hand,Joint,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW");

        eyeWriter = CreateWriter("Eye",
            "SceneName,DateTimeLocal,UnscaledTime," +
            "LeftGazeValid,LeftGazePosX,LeftGazePosY,LeftGazePosZ,LeftGazeRotX,LeftGazeRotY,LeftGazeRotZ,LeftGazeRotW," +
            "RightGazeValid,RightGazePosX,RightGazePosY,RightGazePosZ,RightGazeRotX,RightGazeRotY,RightGazeRotZ,RightGazeRotW," +
            "LeftPupilDiameter,LeftPupilPosX,LeftPupilPosY," +
            "RightPupilDiameter,RightPupilPosX,RightPupilPosY," +
            "LeftOpenness,LeftSqueeze,LeftWide," +
            "RightOpenness,RightSqueeze,RightWide");

        gazeObjectWriter = CreateWriter("GazeObject",
            "SceneName,DateTimeLocal,UnscaledTime," +
            "LeftLookingAt,LeftLayer,LeftHitValid,LeftHitPointX,LeftHitPointY,LeftHitPointZ,LeftHitDistance," +
            "RightLookingAt,RightLayer,RightHitValid,RightHitPointX,RightHitPointY,RightHitPointZ,RightHitDistance");
    }

    private StreamWriter CreateWriter(string dataType, string header)
    {
        id = ParticipantManager.Instance != null
            ? ParticipantManager.Instance.ParticipantId
            : "NoParticipantID";

        string folderPath = Path.Combine(Application.persistentDataPath, $"{id}/TRACKING_Logs");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log($"[TrackingManager] Created folder: {folderPath}");
        }

        string fileName = $"{currentSceneName}_{dataType}_{sessionTimestamp}.csv";
        string path = Path.Combine(folderPath, fileName);

        var writer = new StreamWriter(path);
        writer.WriteLine(header);
        writer.Flush();

        Debug.Log($"[TrackingManager] CSV created: {path}");
        return writer;
    }

    private void InitDevices()
    {
        Debug.Log("[TrackingManager] Initializing XR devices.");

        headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        Debug.Log($"[TrackingManager] Head valid: {headDevice.isValid}");
        Debug.Log($"[TrackingManager] Left controller valid: {leftController.isValid}");
        Debug.Log($"[TrackingManager] Right controller valid: {rightController.isValid}");

        var handSubsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(handSubsystems);

        handSubsystem = null;

        foreach (var s in handSubsystems)
        {
            Debug.Log($"[TrackingManager] Hand subsystem found. Running={s.running}");

            if (s.running)
            {
                handSubsystem = s;
                break;
            }
        }

        if (handSubsystem == null)
            Debug.LogWarning("[TrackingManager] No running XRHandSubsystem found.");
        else
            Debug.Log("[TrackingManager] XRHandSubsystem initialized.");
    }

    void Update()
    {
        RecordHead();
        RecordControllers();
        RecordHands();
        RecordEye();

        if (Time.unscaledTime >= nextFlushTime)
        {
            nextFlushTime = Time.unscaledTime + flushInterval;
            FlushAllWriters();
        }
    }

    private void RecordHead()
    {
        if (headWriter == null) return;

        if (!headDevice.isValid)
            headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);

        if (!headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) ||
            !headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            return;

        headDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 vel);
        headDevice.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 angVel);
        
        CaptureCsvTimestamp(
            out string dateTimeLocal,
            out float unscaledTime
        );
        
        headWriter.WriteLine(
            $"{currentSceneName}," +
            $"{dateTimeLocal}," +
            $"{unscaledTime:F6}," +
            $"{pos.x:F6},{pos.y:F6},{pos.z:F6}," +
            $"{rot.x:F6},{rot.y:F6},{rot.z:F6},{rot.w:F6}," +
            $"{vel.x:F6},{vel.y:F6},{vel.z:F6}," +
            $"{angVel.x:F6},{angVel.y:F6},{angVel.z:F6}"
        );
    }

    private void RecordControllers()
    {
        if (controllerWriter == null)
            return;

        CaptureCsvTimestamp(out string dateTimeLocal, out float unscaledTime);

        RecordController(ref leftController, "Left", XRNode.LeftHand, dateTimeLocal, unscaledTime);
        RecordController(ref rightController, "Right", XRNode.RightHand, dateTimeLocal,unscaledTime);
    }

    private void RecordController(ref InputDevice device, string label, XRNode node, string dateTimeLocal, float unscaledTime)
    {
        if (!device.isValid)
        {
            device = InputDevices.GetDeviceAtXRNode(node);

            if (!device.isValid)
                return;
        }

        device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos);
        device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot);
        device.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
        device.TryGetFeatureValue(CommonUsages.grip, out float grip);
        device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary);
        device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary);

        controllerWriter.WriteLine(
            $"{currentSceneName}," +
            $"{dateTimeLocal}," +
            $"{unscaledTime:F6}," +
            $"{label},"+
            $"{pos.x:F6},{pos.y:F6},{pos.z:F6}," +
            $"{rot.x:F6},{rot.y:F6},{rot.z:F6},{rot.w:F6}," +
            $"{trigger:F3},{grip:F3},{primary},{secondary}");
    }

    private void RecordHands()
    {
        if (handWriter == null)
            return;

        if (handSubsystem == null || !handSubsystem.running)
            return;

        CaptureCsvTimestamp(out string dateTimeLocal, out float unscaledTime);

        RecordHand(handSubsystem.leftHand, "Left", dateTimeLocal, unscaledTime);
        RecordHand(handSubsystem.rightHand, "Right", dateTimeLocal, unscaledTime);
    }

    private void RecordHand(XRHand hand, string label, string dateTimeLocal, float unscaledTime)
    {
        if (!hand.isTracked) return;

        for (int i = XRHandJointID.BeginMarker.ToIndex();
             i < XRHandJointID.EndMarker.ToIndex(); i++)
        {
            var jointId = XRHandJointIDUtility.FromIndex(i);

            if (hand.GetJoint(jointId).TryGetPose(out Pose pose))
            {
                handWriter.WriteLine(
                    $"{currentSceneName}," +
                    $"{dateTimeLocal}," +
                    $"{unscaledTime:F6}," +
                    $"{label},{jointId},"+
                    $"{pose.position.x:F6},{pose.position.y:F6},{pose.position.z:F6}," +
                    $"{pose.rotation.x:F6},{pose.rotation.y:F6},{pose.rotation.z:F6},{pose.rotation.w:F6}");
            }
        }
    }

    private void RecordEye()
    {
        if (eyeWriter == null) return;

        WriteEyeToCSV();

        if (Time.unscaledTime >= nextEyeSend)
        {
            nextEyeSend = Time.unscaledTime + eyeSendInterval;
            SendEyeToServer();
        }
    }

    private void RecordGazeObject(string dateTimeLocal, float unscaledTime)
    {
        if (gazeObjectWriter == null)
            return;

        gazeObjectWriter.WriteLine(
            $"{currentSceneName}," +
            $"{dateTimeLocal}," +
            $"{unscaledTime:F6}," +
            $"{EscapeCSV(leftGazeObjectName)}," +
            $"{EscapeCSV(leftGazeObjectLayer)}," +
            $"{leftGazeHitValid}," +
            $"{leftGazeHitPoint.x:F6}," +
            $"{leftGazeHitPoint.y:F6}," +
            $"{leftGazeHitPoint.z:F6}," +
            $"{leftGazeHitDistance:F6}," +
            $"{EscapeCSV(rightGazeObjectName)}," +
            $"{EscapeCSV(rightGazeObjectLayer)}," +
            $"{rightGazeHitValid}," +
            $"{rightGazeHitPoint.x:F6}," +
            $"{rightGazeHitPoint.y:F6}," +
            $"{rightGazeHitPoint.z:F6}," +
            $"{rightGazeHitDistance:F6}"
        );
    }

    private string EscapeCSV(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        return value;
    }

    private void WriteEyeToCSV()
    {
        try
        {
            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] gazes);
            XR_HTC_eye_tracker.Interop.GetEyePupilData(out XrSingleEyePupilDataHTC[] pupils);
            XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out XrSingleEyeGeometricDataHTC[] geo);

            if (gazes == null || pupils == null || geo == null) return;
            if (gazes.Length < 2 || pupils.Length < 2 || geo.Length < 2) return;

            var leftG = gazes[0];
            var rightG = gazes[1];
            var leftP = pupils[0];
            var rightP = pupils[1];
            var leftGe = geo[0];
            var rightGe = geo[1];

            UpdateGazePoseObjectsAndRaycast(leftG, rightG);

            float lPupilX = leftP.isPositionValid ? leftP.pupilPosition.x : -1f;
            float lPupilY = leftP.isPositionValid ? leftP.pupilPosition.y : -1f;
            float rPupilX = rightP.isPositionValid ? rightP.pupilPosition.x : -1f;
            float rPupilY = rightP.isPositionValid ? rightP.pupilPosition.y : -1f;
            
            CaptureCsvTimestamp(
                out string dateTimeLocal,
                out float unscaledTime
            );
            
            eyeWriter.WriteLine(
                $"{currentSceneName}," +
                $"{dateTimeLocal}," +
                $"{unscaledTime:F6}," +
                $"{leftG.isValid}," +
                $"{leftG.gazePose.position.x:F6},{leftG.gazePose.position.y:F6},{leftG.gazePose.position.z:F6}," +
                $"{leftG.gazePose.orientation.x:F6},{leftG.gazePose.orientation.y:F6},{leftG.gazePose.orientation.z:F6},{leftG.gazePose.orientation.w:F6}," +
                $"{rightG.isValid}," +
                $"{rightG.gazePose.position.x:F6},{rightG.gazePose.position.y:F6},{rightG.gazePose.position.z:F6}," +
                $"{rightG.gazePose.orientation.x:F6},{rightG.gazePose.orientation.y:F6},{rightG.gazePose.orientation.z:F6},{rightG.gazePose.orientation.w:F6}," +
                $"{(leftP.isDiameterValid ? leftP.pupilDiameter : -1f):F4},{lPupilX:F4},{lPupilY:F4}," +
                $"{(rightP.isDiameterValid ? rightP.pupilDiameter : -1f):F4},{rPupilX:F4},{rPupilY:F4}," +
                $"{(leftGe.isValid ? leftGe.eyeOpenness : -1f):F4},{(leftGe.isValid ? leftGe.eyeSqueeze : -1f):F4},{(leftGe.isValid ? leftGe.eyeWide : -1f):F4}," +
                $"{(rightGe.isValid ? rightGe.eyeOpenness : -1f):F4},{(rightGe.isValid ? rightGe.eyeSqueeze : -1f):F4},{(rightGe.isValid ? rightGe.eyeWide : -1f):F4}");

            RecordGazeObject(dateTimeLocal, unscaledTime);

        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TrackingManager] Eye CSV write failed: {e.Message}");
        }
    }

    private void UpdateGazePoseObjectsAndRaycast(
        XrSingleEyeGazeDataHTC leftG,
        XrSingleEyeGazeDataHTC rightG)
    {
        if (leftG.isValid && leftGazePoseObject != null)
        {
            Vector3 gazePos = leftG.gazePose.position.ToUnityVector();

            Quaternion gazeRot = leftG.gazePose.orientation.ToUnityQuaternion();

            leftGazePoseObject.transform.SetPositionAndRotation(
                gazePos,
                gazeRot
            );

            UpdateSingleEyeHit(
                leftGazePoseObject.transform.position,
                leftGazePoseObject.transform.forward,
                true
            );
        }
        else
        {
            SetLeftNoHit();
        }

        if (rightG.isValid && rightGazePoseObject != null)
        {
            Vector3 gazePos = rightG.gazePose.position.ToUnityVector();

            Quaternion gazeRot = rightG.gazePose.orientation.ToUnityQuaternion();

            rightGazePoseObject.transform.SetPositionAndRotation(
                gazePos,
                gazeRot
            );

            UpdateSingleEyeHit(
                rightGazePoseObject.transform.position,
                rightGazePoseObject.transform.forward,
                false
            );
        }
        else
        {
            SetRightNoHit();
        }
    }

    private void UpdateSingleEyeHit(Vector3 origin, Vector3 direction, bool isLeftEye)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            gazeRayDistance,
            gazeRaycastLayers.value,
            QueryTriggerInteraction.Collide
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (!IsValidGazeHit(hit))
                continue;

            GameObject hitObject = hit.collider.gameObject;
            string layerName = LayerMask.LayerToName(hitObject.layer);

            if (isLeftEye)
            {
                leftGazeObjectName = hitObject.name;
                leftGazeObjectLayer = layerName;
                leftGazeHitPoint = hit.point;
                leftGazeHitDistance = hit.distance;
                leftGazeHitValid = true;
            }
            else
            {
                rightGazeObjectName = hitObject.name;
                rightGazeObjectLayer = layerName;
                rightGazeHitPoint = hit.point;
                rightGazeHitDistance = hit.distance;
                rightGazeHitValid = true;
            }

            Debug.Log($"[{(isLeftEye ? "LEFT" : "RIGHT")} GAZE] Hit: {hitObject.name}");

            return;
        }

        if (isLeftEye)
            SetLeftNoHit();
        else
            SetRightNoHit();
    }

    private bool IsValidGazeHit(RaycastHit hit)
    {
        Collider col = hit.collider;
        if (col == null) return false;
        if (!col.enabled) return false;

        GameObject hitObject = col.gameObject;

        if (!hitObject.activeInHierarchy) return false;
        if ((gazeRaycastLayers.value & (1 << hitObject.layer)) == 0) return false;

        if (leftGazePoseObject != null)
        {
            if (hitObject == leftGazePoseObject) return false;
            if (hitObject.transform.IsChildOf(leftGazePoseObject.transform)) return false;
        }

        if (rightGazePoseObject != null)
        {
            if (hitObject == rightGazePoseObject) return false;
            if (hitObject.transform.IsChildOf(rightGazePoseObject.transform)) return false;
        }

        return true;
    }

    private void SetLeftNoHit()
    {
        leftGazeObjectName = "Nothing";
        leftGazeObjectLayer = "None";
        leftGazeHitPoint = Vector3.zero;
        leftGazeHitDistance = -1f;
        leftGazeHitValid = false;
    }

    private void SetRightNoHit()
    {
        rightGazeObjectName = "Nothing";
        rightGazeObjectLayer = "None";
        rightGazeHitPoint = Vector3.zero;
        rightGazeHitDistance = -1f;
        rightGazeHitValid = false;
    }

    private void SendEyeToServer()
    {
        try
        {
            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] gazes);
            XR_HTC_eye_tracker.Interop.GetEyePupilData(out XrSingleEyePupilDataHTC[] pupils);
            XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out XrSingleEyeGeometricDataHTC[] geo);

            if (gazes == null || pupils == null || geo == null) return;
            if (gazes.Length < 2 || pupils.Length < 2 || geo.Length < 2) return;

            var leftG = gazes[0];
            var rightG = gazes[1];
            var leftP = pupils[0];
            var rightP = pupils[1];
            var leftGe = geo[0];
            var rightGe = geo[1];

            EyePacket packet = new EyePacket
            {
                scene_name = currentSceneName,
                timestamp = Time.unscaledTime,

                left_gaze_valid = leftG.isValid,
                left_gaze_pos = new Vector3(leftG.gazePose.position.x, leftG.gazePose.position.y, leftG.gazePose.position.z),
                left_gaze_rot = new Quaternion(leftG.gazePose.orientation.x, leftG.gazePose.orientation.y, leftG.gazePose.orientation.z, leftG.gazePose.orientation.w),

                right_gaze_valid = rightG.isValid,
                right_gaze_pos = new Vector3(rightG.gazePose.position.x, rightG.gazePose.position.y, rightG.gazePose.position.z),
                right_gaze_rot = new Quaternion(rightG.gazePose.orientation.x, rightG.gazePose.orientation.y, rightG.gazePose.orientation.z, rightG.gazePose.orientation.w),

                left_pupil_diameter = leftP.isDiameterValid ? leftP.pupilDiameter : -1f,
                left_pupil_position = leftP.isPositionValid ? new Vector2(leftP.pupilPosition.x, leftP.pupilPosition.y) : new Vector2(-1, -1),

                right_pupil_diameter = rightP.isDiameterValid ? rightP.pupilDiameter : -1f,
                right_pupil_position = rightP.isPositionValid ? new Vector2(rightP.pupilPosition.x, rightP.pupilPosition.y) : new Vector2(-1, -1),

                left_eye_openness = leftGe.isValid ? leftGe.eyeOpenness : -1f,
                left_eye_squeeze = leftGe.isValid ? leftGe.eyeSqueeze : -1f,
                left_eye_wide = leftGe.isValid ? leftGe.eyeWide : -1f,

                right_eye_openness = rightGe.isValid ? rightGe.eyeOpenness : -1f,
                right_eye_squeeze = rightGe.isValid ? rightGe.eyeSqueeze : -1f,
                right_eye_wide = rightGe.isValid ? rightGe.eyeWide : -1f
            };

            string json = JsonUtility.ToJson(packet);
            StartCoroutine(PostRequest(json));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TrackingManager] Eye server send failed: {e.Message}");
        }
    }

    IEnumerator PostRequest(string json)
    {
        UnityWebRequest req = new UnityWebRequest(serverURL, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning($"[TrackingManager] Server error: {req.error}");
    }

    private void RefreshGazeReferences()
    {
        cam = GameObject.Find("Main Camera");
        leftGazePoseObject = GameObject.Find("GazeTargetLeft");
        rightGazePoseObject = GameObject.Find("GazeTargetRight");

        SetLeftNoHit();
        SetRightNoHit();
        nextGazeObjectLogTime = 0f;

        Debug.Log(
            $"[TrackingManager] Refreshed gaze references for {currentSceneName}: " +
            $"Camera={(cam != null ? cam.name : "NULL")}, " +
            $"Left={(leftGazePoseObject != null ? leftGazePoseObject.name : "NULL")}, " +
            $"Right={(rightGazePoseObject != null ? rightGazePoseObject.name : "NULL")}"
        );
    }

    private void FlushAllWriters()
    {
        headWriter?.Flush();
        controllerWriter?.Flush();
        handWriter?.Flush();
        eyeWriter?.Flush();
        gazeObjectWriter?.Flush();
    }

    private void CloseAllWriters()
    {
        Debug.Log("[TrackingManager] Closing CSV writers.");

        headWriter?.Flush();
        headWriter?.Close();
        headWriter = null;

        controllerWriter?.Flush();
        controllerWriter?.Close();
        controllerWriter = null;

        handWriter?.Flush();
        handWriter?.Close();
        handWriter = null;

        eyeWriter?.Flush();
        eyeWriter?.Close();
        eyeWriter = null;

        gazeObjectWriter?.Flush();
        gazeObjectWriter?.Close();
        gazeObjectWriter = null;
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
            FlushAllWriters();
    }

    void OnDisable()
    {
        FlushAllWriters();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CloseAllWriters();
    }

    void OnApplicationQuit()
    {
        CloseAllWriters();
        Debug.Log("[TrackingManager] All CSV recorders closed.");
    }

    [System.Serializable]
    public class EyePacket
    {
        public string scene_name;
        public float timestamp;

        public bool left_gaze_valid;
        public Vector3 left_gaze_pos;
        public Quaternion left_gaze_rot;

        public bool right_gaze_valid;
        public Vector3 right_gaze_pos;
        public Quaternion right_gaze_rot;

        public float left_pupil_diameter;
        public Vector2 left_pupil_position;

        public float right_pupil_diameter;
        public Vector2 right_pupil_position;

        public float left_eye_openness;
        public float left_eye_squeeze;
        public float left_eye_wide;

        public float right_eye_openness;
        public float right_eye_squeeze;
        public float right_eye_wide;
    }
}