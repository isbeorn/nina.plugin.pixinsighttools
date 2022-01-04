using NINA.Core.Utility;

namespace PixInsightTools.Model.QualityGate {
    public class RMSAbsoluteGate : BaseINPC, IQualityGate {
        public string Name => "RMS ArcSec below threshold";

        private double value;
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