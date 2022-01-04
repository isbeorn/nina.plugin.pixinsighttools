using NINA.Core.Enum;

namespace PixInsightTools.Model {
    public class LiveStackItem {

        public LiveStackItem(string path, string target, string filter, double exposureTime, int gain, int offset, int width, int height, bool isBayered, BayerPatternEnum bayerPattern) {
            Path = path;
            Filter = filter;
            ExposureTime = exposureTime;
            Gain = gain;
            Offset = offset;
            Target = target;
            Width = width;
            Height = height;
            IsBayered = isBayered;
            BayerPattern = bayerPattern;
        }

        public string Path { get; }
        public string Target { get; }
        public string Filter { get; }
        public double ExposureTime { get; }
        public int Gain { get; }
        public int Offset { get; }
        public int Width { get; }
        public int Height { get; }
        public bool IsBayered { get; }
        public BayerPatternEnum BayerPattern { get; }
    }
}