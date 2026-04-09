//  -----------------------------------------------------------------------
//  <copyright file="HeadTrackingAPI.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace CoralVR
{
    public class HeadTrackingAPI : ITracker
    {
        public StringBuilder InitiateTracker(StringBuilder csvLogger)
        {
            csvLogger = new StringBuilder();
            string[] header =
            {
                "Time", "#Frame", "HeadQRotationX", "HeadQRotationY", "HeadQRotationZ", "HeadQRotationW",
                "HeadEulX", "HeadEulY", "HeadEulZ", "AspectRatio", "CameraDepth", "FOV", "EyeConvergence", "Velocity"
            };
            header.ForEach(item => csvLogger.Append(item + ","));
            csvLogger.AppendLine();

            return csvLogger;
        }

        public StringBuilder TrackHead(String time, int frameCount, Camera activeCamera, StringBuilder csvLogger,
            PredictionInput input)
        {
            if (activeCamera != null)
            {
                var rotation = activeCamera.transform.rotation;
                Vector3 velocity = activeCamera.velocity;

                csvLogger.AppendFormat(
                    "{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}",
                    time, frameCount, rotation.x, rotation.y, rotation.z, rotation.w,
                    rotation.eulerAngles.x, rotation.eulerAngles.y, rotation.eulerAngles.z,
                    activeCamera.aspect, activeCamera.depth, activeCamera.fieldOfView,
                    activeCamera.stereoConvergence, velocity.magnitude);

                input.HeadEulX = rotation.eulerAngles.x;
                input.HeadEulY = rotation.eulerAngles.y;
                input.HeadEulZ = rotation.eulerAngles.z;
                input.HeadQRotationX = rotation.x;
                input.HeadQRotationY = rotation.y;
                input.HeadQRotationZ = rotation.z;
                input.HeadQRotationW = rotation.w;
                input.Velocity = velocity.magnitude;
                // push to live-bridge
                var live = CoralVRLiveSensorsXR.Instance;
                if (live != null)
                {
                    live.UpdateHeadFromTracker(rotation, activeCamera.transform.position, velocity.magnitude);
                }


                csvLogger.AppendLine();
            }

            return csvLogger;
        }

        public void SaveToFile(String path, StringBuilder csvLogger)
        {
            File.WriteAllText(
                path + "head_tracking_" + DateTime.Now.ToString("dd-MMM-yyyy-(hh-mm-ss)") + ".csv",
                csvLogger.ToString());
            Debug.Log("Head Tracking data saved at: " + path);
            Logger.Log(LogLevel.INFO, "Head Tracking data saved at: " + path);
        }
    }
}