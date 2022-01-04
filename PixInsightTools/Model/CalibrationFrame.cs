using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace PixInsightTools.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class CalibrationFrame {                
        public CalibrationFrame() { 
            
        }
        public CalibrationFrame(string path) :this() {
            Path = path;
            Type = CalibrationFrameType.DARK;
        }

        public CalibrationFrame(string path, int gain, int offset, double exposureTime, string filter) : this(path) {
            Gain = gain;
            Offset = offset;
            ExposureTime = exposureTime;
            Filter = filter;
        }
        [JsonProperty]
        public CalibrationFrameType Type { get; set; }
        [JsonProperty]
        public string Path { get; set; }
        [JsonProperty]
        public int Gain { get; set; }
        [JsonProperty]
        public int Offset { get; set; }
        [JsonProperty]
        public double ExposureTime { get; set; }
        [JsonProperty]
        public string Filter { get; set; }
        [JsonProperty]
        public int Width { get; set; }
        [JsonProperty]
        public int Height { get; set; }
    }


    public enum CalibrationFrameType {
        DARK,
        BIAS,
        FLAT
    }
}