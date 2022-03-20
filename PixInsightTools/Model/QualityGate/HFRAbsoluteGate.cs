using Newtonsoft.Json;
using NINA.Core.Utility;

namespace PixInsightTools.Model.QualityGate {
    [JsonObject(MemberSerialization.OptIn)]
    public class HFRAbsoluteGate : BaseINPC, IQualityGate {
        public HFRAbsoluteGate() {
            Value = 10;
        }

        [JsonProperty]
        public string Name => "HFR below threshold";
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
            return item.Analysis.HFR < value;
        }
    }
}