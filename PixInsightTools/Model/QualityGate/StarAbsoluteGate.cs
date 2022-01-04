using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixInsightTools.Model.QualityGate {
    public class StarAbsoluteGate : BaseINPC, IQualityGate {
        public string Name => "Stars above threshold";
        private double value;
        public double Value {
            get => value;
            set {
                this.value = value;
                RaisePropertyChanged();
            }
        }

        public bool Passes(LiveStackItem item) {
            return item.Analysis.DetectedStars > Value;
        }
    }
}