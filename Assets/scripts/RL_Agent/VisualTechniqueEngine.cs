//  -----------------------------------------------------------------------
//  <copyright file="VisualTechniqueEngine.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using UnityEngine;

namespace CoralVR
{
    public enum MitigationMode { Off = 0, Blur = 1, Tunneling = 2 }

    [DisallowMultipleComponent]
    public class VisualTechniqueEngine : MonoBehaviour
    {
        [Header("Drivers (assign in Inspector)")]
        public BlurDriver blurDriver;
        public CustomTunnelingVignetteController tunnelingController;

        [Header("Startup State")]
        public MitigationMode startMode = MitigationMode.Off;
        [Range(0f, 1f)] public float startBlur01 = 0.5f;
        [Range(0f, 1f)] public float startAperture01 = 1.0f;
        [Range(0f, 1f)] public float startFeather01 = 0.25f;

        [Header("Read-only")]
        public MitigationMode currentMode = MitigationMode.Off;

        void Awake()
        {
            if (!blurDriver)
                Debug.LogWarning("[VisualTechniqueEngine] BlurDriver not assigned.");
            if (!tunnelingController)
                Debug.LogWarning("[VisualTechniqueEngine] Tunneling controller not assigned.");

            switch (startMode)
            {
                case MitigationMode.Blur:
                    SetMode(MitigationMode.Blur);
                    SetBlur01(startBlur01);
                    break;
                case MitigationMode.Tunneling:
                    SetMode(MitigationMode.Tunneling);
                    SetTunneling(startAperture01, startFeather01);
                    break;
                default:
                    SetMode(MitigationMode.Off);
                    break;
            }
        }

        public void SetMode(MitigationMode newMode)
        {
            currentMode = newMode;

            if (blurDriver)                   blurDriver.enableBlur = false;
            if (tunnelingController != null)  tunnelingController.enableVignette = false;

            switch (newMode)
            {
                case MitigationMode.Blur:
                    if (blurDriver) blurDriver.enableBlur = true;
                    break;

                case MitigationMode.Tunneling:
                    if (tunnelingController != null) tunnelingController.enableVignette = true;
                    tunnelingController?.ApplyNow();
                    break;

                case MitigationMode.Off:
                default:
                    break;
            }
        }

        public void SetBlur01(float intensity01)
        {
            if (!blurDriver) return;
            blurDriver.blur = Mathf.Clamp01(intensity01);
            if (currentMode != MitigationMode.Blur) SetMode(MitigationMode.Blur);
        }

        public void SetTunneling(float aperture01, float feather01 = 0.25f)
        {
            if (!tunnelingController) return;

            aperture01 = Mathf.Clamp01(aperture01);
            feather01  = Mathf.Clamp01(feather01);

            tunnelingController.m_DefaultParameters.apertureSize     = aperture01;
            tunnelingController.m_DefaultParameters.featheringEffect = feather01;

            if (currentMode != MitigationMode.Tunneling) SetMode(MitigationMode.Tunneling);

            tunnelingController.ApplyNow();
        }

        public void SetMitigation(MitigationMode mode, float strength01, float feather01 = 0.25f)
        {
            strength01 = Mathf.Clamp01(strength01);
            switch (mode)
            {
                case MitigationMode.Blur:
                    SetBlur01(strength01);
                    break;
                case MitigationMode.Tunneling:
                    SetTunneling(aperture01: 1f - strength01, feather01: feather01);
                    break;
                default:
                    SetMode(MitigationMode.Off);
                    break;
            }
        }

        public void TurnEverythingOff() => SetMode(MitigationMode.Off);
        public void EnableBlur(float strength01) { SetMode(MitigationMode.Blur); SetBlur01(strength01); }
        public void EnableTunneling(float aperture01, float feather01 = 0.25f) { SetMode(MitigationMode.Tunneling); SetTunneling(aperture01, feather01); }
    }
}
