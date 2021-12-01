using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixInsightTools.Dockables {
    internal class PixInsightAlign : PixInsightScript {
        private readonly static string alignmentScript = Path.Combine(scriptFolder, "alignment.js");
        private readonly static string alignmentPostfix = "_r";
        private string input;
        private string stack;

        public PixInsightAlign(string input, string stack, string workingDir, int pixInsightSlot) : base(workingDir, pixInsightSlot, new string[] { "aligned.txt" }) {
            this.input = input;
            this.stack = stack;
        }
        public async Task<string> Run(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = "Aligning" });
            await RunPixInsightScript($" --execute={pixInsightSlot}:\"{alignmentScript},'{guid}',{stack},{input},{workingDir}\"  --automation-mode", progress, ct);

            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = string.Empty });


            return Path.Combine(workingDir, Path.GetFileNameWithoutExtension(input) + $"{alignmentPostfix}.xisf");
        }
    }
}
