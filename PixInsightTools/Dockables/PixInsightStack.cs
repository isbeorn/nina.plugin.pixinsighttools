using NINA.Core.Model;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PixInsightTools.Dockables {

    internal class PixInsightStack : PixInsightScript {
        private readonly static string stackScript = Path.Combine(scriptFolder, "pixelmath_stack.js");
        private string input;
        private string stack;

        public PixInsightStack(string input, string stack, string workingDir, int pixInsightSlot) : base(workingDir, pixInsightSlot, new string[] { "done.txt", "error.txt" }) {
            this.input = input;
            this.stack = stack;
        }

        public async Task<int> Run(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = "Stacking" });
            var result = await RunPixInsightScript($" --execute={pixInsightSlot}:\"{stackScript},'{guid}',{input},{stack},{workingDir}\"  --automation-mode", progress, ct);

            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = string.Empty });

            
            if (int.TryParse(result, out var count)) {
                return count;
            }
            
            return -1;
        }
    }
}