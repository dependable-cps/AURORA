//  -----------------------------------------------------------------------
//  <copyright file="CoralVRLiveSensorsXR.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using UnityEngine;

namespace CoralVR
{
    public class CoralVRLiveSensorsXR : MonoBehaviour
    {
        public static CoralVRLiveSensorsXR Instance { get; private set; }

        public Camera fallbackVRCamera;
        public Transform xrRigRoot;

        Quaternion _headRot;
        Vector3    _headPos;
        float      _headSpeedMps;

        public float headYawRateDegPerSec { get; private set; }
        public float headPosDeltaM        { get; private set; }

        public Vector3 gazeDirWorld = Vector3.forward;
        public float   pupilDiameterMm = 4f;
        public float   gazeVelDegPerSec { get; private set; }

        [Range(0f,1f)] public float locoForward01 { get; private set; }
        [Range(0f,1f)] public float locoTurn01    { get; private set; }
        [Range(0f,1f)] public float isWalking01   { get; private set; }
        [Range(0f,1f)] public float flowMag01     { get; private set; }

        Vector3 _prevHeadPos;
        float   _prevHeadYawDeg;
        Vector3 _prevGazeDir = Vector3.forward;

        bool _gotHeadPushThisFrame;
        bool _gotEyePushThisFrame;

        public float leftEyeOpenness { get; private set; }
        public float rightEyeOpenness { get; private set; }
        public float leftPupilDiameterMm { get; private set; }
        public float rightPupilDiameterMm { get; private set; }
        public Vector2 leftPupilPosInSensor { get; private set; }
        public Vector2 rightPupilPosInSensor { get; private set; }
        public Vector3 leftGazeOriginMm { get; private set; }
        public Vector3 rightGazeOriginMm { get; private set; }
        public Vector3 leftGazeDirNormalized { get; private set; }
        public Vector3 rightGazeDirNormalized { get; private set; }
        public float convergenceDistance { get; private set; }
        public bool leftEyeBlinking { get; private set; }
        public bool rightEyeBlinking { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (!fallbackVRCamera && Camera.main) fallbackVRCamera = Camera.main;
            if (fallbackVRCamera)
            {
                _prevHeadPos    = fallbackVRCamera.transform.position;
                _prevHeadYawDeg = fallbackVRCamera.transform.eulerAngles.y;
            }
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);

            if (_gotHeadPushThisFrame)
            {
                float yawDeg = _headRot.eulerAngles.y;
                headYawRateDegPerSec = Mathf.Abs(Mathf.DeltaAngle(_prevHeadYawDeg, yawDeg)) / dt;
                headPosDeltaM        = (_headPos - _prevHeadPos).magnitude;
                _prevHeadYawDeg = yawDeg;
                _prevHeadPos    = _headPos;
            }
            else if (fallbackVRCamera)
            {
                var t = fallbackVRCamera.transform;
                float yawDeg = t.eulerAngles.y;
                headYawRateDegPerSec = Mathf.Abs(Mathf.DeltaAngle(_prevHeadYawDeg, yawDeg)) / dt;
                headPosDeltaM        = (t.position - _prevHeadPos).magnitude;
                _prevHeadYawDeg = yawDeg;
                _prevHeadPos    = t.position;
            }

            if (_gotEyePushThisFrame || gazeDirWorld.sqrMagnitude > 1e-6f)
            {
                float ang = Vector3.Angle(_prevGazeDir.normalized, gazeDirWorld.normalized);
                gazeVelDegPerSec = ang / dt;
                _prevGazeDir = gazeDirWorld;
            }

            float speedMps = (_gotHeadPushThisFrame) ? _headSpeedMps : (headPosDeltaM / dt);
            float yaw01 = Mathf.Clamp01(headYawRateDegPerSec / 200f);
            float vel01 = Mathf.Clamp01(speedMps / 3f);

            locoForward01 = vel01;
            locoTurn01    = yaw01;
            isWalking01   = vel01 > 0.1f ? 1f : 0f;
            flowMag01     = 0.5f * yaw01 + 0.5f * vel01;

            _gotHeadPushThisFrame = false;
            _gotEyePushThisFrame  = false;
        }

        public void UpdateHeadFromTracker(Quaternion rotation, Vector3 worldPos, float velocityMag)
        {
            _headRot      = rotation;
            _headPos      = worldPos;
            _headSpeedMps = Mathf.Max(0f, velocityMag);
            _gotHeadPushThisFrame = true;
        }

        public void UpdateEyeFromTobii(Vector3 worldGazeDir, float pupilLeftMm, float pupilRightMm)
        {
            gazeDirWorld    = worldGazeDir.sqrMagnitude > 1e-6f ? worldGazeDir.normalized : Vector3.forward;
            pupilDiameterMm = 0.5f * (pupilLeftMm + pupilRightMm);
            leftPupilDiameterMm = pupilLeftMm;
            rightPupilDiameterMm = pupilRightMm;
            _gotEyePushThisFrame = true;
        }

        public void UpdateSRanipalData(
            float leftOpenness, float rightOpenness,
            Vector2 leftPupilPos, Vector2 rightPupilPos,
            Vector3 leftOriginMm, Vector3 rightOriginMm,
            Vector3 leftGazeDir, Vector3 rightGazeDir,
            float convergence,
            bool leftBlink, bool rightBlink)
        {
            leftEyeOpenness = leftOpenness;
            rightEyeOpenness = rightOpenness;
            leftPupilPosInSensor = leftPupilPos;
            rightPupilPosInSensor = rightPupilPos;
            leftGazeOriginMm = leftOriginMm;
            rightGazeOriginMm = rightOriginMm;
            leftGazeDirNormalized = leftGazeDir;
            rightGazeDirNormalized = rightGazeDir;
            convergenceDistance = convergence;
            leftEyeBlinking = leftBlink;
            rightEyeBlinking = rightBlink;
        }

        public void SetFallbackCamera(Camera cam) => fallbackVRCamera = cam;
        public void SetXrRigRoot(Transform root)  => xrRigRoot       = root;
    }
}
