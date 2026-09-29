namespace Aurora
{
    public class ScoreData
    {
        public float None;
        public float Low;
        public float Medium;
        public float High;
        public string Probabilities;

        public void SetScoreData(float low, float medium, float high)
        {
            None = 0f;
            Low = low;
            Medium = medium;
            High = high;
            Probabilities = $"Low: {Low}\nMedium: {Medium}\nHigh: {High}";
        }

        public void SetScoreData(float none, float low, float medium, float high)
        {
            None = none;
            Low = low;
            Medium = medium;
            High = high;
            Probabilities = $"None: {None}\nLow: {Low}\nMedium: {Medium}\nHigh: {High}";
        }
    }
}
