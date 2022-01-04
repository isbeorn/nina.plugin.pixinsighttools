using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixInsightTools.Scripts {
    internal class PixInsightResample : PixInsightScript {
        private readonly static string resampleScript = Path.Combine(scriptFolder, "resample.js");
        private readonly static string resamplePostfix = "_i";
        private string input;
        private int resampleAmount;

        public PixInsightResample(string input, int resampleAmount, string workingDir, int pixInsightSlot) : base(workingDir, pixInsightSlot, new string[] { "done_resample.txt", "error_resample.txt" }) {
            this.input = input;
            this.resampleAmount = resampleAmount;
        }
        public async Task<string> Run(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var output = Path.Combine(workingDir, Path.GetFileNameWithoutExtension(input) + $"{resamplePostfix}.xisf");

            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = "Resampling" });
            await RunPixInsightScript($" --execute={pixInsightSlot}:\"{resampleScript},'{guid}',{input},{resampleAmount},{output},{workingDir}\" --automation-mode", progress, ct);

            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = string.Empty });

            return output;  
        }
    }
}
