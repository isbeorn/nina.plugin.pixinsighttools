using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixInsightTools.Scripts {
    class PixInsightDebayer: PixInsightScript {
        private readonly static string script = Path.Combine(scriptFolder, "debayer.js");
        private readonly static string postfix = "_d";

        public PixInsightDebayer(string workingDir, int pixInsightSlot) : base(workingDir, pixInsightSlot, new string[] { "debayer.txt" }) {

        }
        public async Task<string> Run(string item, string bayerPattern, IProgress<ApplicationStatus> progress, CancellationToken ct) {

            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = $"Debayering" });
            await RunPixInsightScript($" --execute={pixInsightSlot}:\"{script},'{guid}','{item}','{workingDir}',{bayerPattern.ToUpper()}\" --automation-mode", progress, ct);
            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = string.Empty });

            return Path.Combine(workingDir, Path.GetFileNameWithoutExtension(item) + $"{postfix}.xisf");
        }
    }
}
