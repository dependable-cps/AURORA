using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Unity.Barracuda;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Reflection;
using System.Linq;
public class Getinferencefrommodel 

{
    public DenseTensor <int> outputTensor;
    static string modelPath = Application.streamingAssetsPath + "/Models/stacked_clf.onnx";
    InferenceSession session = new InferenceSession(modelPath);
    public long predict (float [] inputArray)
    {
            var inputTensor = new DenseTensor<float>(inputArray,
                                                         new int[] { 1, 11 });
                    var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("feature_input", inputTensor)
            };
       
         
        var sessionOutput = session.Run(inputs);
        var rawResult = (DisposableNamedOnnxValue)sessionOutput.ToArray()[1];
        var onnxValue = (IDisposableReadOnlyCollection<DisposableNamedOnnxValue>)rawResult.Value;
        var probalities = (Dictionary<long, float>)onnxValue.ToArray()[0].Value;
        var rawlabel = (DisposableNamedOnnxValue)sessionOutput.ToArray()[0];
        var onnxlabel = rawlabel.Value as DenseTensor<long>;
        var probabilitynone = probalities.Values.ToArray()[0].ToString();
        var probabilitylow = probalities.Values.ToArray()[1].ToString();
        var probabilitymedium = probalities.Values.ToArray()[2].ToString();
        var probabilityhigh = probalities.Values.ToArray()[3].ToString();
        var responseMessage = $"Probabilities: \nNone: {probabilitynone}\n" +
                $"Low: {probabilitylow}\nMedium: {probabilitymedium}\nHigh: {probabilityhigh}";

        score.proba = responseMessage;

        score.Score = onnxlabel.GetValue(0);
        return onnxlabel.GetValue(0);

    }
    
}
