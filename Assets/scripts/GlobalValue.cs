namespace Aurora
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
