using System;

namespace PixInsightTools.Model {
    public class ColorTab : FilterTab {
        public static readonly string RGB = "RGB";

        public ColorTab(string target, string redPath, string greenPath, string bluePath, int stackEachNrOfFrames) : base(ColorTab.RGB, "", target, false, 2) {
            RedPath = redPath;
            GreenPath = greenPath;
            BluePath = bluePath;
            SCNRAmount = 0.8;
            StackEachNrOfFrames = stackEachNrOfFrames;
        }
        public int StackEachNrOfFrames { get; }
        private int counter;
        public int FrameCounter { get => counter % StackEachNrOfFrames;}

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

        
        internal bool ShouldStack() {
            var should = FrameCounter == 0;
            counter++;
            RaisePropertyChanged(nameof(FrameCounter));
            return should;
        }
    }
}