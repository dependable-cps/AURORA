//  -----------------------------------------------------------------------
//  <copyright file="CoralVRFeeder.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using UnityEngine;

namespace CoralVR
{
    public class CoralVRFeeder : MonoBehaviour
    {
        public CoralVRAgent agent;

        [System.Serializable] public struct FloatRange { public float min, max; }
        [System.Serializable] public struct IntRange   { public int min, max; }

        [Header("Ranges")]
        public IntRange   cs0_3               = new IntRange   { min = 0,   max = 3   };
        public FloatRange headYawDegPerSec    = new FloatRange { min = 0f,  max = 220f };
        public FloatRange headPosDeltaM       = new FloatRange { min = 0f,  max = 1.5f };
        public FloatRange gazeVelDegPerSec    = new FloatRange { min = 0f,  max = 450f };
        public FloatRange pupilMm             = new FloatRange { min = 2.5f, max = 7.5f };

        [Header("Context & prefs")]
        public FloatRange locoFwd01           = new FloatRange { min = 0f,  max = 1f   };
        public FloatRange locoTurn01          = new FloatRange { min = 0f,  max = 1f   };
        public FloatRange isWalking01         = new FloatRange { min = 0f,  max = 1f   };
        public FloatRange blurPref01          = new FloatRange { min = 0f,  max = 1f   };
        public FloatRange tunPref01           = new FloatRange { min = 0f,  max = 1f   };
        public float      minApertureFraction = 0.4f;
        public float      maxBlurRadius       = 1.2f;

        [Header("Timing")]
        public bool randomizeOnEpisodeBegin = true;
        public bool resampleEveryNSteps     = false;
        public int  resampleInterval        = 60;

        [Header("Jitter")]
        public bool  jitter     = true;
        public float jitterHz   = 0.6f;
        public float jitterAmt  = 0.12f;

        int   _step;
        float _t;

        void OnEnable()
        {
            if (!agent) agent = FindObjectOfType<CoralVRAgent>(true);
            if (randomizeOnEpisodeBegin && agent) RandomizeEpisode();
        }

        public void RandomizeEpisode()
        {
            if (!agent) return;

            // Set randomized severity scores via GlobalValue (MTL model outputs)
            GlobalValue.CsSeverity  = Random.Range(0f, 1f);
            GlobalValue.CplSeverity = Random.Range(0f, 1f);
            GlobalValue.CmlSeverity = Random.Range(0f, 1f);
            GlobalValue.WmSeverity  = Random.Range(0f, 1f);
            GlobalValue.Severity    = Mathf.RoundToInt(GlobalValue.CsSeverity * 3f);

            agent.headYawRateDegPerSec = Rand(headYawDegPerSec);
            agent.headPosDeltaM        = Rand(headPosDeltaM);
            agent.gazeVelDegPerSec     = Rand(gazeVelDegPerSec);
            agent.pupilDiameterMm      = Rand(pupilMm);

            agent.locoForward01        = Rand(locoFwd01);
            agent.locoTurn01           = Rand(locoTurn01);
            agent.isWalking01          = Rand(isWalking01) > 0.5f ? 1f : 0f;

            agent.blurPreference       = Rand(blurPref01);
            agent.tunnelingPreference  = Rand(tunPref01);
            agent.minApertureFraction  = minApertureFraction;
            agent.maxBlurRadius        = maxBlurRadius;

            float yaw01 = Mathf.Clamp01(agent.headYawRateDegPerSec / headYawDegPerSec.max);
            float fwd01 = Mathf.Clamp01(agent.locoForward01);
            agent.flowMag01 = Mathf.Clamp01(0.5f * yaw01 + 0.5f * fwd01);
        }

        void Update()
        {
            if (!agent) return;

            if (resampleEveryNSteps && (++_step % Mathf.Max(1, resampleInterval) == 0))
                RandomizeEpisode();

            if (jitter)
            {
                _t += Time.deltaTime * (Mathf.PI * 2f * Mathf.Max(0.01f, jitterHz));
                float j = Mathf.Sin(_t) * jitterAmt;

                agent.headYawRateDegPerSec = Mathf.Max(0f, agent.headYawRateDegPerSec * (1f + j));
                agent.headPosDeltaM        = Mathf.Max(0f, agent.headPosDeltaM        * (1f + 0.5f*j));
                agent.gazeVelDegPerSec     = Mathf.Max(0f, agent.gazeVelDegPerSec     * (1f + 0.7f*j));
                agent.pupilDiameterMm      = Mathf.Clamp(agent.pupilDiameterMm + 0.1f*j, pupilMm.min, pupilMm.max);

                agent.locoForward01 = Mathf.Clamp01(agent.locoForward01 + 0.2f*j);
                agent.locoTurn01    = Mathf.Clamp01(agent.locoTurn01    + 0.2f*j);
                agent.flowMag01     = Mathf.Clamp01(agent.flowMag01     + 0.2f*j);
            }

            float mv   = Mathf.Clamp01(agent.headYawRateDegPerSec / headYawDegPerSec.max);
            float eye  = Mathf.Clamp01(agent.gazeVelDegPerSec     / gazeVelDegPerSec.max);
            float fwd  = Mathf.Clamp01(agent.locoForward01);
            float score = 0.4f*mv + 0.4f*eye + 0.2f*fwd + Random.Range(-0.05f, 0.05f);

            // Update all 4 cognitive state severity scores via GlobalValue
            GlobalValue.CsSeverity  = Mathf.Clamp01(score);
            GlobalValue.CplSeverity = Mathf.Clamp01(0.3f * mv + 0.3f * fwd + 0.4f * eye + Random.Range(-0.05f, 0.05f));
            GlobalValue.CmlSeverity = Mathf.Clamp01(0.5f * eye + 0.3f * mv + 0.2f * fwd + Random.Range(-0.05f, 0.05f));
            GlobalValue.WmSeverity  = Mathf.Clamp01(0.4f * eye + 0.4f * mv + 0.2f * fwd + Random.Range(-0.05f, 0.05f));
            GlobalValue.Severity    = (score < 0.25f) ? 0 :
                                      (score < 0.50f) ? 1 :
                                      (score < 0.75f) ? 2 : 3;
        }

        static float Rand(FloatRange r) => Random.Range(r.min, r.max);
        static int   RandInt(IntRange r) => Random.Range(r.min, r.max + 1);
    }
}
