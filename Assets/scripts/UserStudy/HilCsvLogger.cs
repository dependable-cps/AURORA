//  -----------------------------------------------------------------------
//  <copyright file="HilCsvLogger.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HilCsvLogger : MonoBehaviour
{
    public static HilCsvLogger Instance { get; private set; }

    [Header("Where to save")]
    [Tooltip("Editor only: if true, write under Assets/Data_HIL; otherwise use persistentDataPath.")]
    public bool writeToAssetsFolderInEditor = true;

    [Tooltip("Editor path: Assets/<assetsRootFolder>/<participant>/<scene>/...")]
    public string assetsRootFolder = "Data_HIL";

    [Tooltip("Builds path: <persistentDataPath>/<persistentRootFolder>/<participant>/<scene>/...")]
    public string persistentRootFolder = "CoralVR_HIL";

    [Header("File naming")]
    [Tooltip("Single daily CSV, else a per-session file.")]
    public bool dailyFile = true;

    [Tooltip("CSV base name prefix")]
    public string filePrefix = "hil";

    [Tooltip("Add UTC timestamp to per-session file names")]
    public bool includeUtcStampForSession = true;

    private string _filePath;
    private readonly object _fileLock = new object();

    private string _currentParticipantId = "participant_000";
    private string _currentSceneName = "UnknownScene";
    private string _currentSessionId;
    private int _feedbackIndex;

    // CSV columns: pure user feedback data for later RL training
    private const string CSV_HEADER =
        "timestamp_iso,session_id,participant_id,event_type,feedback_index," +
        "feedback_type,rating_value,raw_utterance," +
        "digit_recall_shown,digit_recall_spoken,digit_recall_correct," +
        "session_time_sec,platform,device_model";

    // ---- Lifecycle ----

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _currentSessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
        _feedbackIndex = 0;

        TryAutoSceneName();
        SetupFilePath();
    }

    // ---- Public Configuration ----

    public void ConfigureFolders(string participantId, string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(participantId)) _currentParticipantId = participantId.Trim();
        if (!string.IsNullOrWhiteSpace(sceneName))     _currentSceneName = sceneName.Trim();
        SetupFilePath();
    }

    public string GetCurrentFilePath() => _filePath;
    public string GetSessionId() => _currentSessionId;
    public string GetParticipantId() => _currentParticipantId;

    // ---- Logging Methods ----

    /// <summary>
    /// Log a cognitive state feedback rating from the user.
    /// feedbackType: "FMS", "CPL", "CML", or "WM_rating"
    /// ratingValue: the numeric rating (0-10 scale, -1 = no response)
    /// rawUtterance: the raw speech text captured
    /// </summary>
    public void LogCognitiveStateFeedback(string feedbackType, int ratingValue, string rawUtterance)
    {
        EnsureCsvExists();
        _feedbackIndex++;

        WriteRow(
            Escape(DateTime.UtcNow.ToString("o")),
            Escape(_currentSessionId),
            Escape(_currentParticipantId),
            "cognitive_feedback",
            _feedbackIndex.ToString(),
            Escape(feedbackType),
            ratingValue.ToString(),
            Escape(rawUtterance),
            "", "", "",
            Time.time.ToString("0.#"),
            Escape(Application.platform.ToString()),
            Escape(SystemInfo.deviceModel)
        );

        Debug.Log($"[HIL] Logged {feedbackType} = {ratingValue} (utterance: \"{rawUtterance}\")");
    }

    /// <summary>
    /// Log a working memory digit recall result.
    /// digitsShown: the digits displayed to the user
    /// digitsSpoken: the digits the user spoke back
    /// correct: whether the recall was correct
    /// </summary>
    public void LogDigitRecall(string digitsShown, string digitsSpoken, bool correct)
    {
        EnsureCsvExists();
        _feedbackIndex++;

        WriteRow(
            Escape(DateTime.UtcNow.ToString("o")),
            Escape(_currentSessionId),
            Escape(_currentParticipantId),
            "digit_recall",
            _feedbackIndex.ToString(),
            "WM_recall",
            correct ? "1" : "0",
            "",
            Escape(digitsShown),
            Escape(digitsSpoken),
            correct ? "1" : "0",
            Time.time.ToString("0.#"),
            Escape(Application.platform.ToString()),
            Escape(SystemInfo.deviceModel)
        );

        Debug.Log($"[HIL] Digit recall: shown={digitsShown}, spoken={digitsSpoken}, correct={correct}");
    }

    /// <summary>
    /// Log a generic session event (start, end, etc.)
    /// </summary>
    public void LogSessionEvent(string eventType, string details = "")
    {
        EnsureCsvExists();
        _feedbackIndex++;

        WriteRow(
            Escape(DateTime.UtcNow.ToString("o")),
            Escape(_currentSessionId),
            Escape(_currentParticipantId),
            Escape(eventType),
            _feedbackIndex.ToString(),
            "", "",
            Escape(details),
            "", "", "",
            Time.time.ToString("0.#"),
            Escape(Application.platform.ToString()),
            Escape(SystemInfo.deviceModel)
        );
    }

    // ---- Internal Helpers ----

    private static string Escape(string v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        v = v.Replace("\"", "\"\"");
        if (v.Contains(",") || v.Contains("\n") || v.Contains("\""))
            return $"\"{v}\"";
        return v;
    }

    private void WriteRow(params string[] cols)
    {
        lock (_fileLock)
        {
            using (var sw = new StreamWriter(_filePath, append: true))
                sw.WriteLine(string.Join(",", cols));
        }
    }

    private void TryAutoSceneName()
    {
        try
        {
            _currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(_currentSceneName)) _currentSceneName = "UnknownScene";
        }
        catch { _currentSceneName = "UnknownScene"; }
    }

    private void SetupFilePath()
    {
        string baseDir;

#if UNITY_EDITOR
        if (writeToAssetsFolderInEditor)
            baseDir = Path.Combine(Application.dataPath, assetsRootFolder);
        else
            baseDir = Path.Combine(Application.persistentDataPath, persistentRootFolder);
#else
        baseDir = Path.Combine(Application.persistentDataPath, persistentRootFolder);
#endif

        var finalDir = Path.Combine(baseDir, _currentParticipantId, _currentSceneName);
        Directory.CreateDirectory(finalDir);

        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string fileName;
        if (dailyFile)
        {
            fileName = $"{filePrefix}_{date}.csv";
        }
        else
        {
            var stamp = includeUtcStampForSession ? "_" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ") : "";
            fileName = $"{filePrefix}_{date}{stamp}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.csv";
        }

        _filePath = Path.Combine(finalDir, fileName);
        CreateCsvAndHeader();

        Debug.Log($"[HIL] Logging to: {_filePath}");
    }

    private void EnsureCsvExists()
    {
        if (string.IsNullOrEmpty(_filePath))
            SetupFilePath();
        else if (!File.Exists(_filePath))
            CreateCsvAndHeader();
    }

    private void CreateCsvAndHeader()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var needHeader = !File.Exists(_filePath) || new FileInfo(_filePath).Length == 0;
        if (needHeader)
        {
            using (var sw = new StreamWriter(_filePath, append: true))
                sw.WriteLine(CSV_HEADER);
        }

#if UNITY_EDITOR
        if (writeToAssetsFolderInEditor)
            AssetDatabase.Refresh();
#endif
    }

#if UNITY_EDITOR
    [ContextMenu("Open Log Folder")]
    private void OpenLogFolder()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Application.OpenURL("file:///" + dir.Replace("\\", "/"));
    }
#endif
}
