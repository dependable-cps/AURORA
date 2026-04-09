//  -----------------------------------------------------------------------
//  <copyright file="SpeechRecogniser.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class SpeechRecogniser : MonoBehaviour
{
    public static SpeechRecogniser Instance { get; private set; }

    protected DictationRecognizer dictationRecognizer;

    [Tooltip("Keeps the most recent N finalized utterances.")]
    public int maxItems = 200;

    public List<string> list = new List<string>();

    // ---- Events ----
    // Fired when a numeric rating (0-10) is detected in speech.
    // Parameters: (rawUtterance, parsedRating)
    public event Action<string, int> OnRatingDetected;

    // Fired when digit recall speech is detected (user reciting digits).
    // Parameter: (rawUtterance)
    public event Action<string> OnDigitRecallDetected;

    // Fired on every finalized dictation result.
    // Parameters: (rawText, confidence)
    public event Action<string, ConfidenceLevel> OnUtteranceFinalized;

    // ---- State ----
    // Set by UserFeedback to tell the recogniser what kind of response to expect.
    // "FMS", "CPL", "CML", "WM_rating", "digit_recall", or "" (none)
    [HideInInspector] public string expectedResponseType = "";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---- Public API ----

    public void StartDictationRequest()
    {
        if (PhraseRecognitionSystem.Status == SpeechSystemStatus.Running)
            PhraseRecognitionSystem.Shutdown();

        StartDictationEngine();
    }

    public void StopDictationRequest()
    {
        if (PhraseRecognitionSystem.Status != SpeechSystemStatus.Running)
            PhraseRecognitionSystem.Restart();

        CloseDictationEngine();
    }

    private void OnApplicationQuit() => CloseDictationEngine();
    private void OnDestroy() => CloseDictationEngine();

    private void StartDictationEngine()
    {
        if (dictationRecognizer != null &&
            dictationRecognizer.Status == SpeechSystemStatus.Running)
            return;

        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.DictationHypothesis += OnDictationHypothesis;
        dictationRecognizer.DictationResult += OnDictationResult;
        dictationRecognizer.DictationComplete += OnDictationComplete;
        dictationRecognizer.DictationError += OnDictationError;

        dictationRecognizer.Start();
    }

    public void CloseDictationEngine()
    {
        if (dictationRecognizer == null) return;

        try
        {
            dictationRecognizer.DictationHypothesis -= OnDictationHypothesis;
            dictationRecognizer.DictationResult -= OnDictationResult;
            dictationRecognizer.DictationComplete -= OnDictationComplete;
            dictationRecognizer.DictationError -= OnDictationError;

            if (dictationRecognizer.Status == SpeechSystemStatus.Running)
                dictationRecognizer.Stop();

            dictationRecognizer.Dispose();
        }
        finally
        {
            dictationRecognizer = null;
        }
    }

    // ---- Dictation Callbacks ----

    private void OnDictationHypothesis(string text)
    {
        // Optional: live captions
    }

    private void OnDictationResult(string text, ConfidenceLevel confidence)
    {
        Debug.Log($"[Dictation] Result ({confidence}): {text}");

        if (string.IsNullOrWhiteSpace(text)) return;

        list.Add(text);
        if (list.Count > maxItems) list.RemoveAt(0);

        OnUtteranceFinalized?.Invoke(text, confidence);

        // Route based on what we're expecting
        if (expectedResponseType == "digit_recall")
        {
            OnDigitRecallDetected?.Invoke(text);
        }
        else if (!string.IsNullOrEmpty(expectedResponseType))
        {
            // Try to parse a numeric rating from the speech
            int rating = ParseRating(text);
            if (rating >= 0)
            {
                OnRatingDetected?.Invoke(text, rating);
            }
            else
            {
                Debug.LogWarning($"[SpeechRecogniser] Could not parse rating from: \"{text}\"");
            }
        }
    }

    private void OnDictationComplete(DictationCompletionCause cause)
    {
        switch (cause)
        {
            case DictationCompletionCause.TimeoutExceeded:
            case DictationCompletionCause.PauseLimitExceeded:
            case DictationCompletionCause.Canceled:
            case DictationCompletionCause.Complete:
                CloseDictationEngine();
                StartDictationEngine();
                break;

            case DictationCompletionCause.UnknownError:
            case DictationCompletionCause.AudioQualityFailure:
            case DictationCompletionCause.MicrophoneUnavailable:
            case DictationCompletionCause.NetworkFailure:
                CloseDictationEngine();
                break;
        }
    }

    private void OnDictationError(string error, int hresult)
    {
        Debug.LogWarning($"[Dictation] Error: {error} (0x{hresult:X})");
    }

    // ---- Rating Parser ----

    /// <summary>
    /// Extracts a numeric rating (0-10) from spoken text.
    /// Handles both digit forms ("7") and word forms ("seven").
    /// Returns -1 if no valid rating found.
    /// </summary>
    public static int ParseRating(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return -1;

        text = text.Trim().ToLowerInvariant();

        // Word-to-number mapping
        var wordMap = new Dictionary<string, int>
        {
            { "zero",  0 }, { "one",   1 }, { "two",   2 }, { "three", 3 },
            { "four",  4 }, { "five",  5 }, { "six",   6 }, { "seven", 7 },
            { "eight", 8 }, { "nine",  9 }, { "ten",  10 }
        };

        // Check word forms first (exact match or contained)
        foreach (var kvp in wordMap)
        {
            if (text.Contains(kvp.Key))
                return kvp.Value;
        }

        // Try to find a number 0-10 in the text
        var match = Regex.Match(text, @"\b(\d{1,2})\b");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int num) && num >= 0 && num <= 10)
            return num;

        return -1;
    }

    /// <summary>
    /// Extracts digits from spoken text for digit recall comparison.
    /// "1 3 5 7 9" or "one three five seven nine" -> "13579"
    /// </summary>
    public static string ExtractDigits(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        text = text.Trim().ToLowerInvariant();

        // Replace word digits with numeric digits
        var wordMap = new Dictionary<string, string>
        {
            { "zero", "0" }, { "one", "1" }, { "two", "2" }, { "three", "3" },
            { "four", "4" }, { "five", "5" }, { "six", "6" }, { "seven", "7" },
            { "eight", "8" }, { "nine", "9" }
        };

        foreach (var kvp in wordMap)
            text = text.Replace(kvp.Key, kvp.Value);

        // Keep only digits
        var digits = Regex.Replace(text, @"[^\d]", "");
        return digits;
    }
}
