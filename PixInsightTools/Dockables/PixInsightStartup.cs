using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugins.PixInsightTools.Dockables {
    internal class PixInsightStartup : PixInsightScript {
        private readonly static string startupScript = Path.Combine(scriptFolder, "startup.js");
        public PixInsightStartup(string workingDir, int pixInsightSlot) : base(workingDir, pixInsightSlot, new string[] { "started.txt" }) { 
        }

        public async Task Run(IProgress<ApplicationStatus> progress, CancellationToken ct) {

            var pixInsightProcessIsRunning = Process.GetProcessesByName("PixInsight")?.FirstOrDefault(x => x.MainWindowTitle == $"PixInsight ({pixInsightSlot})") != null;

            if (!pixInsightProcessIsRunning) {

                progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = "Starting up PixInsight" });
                await RunPixInsightScript($"--run=\"{startupScript},{workingDir}\" --automation-mode --no-startup-gui-messages --no-splash --new-instance={pixInsightSlot} --no-startup-scripts --no-startup-check-updates", progress, ct);

                progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = string.Empty });

            }            
        }
    }
}
