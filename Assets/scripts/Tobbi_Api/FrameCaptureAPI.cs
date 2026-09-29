using System;
using UnityEngine;

namespace Aurora
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