using PixInsightTools.Model;
using System.Windows.Input;

namespace PixInsightTools.Prompts {
    public class CalibrationFramePrompt {
        public CalibrationFramePrompt(CalibrationFrame context) {
            this.Context = context;
            this.ContinueCommand = new GalaSoft.MvvmLight.Command.RelayCommand(() => { Continue = true; });
        }

        public bool Continue { get; set; } = false;

        public CalibrationFrame Context { get; }
        public ICommand ContinueCommand { get; }
    }
}