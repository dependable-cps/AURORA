//  -----------------------------------------------------------------------
//  <copyright file="CoralVRAgent.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors.Reflection;

namespace CoralVR
{
    public class CoralVRAgent : Agent
    {
        [Header("Links")]
        public VisualTechniqueEngine visualTechniqueEngine;
        public CoralVRFeeder feeder;
        public bool useLiveSensors = false;
        public CoralVRLiveSensorsXR live;

        [Header("Signals")]
        public float headYawRateDegPerSec;
        public float headPosDeltaM;
        public float gazeVelDegPerSec;
        public float pupilDiameterMm;
        public float flowMag01;
        public float locoForward01;
        public float locoTurn01;
        public float isWalking01;

        [Header("Preferences")]
        [Range(0f,1f)] public float blurPreference = 0.5f;
        [Range(0f,1f)] public float tunnelingPreference = 0.5f;
        [Range(0.3f,1f)] public float minApertureFraction = 0.4f;
        [Range(0f,1.5f)] public float maxBlurRadius = 1.2f;

        [Header("Multi-objective reward weights (Table II)")]
        public float alphaCS = 0.50f;
        public float alphaCPL = 0.15f;
        public float alphaCML = 0.15f;
        public float alphaWM = 0.10f;
        public float lambda_m = 0.05f;
        public float lambda_delta = 0.10f;
        public float lambda_switch = 0.08f;
        public float lambda_stab = 0.05f;

        // Previous normalized severity scores for reward delta
        float _prevCsSev;
        float _prevCplSev;
        float _prevCmlSev;
        float _prevWmSev;

        int _lastMode;
        float _lastI01;
        float _tSinceSwitch;

        void Awake()
        {
            if (useLiveSensors && live == null) live = CoralVRLiveSensorsXR.Instance;
        }

        void Update()
        {
            _tSinceSwitch += Time.deltaTime;

            if (useLiveSensors)
            {
                if (live == null) live = CoralVRLiveSensorsXR.Instance;
                if (live == null) return;

                headYawRateDegPerSec = live.headYawRateDegPerSec;
                headPosDeltaM        = live.headPosDeltaM;
                gazeVelDegPerSec     = live.gazeVelDegPerSec;
                pupilDiameterMm      = live.pupilDiameterMm;
                flowMag01            = live.flowMag01;
                locoForward01        = live.locoForward01;
                locoTurn01           = live.locoTurn01;
                isWalking01          = live.isWalking01;
            }
        }

        public override void OnEpisodeBegin()
        {
            if (!useLiveSensors && feeder)
                feeder.RandomizeEpisode();

            _prevCsSev = GlobalValue.CsSeverity;
            _prevCplSev = GlobalValue.CplSeverity;
            _prevCmlSev = GlobalValue.CmlSeverity;
            _prevWmSev = GlobalValue.WmSeverity;
            _lastMode = 0;
            _lastI01 = 0;
            _tSinceSwitch = 0;
            visualTechniqueEngine?.SetMode(MitigationMode.Off);
        }

        [Observable(numStackedObservations: 8)]
        Vector4 ObsLocomotion => new Vector4(
            Mathf.Clamp01(headYawRateDegPerSec / 200f),
            Mathf.Clamp01(headPosDeltaM / 1.5f),
            Mathf.Clamp01(locoForward01),
            Mathf.Clamp01(isWalking01)
        );

        [Observable(numStackedObservations: 8)]
        Vector4 ObsEye => new Vector4(
            Mathf.Clamp01((pupilDiameterMm - 2f) / 6f),
            Mathf.Clamp01(gazeVelDegPerSec / 400f),
            Mathf.Clamp01(flowMag01),
            Mathf.Clamp01(locoTurn01)
        );

        [Observable(numStackedObservations: 8)]
        Vector4 ObsAux => new Vector4(
            _lastMode / 2f,
            Mathf.Clamp01(_lastI01),
            Mathf.Clamp01(_tSinceSwitch / 3f),
            Mathf.Clamp01(tunnelingPreference)
        );

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!visualTechniqueEngine) return;

            int mode = actions.DiscreteActions[0];
            float intensity = Mathf.Clamp01(actions.ContinuousActions[0]);

            switch ((MitigationMode)mode)
            {
                case MitigationMode.Tunneling:
                    visualTechniqueEngine.SetMode(MitigationMode.Tunneling);
                    float af = Mathf.Lerp(1f, minApertureFraction, intensity);
                    visualTechniqueEngine.SetTunneling(af, 0.25f);
                    if (_lastMode != 1) _tSinceSwitch = 0;
                    _lastMode = 1;
                    break;

                case MitigationMode.Blur:
                    visualTechniqueEngine.SetMode(MitigationMode.Blur);
                    float r = Mathf.Clamp(intensity * maxBlurRadius, 0f, maxBlurRadius);
                    visualTechniqueEngine.SetBlur01(Mathf.Clamp01(r / 1.5f));
                    if (_lastMode != 2) _tSinceSwitch = 0;
                    _lastMode = 2;
                    break;

                default:
                    visualTechniqueEngine.SetMode(MitigationMode.Off);
                    if (_lastMode != 0) _tSinceSwitch = 0;
                    _lastMode = 0;
                    break;
            }
            
            float csSev = GlobalValue.CsSeverity;
            float cplSev = GlobalValue.CplSeverity;
            float cmlSev = GlobalValue.CmlSeverity;
            float wmSev = GlobalValue.WmSeverity;

            // Multi-objective reward
            // r_t = Σ_k α_k (ŝ_{k,t-1} - ŝ_{k,t}) - λ_m I_t - λ_Δ |I_t - I_{t-1}|
            //       - λ_switch * 𝟙[M_t ≠ M_{t-1}] + λ_stab * (1 - |I_t - I_{t-1}|)
            float reward =
                  alphaCS  * (_prevCsSev  - csSev)
                + alphaCPL * (_prevCplSev - cplSev)
                + alphaCML * (_prevCmlSev - cmlSev)
                + alphaWM  * (_prevWmSev  - wmSev)
                - lambda_m * intensity
                - lambda_delta * Mathf.Abs(intensity - _lastI01)
                + lambda_stab * (1f - Mathf.Abs(intensity - _lastI01));

            if (_lastMode != mode) reward -= lambda_switch;

            AddReward(reward);

            _lastI01 = intensity;
            _prevCsSev = csSev;
            _prevCplSev = cplSev;
            _prevCmlSev = cmlSev;
            _prevWmSev = wmSev;
            
            if (csSev < 0.05f && cplSev < 0.05f && cmlSev < 0.05f && wmSev < 0.05f)
                EndEpisode();
        }
    }
}
