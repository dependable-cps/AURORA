//  -----------------------------------------------------------------------
//  <copyright file="PredictDlModelResult.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CoralVR
{
    [System.Serializable]
    public class MTLScalerParams
    {
        public float[] mean;
        public float[] scale;
        public string[] feature_names;
    }

    public class PredictDlModelResult : MonoBehaviour
    {
        [Header("MTL Model")]
        [SerializeField] private string modelName = "MTL-Based_DL_Model";

        [Header("Prediction Bars (CS, CPL, CML, WM)")]
        [SerializeField] private PredictionBar csBar;
        [SerializeField] private PredictionBar cplBar;
        [SerializeField] private PredictionBar cmlBar;
        [SerializeField] private PredictionBar wmBar;

        public static PredictDlModelResult Instance;
        public bool isTest = true;

        private GetInferenceFromDeepLearningModel _inference;
        private Queue<float[]> _frameBuffer;
        private float[] _scalerMean;
        private float[] _scalerScale;

        private const int SEQUENCE_LENGTH = 60;
        private const int NUM_FEATURES = 20;

        private Coroutine _testLoop;
        private static bool _runnerActive;

        void OnEnable()
        {
            if (!isTest) return;
            if (_runnerActive) return;

            _runnerActive = true;
            _testLoop = StartCoroutine(TestLoop());
        }

        void OnDisable()
        {
            if (_testLoop != null)
            {
                StopCoroutine(_testLoop);
                _testLoop = null;
            }
            _runnerActive = false;
        }

        IEnumerator TestLoop()
        {
            while (isTest)
            {
                float delay = Random.Range(6f, 8f);
                yield return new WaitForSecondsRealtime(delay);
                TestData();
            }
        }

        private void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _frameBuffer = new Queue<float[]>(SEQUENCE_LENGTH);
            LoadScalerParams();
            _inference = new GetInferenceFromDeepLearningModel(modelName);
        }

        private void LoadScalerParams()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Model", "mtl_scaler_params.json");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                MTLScalerParams data = JsonUtility.FromJson<MTLScalerParams>(json);

                if (data != null && data.mean != null && data.scale != null &&
                    data.mean.Length == NUM_FEATURES && data.scale.Length == NUM_FEATURES)
                {
                    _scalerMean = data.mean;
                    _scalerScale = data.scale;
                    Debug.Log("PredictDlModelResult: Scaler params loaded (20 features)");
                    return;
                }

                Debug.LogWarning($"PredictDlModelResult: Scaler size mismatch (mean={data?.mean?.Length}, scale={data?.scale?.Length})");
            }
            else
            {
                Debug.LogWarning($"PredictDlModelResult: Scaler file not found at {path}");
            }
            
            _scalerMean = new float[NUM_FEATURES];
            _scalerScale = new float[NUM_FEATURES];
            for (int i = 0; i < NUM_FEATURES; i++)
                _scalerScale[i] = 1f;
        }
        
        public void StartPredict(float[] rawFeatures)
        {
            if (isTest) return;
            if (rawFeatures == null || rawFeatures.Length != NUM_FEATURES)
            {
                Debug.LogWarning($"PredictDlModelResult: Expected {NUM_FEATURES} features, got {rawFeatures?.Length}");
                return;
            }
            
            float[] normalized = new float[NUM_FEATURES];
            for (int i = 0; i < NUM_FEATURES; i++)
            {
                if (Mathf.Abs(_scalerScale[i]) > 1e-8f)
                    normalized[i] = (rawFeatures[i] - _scalerMean[i]) / _scalerScale[i];
                else
                    normalized[i] = 0f;

                if (float.IsNaN(normalized[i]) || float.IsInfinity(normalized[i]))
                    normalized[i] = 0f;
            }
            
            _frameBuffer.Enqueue(normalized);
            if (_frameBuffer.Count > SEQUENCE_LENGTH)
                _frameBuffer.Dequeue();

            if (_frameBuffer.Count < SEQUENCE_LENGTH) return;
            
            float[,,] tensor = new float[1, SEQUENCE_LENGTH, NUM_FEATURES];
            int t = 0;
            foreach (float[] frame in _frameBuffer)
            {
                for (int f = 0; f < NUM_FEATURES; f++)
                    tensor[0, t, f] = frame[f];
                t++;
            }

            RunInference(tensor);
        }

        private void TestData()
        {
            float[,,] dummyTensor = new float[1, SEQUENCE_LENGTH, NUM_FEATURES];
            for (int t = 0; t < SEQUENCE_LENGTH; t++)
                for (int f = 0; f < NUM_FEATURES; f++)
                    dummyTensor[0, t, f] = Random.Range(-2f, 2f);

            RunInference(dummyTensor);
        }

        private void RunInference(float[,,] tensor)
        {
            GetInferenceFromDeepLearningModel.MTLOutput output = _inference.Predict(tensor);
            
            float[] csProbs = Softmax(output.csLogits);
            float[] cplProbs = Softmax(output.cplLogits);
            float[] cmlProbs = Softmax(output.cmlLogits);
            float[] wmProbs = Softmax(output.wmLogits);
            
            int csPred = ArgMax(csProbs);
            int cplPred = ArgMax(cplProbs);
            int cmlPred = ArgMax(cmlProbs);
            int wmPred = ArgMax(wmProbs);

            // Update prediction bars
            if (csBar != null) csBar.UpdatePredictionBar(csPred);
            if (cplBar != null) cplBar.UpdatePredictionBar(cplPred);
            if (cmlBar != null) cmlBar.UpdatePredictionBar(cmlPred);
            if (wmBar != null) wmBar.UpdatePredictionBar(wmPred);
            
            GlobalValue.CsProbs = csProbs;
            GlobalValue.CplProbs = cplProbs;
            GlobalValue.CmlProbs = cmlProbs;
            GlobalValue.WmProbs = wmProbs;


            GlobalValue.CsSeverity = NormalizedSeverity(csProbs, 4);
            GlobalValue.CplSeverity = NormalizedSeverity(cplProbs, 3);
            GlobalValue.CmlSeverity = NormalizedSeverity(cmlProbs, 3);
            GlobalValue.WmSeverity = NormalizedSeverity(wmProbs, 3);
            
            GlobalValue.Severity = csPred;

            Debug.Log($"MTL Prediction -> CS:{csPred} ({GlobalValue.CsSeverity:F3}) " +
                      $"CPL:{cplPred} ({GlobalValue.CplSeverity:F3}) " +
                      $"CML:{cmlPred} ({GlobalValue.CmlSeverity:F3}) " +
                      $"WM:{wmPred} ({GlobalValue.WmSeverity:F3})");
        }

        private static float NormalizedSeverity(float[] probs, int numClasses)
        {
            float score = 0f;
            for (int i = 0; i < numClasses; i++)
                score += i * probs[i];
            return score / (numClasses - 1);
        }

        private static float[] Softmax(float[] logits)
        {
            float max = logits.Max();
            float[] exp = new float[logits.Length];
            float sum = 0f;

            for (int i = 0; i < logits.Length; i++)
            {
                exp[i] = Mathf.Exp(logits[i] - max);
                sum += exp[i];
            }

            float[] probs = new float[logits.Length];
            for (int i = 0; i < logits.Length; i++)
                probs[i] = exp[i] / sum;

            return probs;
        }

        private static int ArgMax(float[] array)
        {
            int maxIndex = 0;
            float maxValue = array[0];
            for (int i = 1; i < array.Length; i++)
            {
                if (array[i] > maxValue)
                {
                    maxValue = array[i];
                    maxIndex = i;
                }
            }
            return maxIndex;
        }

        public void ResetBuffer()
        {
            _frameBuffer?.Clear();
            Debug.Log("PredictDlModelResult: Buffer reset");
        }
    }
}
