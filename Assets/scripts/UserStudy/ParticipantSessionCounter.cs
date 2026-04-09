using UnityEngine;

/// <summary>
/// Increments the participant ID exactly once at the end of the app/session.
/// </summary>
public class ParticipantSessionCounter : MonoBehaviour
{
    [Tooltip("Reference to the ParticipantIdSettings ScriptableObject.")]
    public ParticipantIdSettings settings;

    [Tooltip("If true, also increment when stopping Play Mode in the Editor.")]
    public bool incrementInEditor = true;

    private bool _incremented = false;

    void Awake()
    {
        if (settings == null)
            Debug.LogError("[ParticipantSessionCounter] Settings reference is not set.");
        else
            settings.Load(); // ensure current ID is ready for others to read
    }

    void OnApplicationQuit()
    {
        TryIncrement("OnApplicationQuit");
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        // When stopping Play Mode, OnApplicationQuit is called; OnDestroy can fire during domain reload.
        // As a safety net (Editor only), optionally increment here if OnApplicationQuit didn’t.
        if (incrementInEditor && !_incremented && !Application.isPlaying)
        {
            TryIncrement("OnDestroy(Editor)");
        }
#endif
    }

    private void TryIncrement(string source)
    {
        if (_incremented || settings == null) return;

        settings.IncrementAndSave();
        _incremented = true;
        Debug.Log(
            $"[ParticipantSessionCounter] Incremented participant ID at session end ({source}). New current: {settings.GetCurrentParticipantId()}");
    }
}