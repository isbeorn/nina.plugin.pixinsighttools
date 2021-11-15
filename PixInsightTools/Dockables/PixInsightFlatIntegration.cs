using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugins.PixInsightTools.Dockables {
    internal class PixInsightFlatIntegration : PixInsightScript {
        private readonly static string script = Path.Combine(scriptFolder, "flat_integration.js");

        public PixInsightFlatIntegration(string workingDir, int pixInsightSlot) : base(workingDir, pixInsightSlot, new string[] { "done_flatintegration.txt" }) {
        }
        public async Task<string> Run(string filter, string flatsForIntegration, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var output = Path.Combine(workingDir, $"STACK_FLAT_{filter}.xisf");

            progress?.Report(new ApplicationStatus() { Status = "Integrating flat" });
            await RunPixInsightScript($" --execute={pixInsightSlot}:\"{script},{flatsForIntegration},{workingDir},{true},{output}\" --automation-mode", progress, ct);

            progress?.Report(new ApplicationStatus() { Status = string.Empty });

           return output;
        }
    }
}
