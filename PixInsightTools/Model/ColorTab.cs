namespace PixInsightTools.Model {
    public class ColorTab : FilterTab {
        public static readonly string RGB = "RGB";

        public ColorTab(string target, string redPath, string greenPath, string bluePath) : base(ColorTab.RGB, "", target) {
            RedPath = redPath;
            GreenPath = greenPath;
            BluePath = bluePath;
        }

        public string RedPath { get; }
        public string GreenPath { get; }
        public string BluePath { get; }
    }
}