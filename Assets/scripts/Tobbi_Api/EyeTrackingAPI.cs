//  -----------------------------------------------------------------------
//  <copyright file="EyeTrackingAPI.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using CoralVR;
using Tobii.XR;
using UnityEngine;
using ViveSR.anipal.Eye;
using Valve.VR.InteractionSystem;
using ViveSR;

namespace CoralVR
{
    public class EyeTrackingAPI : ITracker
    {
        private TobiiXR_EyeTrackingData _eyeTrackingData;
        private long _frameCounter;
        private VerboseData _sRanipalEyeVerboseData;
        private EyeData _eyeData;
        private int n = 0;

        public StringBuilder InitiateTracker(StringBuilder csvLogger)
        {
            csvLogger = new StringBuilder();

            // Eye Verbose Data from SRanipal SDK
            _sRanipalEyeVerboseData = new VerboseData();
            _eyeData = new EyeData();

            // Create the Header file
            csvLogger = CreateCsvHeader(csvLogger);
            return csvLogger;
        }

        private StringBuilder CreateCsvHeader(StringBuilder csvLogger)
        {
            string[] header =
            {
                "Time", "#Frame",

                "ConvergenceValid",
                "Convergence_distance",

                "Left_Eye_Openness",
                "Right_Eye_Openness",

                "Left_Eye_Closed",
                "Right_Eye_Closed",

                "LeftPupilDiameter",
                "RightPupilDiameter",

                "LeftPupilPosInSensorX",
                "LeftPupilPosInSensorY",

                "RightPupilPosInSensorX",
                "RightPupilPosInSensorY",

                // Local gaze data
                "LocalGazeValid",
                "GazeOriginLclSpc_X",
                "GazeOriginLclSpc_Y",
                "GazeOriginLclSpc_Z",

                "GazeDirectionLclSpc_X",
                "GazeDirectionLclSpc_Y",
                "GazeDirectionLclSpc_Z",

                // Local gaze data
                "WorldGazeValid",
                "GazeOriginWrldSpc_X",
                "GazeOriginWrldSpc_Y",
                "GazeOriginWrldSpc_Z",

                "GazeDirectionWrldSpc_X",
                "GazeDirectionWrldSpc_Y",
                "GazeDirectionWrldSpc_Z",

                // Normalized Gaze Origin
                "NrmLeftEyeOriginX",
                "NrmLeftEyeOriginY",
                "NrmLeftEyeOriginZ",

                "NrmRightEyeOriginX",
                "NrmRightEyeOriginY",
                "NrmRightEyeOriginZ",

                // Normalized Gaze Direction
                "NrmSRLeftEyeGazeDirX",
                "NrmSRLeftEyeGazeDirY",
                "NrmSRLeftEyeGazeDirZ",

                "NrmSRRightEyeGazeDirX",
                "NrmSRRightEyeGazeDirY",
                "NrmSRRightEyeGazeDirZ"
            };
            header.ForEach(item => csvLogger.Append(item + ","));

            // Write the Header
            csvLogger.AppendLine();
            return csvLogger;
        }


        public StringBuilder StartTracking(String time, int frameCount, StringBuilder csvLogger, PredictionInput input)
        {
            // Get Eye data from TOBI API
            TobiiXR_EyeTrackingData localEyeData = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.Local);
            TobiiXR_EyeTrackingData worldEyeData = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.World);


            // Get Eye data from SRNipal
            ViveSR.Error error = SRanipal_Eye_API.GetEyeData(ref _eyeData);

