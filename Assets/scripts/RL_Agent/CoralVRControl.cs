//  -----------------------------------------------------------------------
//  <copyright file="CoralVRControl.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using UnityEngine;
using Unity.MLAgents;

namespace CoralVR
{
    public class CoralVRControl : MonoBehaviour
    {
        public VisualTechniqueEngine visualTechniqueEngine;

        float strength01 = 0f;
        bool blurOn = false;
        bool tunnelOn = false;
        int lastToggle = 0;
        bool _agentPrevEnabled;

        void OnEnable()
        {
            ApplyMode();
            ApplyIntensity();
        }

        void OnDisable()
        {
            visualTechniqueEngine?.SetMode(MitigationMode.Off);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                blurOn = !blurOn;
                lastToggle = 1;
                ApplyMode();
                ApplyIntensity();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                tunnelOn = !tunnelOn;
                lastToggle = 2;
                ApplyMode();
                ApplyIntensity();
            }

            var ni = ReadDigit();
            if (ni >= 0f)
            {
                strength01 = ni;
                ApplyIntensity();
            }

            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                strength01 = 0f;
                ApplyIntensity();
            }
        }

        float ReadDigit()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 0.10f;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 0.20f;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 0.30f;
            if (Input.GetKeyDown(KeyCode.Alpha4)) return 0.40f;
            if (Input.GetKeyDown(KeyCode.Alpha5)) return 0.50f;
            if (Input.GetKeyDown(KeyCode.Alpha6)) return 0.60f;
            if (Input.GetKeyDown(KeyCode.Alpha7)) return 0.70f;
            if (Input.GetKeyDown(KeyCode.Alpha8)) return 0.80f;
            if (Input.GetKeyDown(KeyCode.Alpha9)) return 0.90f;
            if (Input.GetKeyDown(KeyCode.Alpha0)) return 1.00f;
            return -1f;
        }

        void ApplyMode()
        {
            if (!visualTechniqueEngine) return;

            if (blurOn && !tunnelOn)
            {
                visualTechniqueEngine.SetMode(MitigationMode.Blur);
            }
            else if (tunnelOn && !blurOn)
            {
                visualTechniqueEngine.SetMode(MitigationMode.Tunneling);
            }
            else if (blurOn && tunnelOn)
            {
                if (lastToggle == 1)
                    visualTechniqueEngine.SetMode(MitigationMode.Blur);
                else
                    visualTechniqueEngine.SetMode(MitigationMode.Tunneling);
            }
            else
            {
                visualTechniqueEngine.SetMode(MitigationMode.Off);
            }
        }

        void ApplyIntensity()
        {
            if (!visualTechniqueEngine) return;

            if (visualTechniqueEngine.currentMode == MitigationMode.Blur)
            {
                visualTechniqueEngine.SetBlur01(Mathf.Clamp01(strength01));
            }
            else if (visualTechniqueEngine.currentMode == MitigationMode.Tunneling)
            {
                float aperture01 = 1f - Mathf.Clamp01(strength01);
                visualTechniqueEngine.SetTunneling(aperture01, 0.25f);
            }
        }
    }
}
