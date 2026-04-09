//  -----------------------------------------------------------------------
//  <copyright file="FrameCaptureAPI.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using System;
using UnityEngine;

namespace CoralVR
{
    public class FrameCaptureAPI
    {
        public void SaveImageToFile(String path, int frameCount, String time)
        {
            // Both Eye
            String imageFileName = "Frame-" + frameCount + "-" + time +".png";
            
            ScreenCapture.CaptureScreenshot(path + "/" + imageFileName,
                ScreenCapture.StereoScreenCaptureMode.BothEyes); /// IO operation 
        }
    }
}