            if (error == Error.WORK)
            {
                _sRanipalEyeVerboseData = _eyeData.verbose_data;

                // Left Eye Data
                SingleEyeData sRleftEyeData = _sRanipalEyeVerboseData.left;
                // Right Eye
                SingleEyeData sRrightEyeData = _sRanipalEyeVerboseData.right;

                // Write in the CSV file
                csvLogger.AppendFormat(
                    "{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}," +
                    "{19}, {20}, {21}, {22}, {23}, {24}, {25},{26}, {27}, {28}, {29}, {30}, {31}, {32}, {33}, {34}, {35}," +
                    "{36}, {37}, {38}, {39}",
                    // Time and Frame count
                    time, frameCount,

                    // Convergence Distance
                    worldEyeData.ConvergenceDistanceIsValid,
                    worldEyeData.ConvergenceDistance,

                    // Eye Openness
                    sRleftEyeData.eye_openness,
                    sRrightEyeData.eye_openness,

                    // Eye blinking
                    localEyeData.IsLeftEyeBlinking,
                    localEyeData.IsRightEyeBlinking,

                    // Pupil Diameter
                    sRleftEyeData.pupil_diameter_mm,
                    sRrightEyeData.pupil_diameter_mm,

                    // Pupil Position in Sensor area (x, y)
                    sRleftEyeData.pupil_position_in_sensor_area.x,
                    sRleftEyeData.pupil_position_in_sensor_area.y,
                    sRrightEyeData.pupil_position_in_sensor_area.x,
                    sRrightEyeData.pupil_position_in_sensor_area.y,

                    // IS local Gaze Valid
                    localEyeData.GazeRay.IsValid,

                    // Local Space Gaze Origin Combined
                    localEyeData.GazeRay.Origin.x,
                    localEyeData.GazeRay.Origin.y,
                    localEyeData.GazeRay.Origin.z,

                    // Local Space Gaze Direction Combined
                    localEyeData.GazeRay.Direction.x,
                    localEyeData.GazeRay.Direction.y,
                    localEyeData.GazeRay.Direction.z,

                    // IS World Gaze Valid
                    worldEyeData.GazeRay.IsValid,

                    //world space Gaze Origin Combined
                    worldEyeData.GazeRay.Origin.x,
                    worldEyeData.GazeRay.Origin.y,
                    worldEyeData.GazeRay.Origin.z,

                    // world space Gaze Direction Combined
                    worldEyeData.GazeRay.Direction.x,
                    worldEyeData.GazeRay.Direction.y,
                    worldEyeData.GazeRay.Direction.z,

                    // Gaze Origin in mm
                    sRleftEyeData.gaze_origin_mm.x,
                    sRleftEyeData.gaze_origin_mm.y,
                    sRleftEyeData.gaze_origin_mm.z,
                    sRrightEyeData.gaze_origin_mm.x,
                    sRrightEyeData.gaze_origin_mm.y,
                    sRrightEyeData.gaze_origin_mm.z,

                    // Normalized Gaze direction
                    sRleftEyeData.gaze_direction_normalized.x,
                    sRleftEyeData.gaze_direction_normalized.y,
                    sRleftEyeData.gaze_direction_normalized.z,
                    sRrightEyeData.gaze_direction_normalized.x,
                    sRrightEyeData.gaze_direction_normalized.y,
                    sRrightEyeData.gaze_direction_normalized.z
                );
                csvLogger.AppendLine();

                GetDataToPredict(sRrightEyeData, sRleftEyeData, worldEyeData, localEyeData, input, frameCount);
            }

