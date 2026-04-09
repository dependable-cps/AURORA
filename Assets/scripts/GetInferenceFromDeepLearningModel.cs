//  -----------------------------------------------------------------------
//  <copyright file="GetInferenceFromDeepLearningModel.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using UnityEngine;

namespace CoralVR
{
    /// <summary>
    /// Runs inference on the MTL-Based DL Model (ONNX).
    /// Input:  "input"      -> (1, 60, 20) float32
    /// Output: "cs_output"  -> (1, 4)  [None, Low, Medium, High]
    ///         "cpl_output" -> (1, 3)  [Low, Medium, High]
    ///         "cml_output" -> (1, 3)  [Low, Medium, High]
    ///         "wm_output"  -> (1, 3)  [Low, Medium, High]
    /// </summary>
    public class GetInferenceFromDeepLearningModel
    {
        private readonly InferenceSession _session;

        private const int SEQUENCE_LENGTH = 60;
        private const int NUM_FEATURES = 20;
        private const int CS_CLASSES = 4;
        private const int CPL_CLASSES = 3;
        private const int CML_CLASSES = 3;
        private const int WM_CLASSES = 3;

        public struct MTLOutput
        {
            public float[] csLogits;
            public float[] cplLogits;
            public float[] cmlLogits;
            public float[] wmLogits;
        }

        public GetInferenceFromDeepLearningModel(string modelName)
        {
            string modelPath = Application.streamingAssetsPath + "/Model/" + modelName + ".onnx";
            _session = new InferenceSession(modelPath);
            Debug.Log($"MTL model loaded from: {modelPath}");
        }

        public MTLOutput Predict(float[,,] inputTensor)
        {
            DenseTensor<float> tensor = new DenseTensor<float>(new[] { 1, SEQUENCE_LENGTH, NUM_FEATURES });

            for (int t = 0; t < SEQUENCE_LENGTH; t++)
            {
                for (int f = 0; f < NUM_FEATURES; f++)
                {
                    tensor[0, t, f] = inputTensor[0, t, f];
                }
            }

            List<NamedOnnxValue> inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", tensor)
            };

            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);

            MTLOutput output = new MTLOutput
            {
                csLogits = new float[CS_CLASSES],
                cplLogits = new float[CPL_CLASSES],
                cmlLogits = new float[CML_CLASSES],
                wmLogits = new float[WM_CLASSES]
            };

            foreach (DisposableNamedOnnxValue result in results)
            {
                DenseTensor<float> values = (DenseTensor<float>)result.Value;

                switch (result.Name)
                {
                    case "cs_output":
                        for (int i = 0; i < CS_CLASSES; i++)
                            output.csLogits[i] = values[0, i];
                        break;
                    case "cpl_output":
                        for (int i = 0; i < CPL_CLASSES; i++)
                            output.cplLogits[i] = values[0, i];
                        break;
                    case "cml_output":
                        for (int i = 0; i < CML_CLASSES; i++)
                            output.cmlLogits[i] = values[0, i];
                        break;
                    case "wm_output":
                        for (int i = 0; i < WM_CLASSES; i++)
                            output.wmLogits[i] = values[0, i];
                        break;
                }
            }

            return output;
        }
    }
}
