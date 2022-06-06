using NINA.Core.Utility;
using System.Windows.Media.Imaging;

namespace PixInsightTools.Model {
    public class FilterTab : BaseINPC {

        public static readonly string NOTARGET = "No_target";
        public static readonly string NOFILTER = "No_filter";

        public FilterTab(string filter, string path, string target, bool defaultABE, int defaultABEDegree) {
            Filter = string.IsNullOrWhiteSpace(filter) ? NOFILTER : filter;
            Path = path;
            Target = string.IsNullOrWhiteSpace(target) ? NOTARGET : target;
            Count = 0;
            Locked = false;
            EnableABE = defaultABE;
            ABEDegree = defaultABEDegree;
            Noise = new AsyncObservableCollection<Noise>();
            ShowNoise = PixInsightToolsMediator.Instance.ToolsPlugin.EvaluateNoise;
        }
        public string Filter { get; }
        public string Path { get; }
        public string Target { get; }
        private int count;

        public int Count {
            get => count;
            set {
                count = value;
                RaisePropertyChanged();
            }
        }
        private double sigmaStart = double.NaN;

        public double SigmaStart {
            get => sigmaStart;
            set {
                sigmaStart = value;
                RaisePropertyChanged();
            }
        }
        private double sigmaNow = double.NaN;

        public double SigmaNow {
            get => sigmaNow;
            set {
                sigmaNow = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SigmaImprovement));
            }
        }

        private int noiseId = 0;

        public double SigmaImprovement {
            get => (SigmaStart - SigmaNow) / SigmaStart * 100;
        }

        public bool EnableABE { get; set; }
        public int ABEDegree { get; set; }

        private bool locked;

        public bool Locked {
            get {
                lock (lockobj) {
                    return locked;
                }
            }
            set {
                lock (lockobj) {
                    locked = value;
                }
                RaisePropertyChanged();
            }
        }

        private bool showNoise;

        public bool ShowNoise {
            get {
                return showNoise;
            }
            set {
                showNoise = value;
                RaisePropertyChanged();
            }
        }

        private object lockobj = new object();

        private BitmapSource stack;

        public BitmapSource Stack {
            get => stack;
            set {
                stack = value;
                RaisePropertyChanged();
            }
        }

        public string PngPath { get; set; }

        public AsyncObservableCollection<Noise> Noise { get; }

        public void AddNoiseEvaluation(double sigma, double percentage) {
            if(double.IsNaN(sigma) || double.IsNaN(percentage)) {
                return;
            }
            var noise = new Noise(noiseId++, sigma, percentage);
            if(double.IsNaN(SigmaStart)) {
                SigmaStart = sigma;
            }

            SigmaNow = sigma;
            Noise.Add(noise);
        }
    }
}