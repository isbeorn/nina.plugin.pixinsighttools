
using System.ComponentModel;

namespace PixInsightTools.Model.QualityGate {
    public interface IQualityGate: INotifyPropertyChanged {
        string Name { get; }
        double Value { get; }
        bool Passes(LiveStackItem item);
    }
}