using Newtonsoft.Json;
using NINA.Core.Utility;

namespace PixInsightTools.Model.QualityGate {
    [JsonObject(MemberSerialization.OptIn)]
    public class RMSAbsoluteGate : BaseINPC, IQualityGate {
        public RMSAbsoluteGate() {
            Value = 10;
        }
        [JsonProperty]
        public string Name => "RMS ArcSec below threshold";

        private double value;
        [JsonProperty]
        public double Value {
            get => value;
            set {
                this.value = value;
                RaisePropertyChanged();
            }
        }

        public bool Passes(LiveStackItem item) {
            return item.MetaData.Image.RecordedRMS.Total * item.MetaData.Image.RecordedRMS.Scale < Value;
        }
    }
}