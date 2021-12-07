using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixInsightTools.Dockables {
    class PixInsightColorCombine : PixInsightScript {
        private readonly static string colorcombineScript = Path.Combine(scriptFolder, "colorcombine_stack.js");
        private readonly static string colorcombinePostFix = "_colorcombine";
        private string r;
        private string g;
        private string b;
        private string target;

        public PixInsightColorCombine(string r, string g, string b, string target, string workingDir, int pixInsightSlot) : base(workingDir, pixInsightSlot, new string[] { "done_colorcombine.txt", "error_colorcombine.txt" }) {
            this.r = r;
            this.g = g;
            this.b = b;
            this.target = target;
        }
        public async Task<string> Run(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var output = Path.Combine(workingDir, $"MASTER_LIGHT_{target}{colorcombinePostFix}.png");

            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = "Combining colors" });
            await RunPixInsightScript($" --execute={pixInsightSlot}:\"{colorcombineScript},'{guid}',{r},{g},{b},{output},{workingDir}\" --automation-mode", progress, ct);

            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = string.Empty });

            return output;
        }
    }
}
