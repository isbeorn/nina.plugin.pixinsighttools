using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixInsightTools.Dockables {
    internal class PixInsightPostProcessing : PixInsightScript {
        private readonly static string abeScript = Path.Combine(scriptFolder, "postprocess_stack.js");
        private readonly static string abePostFix = "_postprocess";
        private string input;
        private int degree;

        public PixInsightPostProcessing(string input, int degree, string workingDir, int pixInsightSlot) : base(workingDir, pixInsightSlot, new string[] { "done_abe.txt", "error_abe.txt" }) {
            this.input = input;
            this.degree = degree;
        }
        public async Task<string> Run(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var output = Path.Combine(workingDir, Path.GetFileNameWithoutExtension(input) + $"{abePostFix}.png");

            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = "Post processing" });
            await RunPixInsightScript($" --execute={pixInsightSlot}:\"{abeScript},'{guid}',{input},{degree},{output},{workingDir}\" --automation-mode", progress, ct);

            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = string.Empty });

            return output;
        }
    }
}
