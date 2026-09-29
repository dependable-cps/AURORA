using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aurora
{
    public class BlurDriver : MonoBehaviour
{
    [Header("References")] public VolumeProfile volumeProfile;

    [Header("Blur Control")] public bool enableBlur = true; // ✅ toggle in Inspector
    [Range(0f, 1f)] public float blur = 0.5f; // 0–1 slider (remapped to 0.5–1.5)

    [Header("DOF Control")] public bool enableDof = false;
    [Range(0f, 1f)] public float dof01 = 0.5f;

    private DepthOfField dof;

    // Actual blur radius range you want
    private const float MIN_RADIUS = 0.5f;
    private const float MAX_RADIUS = 1.5f;

    private const float MIN_FOCUS_M = 0.5f;
    private const float MAX_FOCUS_M = 10.0f;
    private const float MIN_APERTURE = 1f;
    private const float MAX_APERTURE = 32f;

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
        else if (enableDof)
        {
            dof.active = true;
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(Mathf.Lerp(MAX_FOCUS_M, MIN_FOCUS_M, dof01));
            dof.aperture.Override(Mathf.Lerp(MAX_APERTURE, MIN_APERTURE, dof01));
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