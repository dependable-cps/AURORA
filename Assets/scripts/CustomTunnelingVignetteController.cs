//  -----------------------------------------------------------------------
//  <copyright file="CustomTunnelingVignetteController.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoralVR
{
    public class CustomTunnelingVignetteController : MonoBehaviour
{
    [Header("Target")]
    public TunnelingVignetteController vignetteController;

    [Header("Control Toggles")]
    [Tooltip("Enable or disable vignette manually.")]
    public bool enableVignette = true;

    [Tooltip("When enabled, updates also run in Edit mode (not playing).")]
    public bool applyInEditMode = true;

    private static readonly int _ApertureSize = Shader.PropertyToID("_ApertureSize");
    private static readonly int _FeatheringEffect = Shader.PropertyToID("_FeatheringEffect");

    private MaterialPropertyBlock _block;
    private MeshRenderer _meshRenderer;

    [Header("Parameters")]
    public VignetteParameters m_DefaultParameters = new VignetteParameters();

    private void OnEnable()
    {
        EnsureCache();

        if (vignetteController != null && Application.isPlaying)
        {
            // Disable automatic locomotion providers for manual control
            vignetteController.locomotionVignetteProviders.Clear();
        }

        ApplyNow();
    }

    private void Start()
    {
        EnsureCache();
        ApplyNow();
    }

    private void LateUpdate()
    {
        if (vignetteController == null) return;

        if (Application.isPlaying || applyInEditMode)
        {
            ApplyNow();
        }
    }

    private void OnValidate()
    {
        EnsureCache();
        if (vignetteController != null && (Application.isPlaying || applyInEditMode))
        {
            ApplyNow();
        }
    }

    private void EnsureCache()
    {
        if (_block == null) _block = new MaterialPropertyBlock();
        if (vignetteController != null)
        {
            if (_meshRenderer == null)
                _meshRenderer = vignetteController.GetComponent<MeshRenderer>();
        }
    }

    [ContextMenu("Apply Now")]
    public void ApplyNow()
    {
        if (vignetteController == null || _meshRenderer == null) return;

        // Enable/disable renderer based on toggle
        _meshRenderer.enabled = enableVignette;

        if (!enableVignette) return;

        _meshRenderer.GetPropertyBlock(_block);
        _block.SetFloat(_ApertureSize, m_DefaultParameters.apertureSize);
        _block.SetFloat(_FeatheringEffect, m_DefaultParameters.featheringEffect);
        _meshRenderer.SetPropertyBlock(_block);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(_meshRenderer);
            EditorUtility.SetDirty(vignetteController);
        }
#endif
    }
    }
}
