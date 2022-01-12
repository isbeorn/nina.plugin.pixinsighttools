namespace PixInsightTools.Model {
    public class ColorTab : FilterTab {
        public static readonly string RGB = "RGB";

        public ColorTab(string target, string redPath, string greenPath, string bluePath) : base(ColorTab.RGB, "", target) {
            RedPath = redPath;
            GreenPath = greenPath;
            BluePath = bluePath;
            SCNRAmount = 0.8;
        }

        public string RedPath { get; }
        public string GreenPath { get; }
        public string BluePath { get; }
        public bool EnableSCNR { get; set; }
        private double scnrAmount;
        public double SCNRAmount {
            get => scnrAmount;
            set {
                if(value < 0) { value = 0; }
                if(value > 1) { value = 1; }
                scnrAmount = value;
                RaisePropertyChanged();
            }
        }
    }
}