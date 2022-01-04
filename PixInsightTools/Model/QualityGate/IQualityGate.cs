
namespace PixInsightTools.Model.QualityGate {
    public interface IQualityGate {
        string Name { get; }
        double Value { get; }
        bool Passes(LiveStackItem item);
    }
}