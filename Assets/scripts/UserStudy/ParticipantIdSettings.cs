using UnityEngine;

[CreateAssetMenu(fileName = "ParticipantIdSettings", menuName = "CoralVR/Participant ID Settings")]
public class ParticipantIdSettings : ScriptableObject
{
    [Header("ID format")] [Tooltip("Prefix for the participant ID string, e.g., 'participant_' or 'P'")]
    public string idPrefix = "participant_";

    [Tooltip("How many digits to pad the numeric part with.")]
    public int padDigits = 3;

    [Header("Counter storage")] [Tooltip("PlayerPrefs key used to persist the next numeric ID.")]
    public string playerPrefsKey = "CoralVR.Participant.NextId";

    [Tooltip("Starting numeric value if none is stored yet.")]
    public int startAt = 1;

    [Tooltip("If enabled, the SO initializes the counter when first loaded if the key doesn't exist.")]
    public bool createIfMissing = true;

    private bool _loaded = false;
    private int _nextNumericId; // in-memory cache

    /// <summary>
    /// Ensure in-memory cache is populated from PlayerPrefs.
    /// </summary>
    public void Load()
    {
        if (_loaded) return;

        if (PlayerPrefs.HasKey(playerPrefsKey))
        {
            _nextNumericId = PlayerPrefs.GetInt(playerPrefsKey, startAt);
        }
        else
        {
            _nextNumericId = startAt;
            if (createIfMissing)
            {
                PlayerPrefs.SetInt(playerPrefsKey, _nextNumericId);
                PlayerPrefs.Save();
            }
        }

        _loaded = true;
    }

    /// <summary>
    /// Returns the current participant ID string, e.g., "participant_001".
    /// </summary>
    public string GetCurrentParticipantId()
    {
        Load();
        return $"{idPrefix}{_nextNumericId.ToString().PadLeft(padDigits, '0')}";
    }

    /// <summary>
    /// Returns the numeric part that will be used next.
    /// </summary>
    public int GetCurrentNumericId()
    {
        Load();
        return _nextNumericId;
    }

    /// <summary>
    /// Increments the counter and persists it.
    /// Call this once at the end of a session.
    /// </summary>
    public void IncrementAndSave()
    {
        Load();
        _nextNumericId++;
        PlayerPrefs.SetInt(playerPrefsKey, _nextNumericId);
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// Resets the counter to an explicit value and persists it.
    /// </summary>
    public void ResetTo(int numericValue)
    {
        _nextNumericId = Mathf.Max(0, numericValue);
        PlayerPrefs.SetInt(playerPrefsKey, _nextNumericId);
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // Convenience in the inspector
#if UNITY_EDITOR
    [ContextMenu("Print Current ID to Console")]
    private void _Ctx_Print()
    {
        Debug.Log($"[ParticipantIdSettings] Current ID: {GetCurrentParticipantId()} (numeric {_nextNumericId})");
    }

    [ContextMenu("Increment & Save (Test)")]
    private void _Ctx_Increment()
    {
        IncrementAndSave();
        Debug.Log($"[ParticipantIdSettings] Incremented. New current: {GetCurrentParticipantId()}");
    }
#endif
}