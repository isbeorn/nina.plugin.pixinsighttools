using NINA.Core.Enum;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;

namespace PixInsightTools.Model {
    public class LiveStackItem {

        public LiveStackItem(string path,
                             string target,
                             string filter,
                             double exposureTime,
                             int gain,
                             int offset,
                             int width,
                             int height,
                             bool isBayered,
                             BayerPatternEnum bayerPattern,
                             ImageMetaData metaData,
                             IImageStatistics statistics,
                             IStarDetectionAnalysis analysis) {
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
            MetaData = metaData;
            Statistics = statistics;
            Analysis = analysis;
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
        public ImageMetaData MetaData { get; }
        public IImageStatistics Statistics { get; }
        public IStarDetectionAnalysis Analysis { get; }
    }
}