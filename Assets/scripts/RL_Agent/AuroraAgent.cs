using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors.Reflection;

namespace Aurora
{
    public class AuroraAgent : Agent
    {
        [Header("Links")]
        public VisualTechniqueEngine visualTechniqueEngine;
        public AuroraFeeder feeder;
        public bool useLiveSensors = false;
        public AuroraLiveSensorsXR live;

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

        [Header("Reward")]
        public float w_CS = 0.45f;
        public float w_CPL = 0.20f;
        public float w_CML = 0.20f;
        public float w_WM = 0.15f;
        public float lambda_m = 0.05f;
        public float lambda_delta = 0.10f;
        public float lambda_switch = 0.08f;
        public int maxDecisionSteps = 100;

        [Header("HIL")]
        public bool hilActive = false;
        [Min(0f)] public float eta = 0.5f;
        [Range(0f,1f)] public float obsMeanCS;
        [Range(0f,1f)] public float obsMeanCPL;
        [Range(0f,1f)] public float obsMeanCML;
        [Range(0f,1f)] public float obsMeanWM;
        public bool experiencedDFOV;
        [Range(0f,100f)] public float preferredIntensityDFOV = 50f;
        public bool experiencedDGB;
        [Range(0f,100f)] public float preferredIntensityDGB = 50f;
        public bool experiencedDOF;
        [Range(0f,100f)] public float preferredIntensityDOF = 50f;
        public bool experiencedRF;
        [Range(0f,100f)] public float preferredIntensityRF = 50f;

        static readonly float[] Direction = { 1f, 1f, 1f, -1f };

        readonly float[] _prevScores = new float[4];
        readonly float[] _weights = new float[4];

        int _lastMode;
        float _lastI01;
        float _tSinceSwitch;
        int _decisionPeriod = 1;

        void Awake()
        {
            if (useLiveSensors && live == null) live = AuroraLiveSensorsXR.Instance;
            var requester = GetComponent<DecisionRequester>();
            if (requester) _decisionPeriod = Mathf.Max(1, requester.DecisionPeriod);
        }

        static float[] CurrentScores() => new[]
        {
            GlobalValue.CsSeverity, GlobalValue.CplSeverity,
            GlobalValue.CmlSeverity, GlobalValue.WmSeverity
        };

        void UpdateRewardWeights()
        {
            float[] w = { w_CS, w_CPL, w_CML, w_WM };
            if (!hilActive)
            {
                w.CopyTo(_weights, 0);
                return;
            }

            float[] b = { obsMeanCS, obsMeanCPL, obsMeanCML, 1f - obsMeanWM };
            float sum = 0f;
            for (int k = 0; k < 4; k++)
            {
                _weights[k] = w[k] * (1f + eta * b[k]);
                sum += _weights[k];
            }
            if (sum <= 0f) return;
            for (int k = 0; k < 4; k++) _weights[k] /= sum;
        }

        float IntensityCost(MitigationMode mode, float intensity)
        {
            if (!hilActive) return intensity;

            switch (mode)
            {
                case MitigationMode.Tunneling when experiencedDFOV:
                    return Mathf.Abs(intensity - preferredIntensityDFOV / 100f);
                case MitigationMode.Blur when experiencedDGB:
                    return Mathf.Abs(intensity - preferredIntensityDGB / 100f);
                case MitigationMode.DepthOfField when experiencedDOF:
                    return Mathf.Abs(intensity - preferredIntensityDOF / 100f);
                case MitigationMode.RestFrame when experiencedRF:
                    return Mathf.Abs(intensity - preferredIntensityRF / 100f);
                default:
                    return intensity;
            }
        }

        void Update()
        {
            _tSinceSwitch += Time.deltaTime;

            if (useLiveSensors)
            {
                if (live == null) live = AuroraLiveSensorsXR.Instance;
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

            CurrentScores().CopyTo(_prevScores, 0);
            UpdateRewardWeights();
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
            _lastMode / 4f,
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
                    float af = Mathf.Lerp(1f, minApertureFraction, intensity);
                    visualTechniqueEngine.SetTunneling(af, 0.25f);
                    break;

                case MitigationMode.Blur:
                    float r = Mathf.Clamp(intensity * maxBlurRadius, 0f, maxBlurRadius);
                    visualTechniqueEngine.SetBlur01(Mathf.Clamp01(r / 1.5f));
                    break;

                case MitigationMode.DepthOfField:
                    visualTechniqueEngine.SetDof01(intensity);
                    break;

                case MitigationMode.RestFrame:
                    visualTechniqueEngine.SetRestFrame01(intensity);
                    break;

                default:
                    visualTechniqueEngine.SetMode(MitigationMode.Off);
                    break;
            }
            if (mode != _lastMode) _tSinceSwitch = 0;
            
            float[] scores = CurrentScores();

            // Multi-objective reward
            // r_{z,t} = Σ_k [w_k + h_z (w~_{z,k} - w_k)] d_k (ŝ_{k,t} - ŝ_{k,t+1})
            //           - (1/N) (λ_m P_z(a_t, h_z) + λ_Δ |I_t - I_{t-1}| + λ_switch 𝟙[M_t ≠ M_{t-1}])
            float reward = 0f;
            for (int k = 0; k < 4; k++)
                reward += _weights[k] * Direction[k] * (_prevScores[k] - scores[k]);

            float penalty =
                  lambda_m * IntensityCost((MitigationMode)mode, intensity)
                + lambda_delta * Mathf.Abs(intensity - _lastI01)
                + (mode != _lastMode ? lambda_switch : 0f);
            reward -= penalty / Mathf.Max(1, maxDecisionSteps);

            AddReward(reward);

            _lastMode = mode;
            _lastI01 = intensity;
            scores.CopyTo(_prevScores, 0);

            if (scores[0] < 0.05f && scores[1] < 0.05f && scores[2] < 0.05f && scores[3] > 0.95f)
                EndEpisode();
            else if (!useLiveSensors && StepCount >= maxDecisionSteps * _decisionPeriod)
                EpisodeInterrupted();
        }
    }
}
