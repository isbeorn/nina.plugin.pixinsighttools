namespace PixInsightTools.Model {
    public class Noise {
        public Noise(int id, double sigma, double percentage) {
            this.Id = id;
            this.Sigma = sigma;
            this.Percentage = percentage;
        }

        public int Id { get; }
        public double Sigma { get; }
        public double Percentage { get; }
    }
}