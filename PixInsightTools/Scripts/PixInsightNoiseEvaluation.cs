using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixInsightTools.Scripts {
    internal class PixInsightNoiseEvaluation : PixInsightScript {
        private readonly static string evalScript = Path.Combine(scriptFolder, "noise_evaluation.js");
        private string input;
        private bool isBayered;

        public PixInsightNoiseEvaluation(string input, bool isBayered, string workingDir, int pixInsightSlot) : base(workingDir, pixInsightSlot, new string[] { "done_noise_evaluation.txt", "error_noise_evaluation.txt" }) {
            this.input = input;
            this.isBayered = isBayered;
        }
        public async Task<(double, double)> Run(IProgress<ApplicationStatus> progress, CancellationToken ct) {            

            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = "Noise Evaluation" });
            var result = await RunPixInsightScript($" --execute={pixInsightSlot}:\"{evalScript},'{guid}',{input},{isBayered},{workingDir}\" --automation-mode", progress, ct);
            
            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = string.Empty });

            var parts = result.Split('|');
            if (parts.Length > 1 && double.TryParse(parts[0], out var noiseSigma)) {
                if (double.TryParse(parts[1], out var noisePct)) {
                    return (noiseSigma, noisePct);
                }
            }
            return (double.NaN, double.NaN);
        }
    }
}
