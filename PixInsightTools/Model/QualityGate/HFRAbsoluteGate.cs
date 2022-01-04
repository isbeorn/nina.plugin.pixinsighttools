using NINA.Core.Utility;

namespace PixInsightTools.Model.QualityGate {
    public class HFRAbsoluteGate : BaseINPC, IQualityGate {
        public string Name => "HFR below threshold";
        private double value;
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