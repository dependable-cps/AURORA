//  -----------------------------------------------------------------------
//  <copyright file="GlobalValue.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

namespace CoralVR
{
    public static class GlobalValue
    {
        public static bool isTechniqueOn;
        public static int Severity;
        
        public static float CsSeverity;
        public static float CplSeverity;
        public static float CmlSeverity;
        public static float WmSeverity;
        
        public static float[] CsProbs = new float[4];
        public static float[] CplProbs = new float[3];
        public static float[] CmlProbs = new float[3];
        public static float[] WmProbs = new float[3];
    }
}
