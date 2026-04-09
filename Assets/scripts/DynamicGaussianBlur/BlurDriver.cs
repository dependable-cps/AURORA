//  -----------------------------------------------------------------------
//  <copyright file="BlurDriver.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoralVR
{
    public class BlurDriver : MonoBehaviour
{
    [Header("References")] public VolumeProfile volumeProfile;

    [Header("Blur Control")] public bool enableBlur = true; // ✅ toggle in Inspector
    [Range(0f, 1f)] public float blur = 0.5f; // 0–1 slider (remapped to 0.5–1.5)

    private DepthOfField dof;

    // Actual blur radius range you want
    private const float MIN_RADIUS = 0.5f;
    private const float MAX_RADIUS = 1.5f;

    void Awake()
    {
        if (!dof) volumeProfile.TryGet(out dof);
        dof.active = false;
    }
    
    void Update()
    {
        if (dof == null) return;


        if (enableBlur)
        {
            dof.active = true;
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            float radius = Mathf.Lerp(MIN_RADIUS, MAX_RADIUS, blur);
            dof.gaussianMaxRadius.Override(radius);
            dof.gaussianStart.Override(0f);
            dof.gaussianEnd.Override(0f);
        }
        else
        {

            dof.mode.Override(DepthOfFieldMode.Off);
            blur = 0.0f;
            dof.gaussianMaxRadius.Override(0.5f);
            dof.gaussianStart.Override(0f);
            dof.gaussianEnd.Override(0f);
            dof.active = false;
        }
    }
    }
}