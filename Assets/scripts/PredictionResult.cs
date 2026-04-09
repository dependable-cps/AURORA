//  -----------------------------------------------------------------------
//  <copyright file="PredictionResult.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------
using System.Linq;
using UnityEngine;

namespace CoralVR
{
    public class PredictionResult
    {
        private readonly ScoreData _scoreData;
        private readonly PredictionBar _predictionBar;

        public PredictionResult(PredictionBar predictionBar)
        {
            _scoreData = new ScoreData();
            _predictionBar = predictionBar;
        }

        public void OnDataReceived(float low, float medium, float high)
        {
            _scoreData.SetScoreData(low, medium, high);
            Debug.Log("Result:" + _scoreData.Probabilities);

            if (low >= medium && low >= high)
            {
                _predictionBar.UpdatePredictionBar(0);
            }
            else if (medium >= low && medium >= high)
            {
                _predictionBar.UpdatePredictionBar(1);
            }
            else
            {
                _predictionBar.UpdatePredictionBar(2);
            }
        }

        public void OnDataReceived(float none, float low, float medium, float high)
        {
            _scoreData.SetScoreData(none, low, medium, high);

            float[] logits = { none, low, medium, high };

            float[] probabilities = Softmax(logits);

            int predictedClass = ArgMax(logits);

            Debug.Log($"CS Prediction: None={probabilities[0]:F3}, Low={probabilities[1]:F3}, " +
                      $"Med={probabilities[2]:F3}, High={probabilities[3]:F3} -> Class {predictedClass}");

            _predictionBar.UpdatePredictionBar(predictedClass);
        }

        private float[] Softmax(float[] logits)
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
            {
                probs[i] = exp[i] / sum;
            }

            return probs;
        }

        private int ArgMax(float[] array)
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
    }
}
