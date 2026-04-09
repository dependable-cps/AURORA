//  -----------------------------------------------------------------------
//  <copyright file="PredictionBar.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace CoralVR
{
    public class PredictionBar : MonoBehaviour
    {
        public Slider slider;
        public Gradient gradient;
        public Image fill;
        public Image reaction;
        public Sprite[] reactionSprites;

        [Tooltip("Max class index: 3 for CS (None/Low/Med/High), 2 for CPL/CML/WM (Low/Med/High)")]
        public int maxLevel = 3;

        private void Start()
        {
            slider.maxValue = maxLevel;
            slider.value = 0;
            fill.color = gradient.Evaluate(0f);
        }

        public void UpdatePredictionBar(int prediction)
        {
            if (prediction < 0 || prediction > maxLevel)
            {
                Debug.LogWarning($"PredictionBar: Invalid prediction {prediction}, clamping to [0,{maxLevel}]");
                prediction = Mathf.Clamp(prediction, 0, maxLevel);
            }

            if (GlobalValue.isTechniqueOn)
            {
                prediction = 0;
            }

            slider.value = prediction;
            fill.color = gradient.Evaluate(slider.normalizedValue);

            if (prediction >= 0 && prediction < reactionSprites.Length)
            {
                reaction.sprite = reactionSprites[prediction];
            }
        }
    }
}