            return csvLogger;
        }

        private void GetDataToPredict(SingleEyeData sRrightEyeData, SingleEyeData sRleftEyeData,
            TobiiXR_EyeTrackingData worldEyeData, TobiiXR_EyeTrackingData localEyeData,
            PredictionInput input, int frameCount)
        {
            // Populate PredictionInput (unchanged)
            input.Left_Eye_Openness = sRleftEyeData.eye_openness;
            input.Right_Eye_Openness = sRrightEyeData.eye_openness;
            input.LeftPupilDiameter = sRleftEyeData.pupil_diameter_mm;
            input.RightPupilDiameter = sRrightEyeData.pupil_diameter_mm;
            input.LeftPupilPosInSensorX = sRleftEyeData.pupil_position_in_sensor_area.x;
            input.LeftPupilPosInSensorY = sRleftEyeData.pupil_position_in_sensor_area.y;
            input.RightPupilPosInSensorX = sRrightEyeData.pupil_position_in_sensor_area.x;
            input.RightPupilPosInSensorY = sRrightEyeData.pupil_position_in_sensor_area.y;
            input.GazeOriginLclSpc_X = localEyeData.GazeRay.Origin.x;
            input.GazeOriginLclSpc_Y = localEyeData.GazeRay.Origin.y;
            input.GazeOriginLclSpc_Z = localEyeData.GazeRay.Origin.z;
            input.GazeDirectionLclSpc_X = localEyeData.GazeRay.Direction.x;
            input.GazeDirectionLclSpc_Y = localEyeData.GazeRay.Direction.y;
            input.GazeDirectionLclSpc_Z = localEyeData.GazeRay.Direction.z;
            input.GazeOriginWrldSpc_X = worldEyeData.GazeRay.Origin.x;
            input.GazeOriginWrldSpc_Y = worldEyeData.GazeRay.Origin.y;
            input.GazeOriginWrldSpc_Z = worldEyeData.GazeRay.Origin.z;
            input.GazeDirectionWrldSpc_X = worldEyeData.GazeRay.Direction.x;
            input.GazeDirectionWrldSpc_Y = worldEyeData.GazeRay.Direction.y;
            input.GazeDirectionWrldSpc_Z = worldEyeData.GazeRay.Direction.z;
            input.NrmLeftEyeOriginX = sRleftEyeData.gaze_origin_mm.x;
            input.NrmLeftEyeOriginY = sRleftEyeData.gaze_origin_mm.y;
            input.NrmLeftEyeOriginZ = sRleftEyeData.gaze_origin_mm.z;
            input.NrmRightEyeOriginX = sRrightEyeData.gaze_origin_mm.x;
            input.NrmRightEyeOriginY = sRrightEyeData.gaze_origin_mm.y;
            input.NrmRightEyeOriginZ = sRrightEyeData.gaze_origin_mm.z;
            input.NrmSRLeftEyeGazeDirX = sRleftEyeData.gaze_direction_normalized.x;
            input.NrmSRLeftEyeGazeDirY = sRleftEyeData.gaze_direction_normalized.y;
            input.NrmSRLeftEyeGazeDirZ = sRleftEyeData.gaze_direction_normalized.z;
            input.NrmSRRightEyeGazeDirX = sRrightEyeData.gaze_direction_normalized.x;
            input.NrmSRRightEyeGazeDirY = sRrightEyeData.gaze_direction_normalized.y;
            input.NrmSRRightEyeGazeDirZ = sRrightEyeData.gaze_direction_normalized.z;
            input.Convergence_distance = worldEyeData.ConvergenceDistance;

            // ---- Build 20 SHAP-selected features for MTL model ----
            // Head data from camera
            Camera cam = Camera.main;
            Quaternion headRot = cam != null ? cam.transform.rotation : Quaternion.identity;
            Vector3 headPos = cam != null ? cam.transform.position : Vector3.zero;
            Vector3 headFwd = cam != null ? cam.transform.forward : Vector3.forward;
            Vector3 gazeTarget = headPos + headFwd * 2f;

            // Angular features: HMD-to-target angles
            Vector3 rightOriginM = new Vector3(
                sRrightEyeData.gaze_origin_mm.x / 1000f,
                sRrightEyeData.gaze_origin_mm.y / 1000f,
                sRrightEyeData.gaze_origin_mm.z / 1000f
            );
            Vector3 rightToTarget = (gazeTarget - rightOriginM).normalized;
            Vector3 rightAngles = CalculateAngularDifference(headFwd, rightToTarget);

            Vector3 leftOriginM = new Vector3(
                sRleftEyeData.gaze_origin_mm.x / 1000f,
                sRleftEyeData.gaze_origin_mm.y / 1000f,
                sRleftEyeData.gaze_origin_mm.z / 1000f
            );
            Vector3 leftToTarget = (gazeTarget - leftOriginM).normalized;
            Vector3 leftAngles = CalculateAngularDifference(headFwd, leftToTarget);

            // Combined gaze point
            Vector3 combOrigin = worldEyeData.GazeRay.Origin;
            Vector3 combDir = worldEyeData.GazeRay.Direction;
            Vector3 combGazePoint = combOrigin + combDir * 2f;

            // 20 SHAP features in exact training order (from mtl_scaler_params.json)
            float[] inputArray =
            {
                sRrightEyeData.gaze_origin_mm.x,                      // [0]  Right_Origin_X
                worldEyeData.GazeRay.Origin.x,                        // [1]  Combine_Origin_X
                frameCount,                                             // [2]  EyeFrames
                sRleftEyeData.gaze_origin_mm.x,                       // [3]  Left_Origin_X
                sRleftEyeData.pupil_diameter_mm,                      // [4]  Left_Diameter
                rightAngles.z,                                          // [5]  Right_HMD2TargAng_Z
                headRot.z,                                              // [6]  HMDRot_Z
                sRleftEyeData.gaze_origin_mm.y,                       // [7]  Left_Origin_Y
                rightAngles.x,                                          // [8]  Right_HMD2TargAng_X
                sRleftEyeData.gaze_direction_normalized.z,             // [9]  Left_GazeDir_Z
                sRleftEyeData.pupil_position_in_sensor_area.x,        // [10] Left_PupilSensor_X
                sRleftEyeData.gaze_origin_mm.z,                       // [11] Left_Origin_Z
                leftAngles.x,                                           // [12] Left_HMD2TargAng_X
                headRot.w,                                              // [13] HMDRot_W
                gazeTarget.y,                                           // [14] GazeTarget_Y
                (sRleftEyeData.pupil_diameter_mm > 0) ? 1f : 0f,      // [15] Left_Validity
                sRrightEyeData.pupil_position_in_sensor_area.y,        // [16] Right_PupilSensor_Y
                sRleftEyeData.gaze_direction_normalized.x,             // [17] Left_GazeDir_X
                combGazePoint.z,                                        // [18] Combine_GazePoint_Z
                headRot.y                                               // [19] HMDRot_Y
            };

            PredictDlModelResult.Instance.StartPredict(inputArray);

            var live = CoralVRLiveSensorsXR.Instance;
            if (live != null)
            {
                Vector3 worldGazeDir = new Vector3(
                    worldEyeData.GazeRay.Direction.x,
                    worldEyeData.GazeRay.Direction.y,
                    worldEyeData.GazeRay.Direction.z
                );
                live.UpdateEyeFromTobii(worldGazeDir,
                    sRleftEyeData.pupil_diameter_mm,
                    sRrightEyeData.pupil_diameter_mm);

                live.UpdateSRanipalData(
                    sRleftEyeData.eye_openness,
                    sRrightEyeData.eye_openness,
                    sRleftEyeData.pupil_position_in_sensor_area,
                    sRrightEyeData.pupil_position_in_sensor_area,
                    sRleftEyeData.gaze_origin_mm,
                    sRrightEyeData.gaze_origin_mm,
                    sRleftEyeData.gaze_direction_normalized,
                    sRrightEyeData.gaze_direction_normalized,
                    worldEyeData.ConvergenceDistance,
                    localEyeData.IsLeftEyeBlinking,
                    localEyeData.IsRightEyeBlinking
                );
            }
        }

        private static Vector3 CalculateAngularDifference(Vector3 reference, Vector3 target)
        {
            Vector3 cross = Vector3.Cross(reference, target);
            float dot = Vector3.Dot(reference, target);
            float angleX = Mathf.Atan2(cross.y, dot) * Mathf.Rad2Deg;
            float angleY = Mathf.Atan2(cross.x, dot) * Mathf.Rad2Deg;
            float angleZ = Vector3.Angle(reference, target);
            return new Vector3(angleX, angleY, angleZ);
        }

        public void SaveToFile(String path, StringBuilder csvLogger)
        {
            System.IO.File.WriteAllText(
                path + "eye_tracking_data-" + DateTime.Now.ToString("dd-MMM-yyyy-(hh-mm-ss)") + ".csv",
                csvLogger.ToString());
            Debug.Log("Eye Tracking data saved at: " + path);
            Logger.Log(LogLevel.INFO, "Eye Tracking data saved at: " + path);
        }
    }
}
