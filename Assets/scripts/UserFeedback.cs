//  -----------------------------------------------------------------------
//  <copyright file="UserFeedback.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using System;
using System.Collections;
using UnityEngine;
using TMPro;
using Random = System.Random;

public class UserFeedback : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI WMText;
    public SpeechRecogniser speechRecogniser;

    [Header("Timer Settings")]
    public float currentTime;
    public bool countDown;

    [Header("Feedback Timing")]
    [Tooltip("Seconds between full feedback prompt cycles (FMS + CPL + CML).")]
    public float feedbackCycleInterval = 60f;

    [Tooltip("Seconds to wait for user to respond to each prompt.")]
    public float responseWaitTime = 8f;

    [Tooltip("Seconds between digit recall sequences.")]
    public float digitRecallInterval = 25f;

    private string lastSpokenText = "";
    private Random rnd = new Random();
    private string currentDigits = "";
    private bool startTimer;
    private Coroutine digitRecallCoroutine;
    private Coroutine feedbackCycleCoroutine;

    // Tracks the current prompt type we're waiting for a response to
    private string _awaitingFeedbackType = "";
    private bool _ratingReceived;
    private bool _digitRecallReceived;
    private string _lastDigitRecallUtterance;

    void Start()
    {
        startTimer = false;
        WMText.enabled = false;
    }

    void OnEnable()
    {
        ImageToggleController.ActiveTimer += StartTimer;

        // Subscribe to speech events
        if (speechRecogniser == null)
            speechRecogniser = SpeechRecogniser.Instance;

        if (speechRecogniser != null)
        {
            speechRecogniser.OnRatingDetected += HandleRatingDetected;
            speechRecogniser.OnDigitRecallDetected += HandleDigitRecallDetected;
        }
    }

    void OnDisable()
    {
        ImageToggleController.ActiveTimer -= StartTimer;

        if (speechRecogniser != null)
        {
            speechRecogniser.OnRatingDetected -= HandleRatingDetected;
            speechRecogniser.OnDigitRecallDetected -= HandleDigitRecallDetected;
        }
    }

    private void StartTimer(bool start)
    {
        startTimer = start;
        if (startTimer)
        {
            // Log session start
            HilCsvLogger.Instance?.LogSessionEvent("session_start", "HIL feedback collection started");

            digitRecallCoroutine ??= StartCoroutine(DigitRecallSequence());
            feedbackCycleCoroutine ??= StartCoroutine(CognitiveStateFeedbackCycle());
        }
        else
        {
            HilCsvLogger.Instance?.LogSessionEvent("session_end", "HIL feedback collection stopped");

            if (digitRecallCoroutine != null)
            {
                StopCoroutine(digitRecallCoroutine);
                digitRecallCoroutine = null;
            }

            if (feedbackCycleCoroutine != null)
            {
                StopCoroutine(feedbackCycleCoroutine);
                feedbackCycleCoroutine = null;
            }
        }
    }

    void Update()
    {
        if (!startTimer) return;

        currentTime = countDown ? currentTime - Time.deltaTime : currentTime + Time.deltaTime;
        timerText.text = "Time: " + TimeSpan.FromSeconds(currentTime).ToString(@"mm\:ss");
        AttentionScoringSystem.globalTime = (int)currentTime;

        if (currentTime < 40)
            timerText.faceColor = Color.red;
        else if (currentTime < 120)
            timerText.faceColor = Color.yellow;
    }

    // ---- Speech Event Handlers ----

    private void HandleRatingDetected(string rawUtterance, int rating)
    {
        if (string.IsNullOrEmpty(_awaitingFeedbackType)) return;

        Debug.Log($"[UserFeedback] Received {_awaitingFeedbackType} rating: {rating} from \"{rawUtterance}\"");

        // Log to HIL CSV
        HilCsvLogger.Instance?.LogCognitiveStateFeedback(_awaitingFeedbackType, rating, rawUtterance);

        _ratingReceived = true;
        speechRecogniser.expectedResponseType = "";
    }

    private void HandleDigitRecallDetected(string rawUtterance)
    {
        _lastDigitRecallUtterance = rawUtterance;
        _digitRecallReceived = true;
        speechRecogniser.expectedResponseType = "";
    }

    // ---- Coroutine: Digit Recall (Working Memory) ----

    private IEnumerator DigitRecallSequence()
    {
        while (true)
        {
            // Generate and show digits
            currentDigits = GenerateFormattedNumber();
            string rawDigits = currentDigits.Replace(" ", "");

            yield return new WaitForSeconds(2f);
            ShowMessage("Remember the following digits \n            " + currentDigits);

            yield return new WaitForSeconds(15f);

            // Ask user to recall
            ShowMessage("Say the digits out loud!");
            _digitRecallReceived = false;
            _lastDigitRecallUtterance = "";

            if (speechRecogniser != null)
                speechRecogniser.expectedResponseType = "digit_recall";

            yield return new WaitForSeconds(responseWaitTime);

            // Evaluate recall
            if (_digitRecallReceived && !string.IsNullOrEmpty(_lastDigitRecallUtterance))
            {
                string spokenDigits = SpeechRecogniser.ExtractDigits(_lastDigitRecallUtterance);
                bool correct = spokenDigits == rawDigits;

                HilCsvLogger.Instance?.LogDigitRecall(rawDigits, spokenDigits, correct);

                if (correct)
                    ShowMessage("Correct!");
                else
                    ShowMessage($"The digits were: {currentDigits}");
            }
            else
            {
                HilCsvLogger.Instance?.LogDigitRecall(rawDigits, "", false);
                ShowMessage($"The digits were: {currentDigits}");
            }

            speechRecogniser.expectedResponseType = "";
            yield return new WaitForSeconds(5f);
            WMText.enabled = false;

            yield return new WaitForSeconds(digitRecallInterval);
        }
    }

    // ---- Coroutine: Cognitive State Feedback (FMS, CPL, CML) ----

    private IEnumerator CognitiveStateFeedbackCycle()
    {
        // Initial delay before first feedback prompt
        yield return new WaitForSeconds(feedbackCycleInterval);

        while (true)
        {
            // Pause digit recall during feedback collection
            if (digitRecallCoroutine != null)
            {
                StopCoroutine(digitRecallCoroutine);
                digitRecallCoroutine = null;
            }

            // --- FMS (Cybersickness / Fast Motion Sickness) ---
            yield return StartCoroutine(PromptForRating(
                "FMS",
                "Rate your motion sickness on a scale from 0 to 10"
            ));

            yield return new WaitForSeconds(2f);

            // --- CPL (Cognitive Physical Load) ---
            yield return StartCoroutine(PromptForRating(
                "CPL",
                "Rate your physical demand on a scale from 0 to 10"
            ));

            yield return new WaitForSeconds(2f);

            // --- CML (Cognitive Mental Load) ---
            yield return StartCoroutine(PromptForRating(
                "CML",
                "Rate your mental demand on a scale from 0 to 10"
            ));

            yield return new WaitForSeconds(2f);
            WMText.enabled = false;

            // Resume digit recall
            digitRecallCoroutine ??= StartCoroutine(DigitRecallSequence());

            yield return new WaitForSeconds(feedbackCycleInterval);
        }
    }

    /// <summary>
    /// Prompts user for a single rating, waits for speech response, logs result.
    /// </summary>
    private IEnumerator PromptForRating(string feedbackType, string promptMessage)
    {
        _awaitingFeedbackType = feedbackType;
        _ratingReceived = false;

        ShowMessage(promptMessage);

        if (speechRecogniser != null)
            speechRecogniser.expectedResponseType = feedbackType;

        // Wait for response or timeout
        float elapsed = 0f;
        while (!_ratingReceived && elapsed < responseWaitTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!_ratingReceived)
        {
            Debug.LogWarning($"[UserFeedback] No response for {feedbackType} within {responseWaitTime}s");
            // Log a missed response (-1 = no response)
            HilCsvLogger.Instance?.LogCognitiveStateFeedback(feedbackType, -1, "NO_RESPONSE");
        }

        _awaitingFeedbackType = "";
        if (speechRecogniser != null)
            speechRecogniser.expectedResponseType = "";
    }

    // ---- Helpers ----

    private string GenerateFormattedNumber()
    {
        int num = rnd.Next(10000, 99999);
        string numString = num.ToString();
        return $"{numString[0]}  {numString[1]}  {numString[2]}  {numString[3]}  {numString[4]}";
    }

    private void ShowMessage(string message)
    {
        if (WMText.text != message)
        {
            WMText.enabled = true;
            WMText.text = message;

            if (lastSpokenText != message)
            {
                WindowsVoice.speak(message);
                lastSpokenText = message;
            }
        }
    }
}
