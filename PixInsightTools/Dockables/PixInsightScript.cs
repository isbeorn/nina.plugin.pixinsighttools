using NINA.Core.Model;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixInsightTools.Dockables {
    class PixInsightScript {
        protected readonly static string scriptFolder = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\NINA\Plugins\PixInsight Tools\Scripts");
        protected string workingDir;
        protected int pixInsightSlot;

        protected string[] scriptStatusFiles;
        protected Guid guid;

        public PixInsightScript(string workingDir, int pixInsightSlot, string[] scriptStatusFiles) {
            this.workingDir = workingDir;
            this.pixInsightSlot = pixInsightSlot;
            this.scriptStatusFiles = scriptStatusFiles;
            this.guid = Guid.NewGuid();
        }

        protected async Task TryDeleteFile(string path) {

            for (int i = 0; i < 5; i++) {
                try {
                    if (File.Exists(path)) {
                        File.Delete(path);
                    }
                    break;
                } catch (Exception) {
                    await Task.Delay(500);
                }
            }
        }

        protected async Task<string> TryReadFile(string path) {

            for (int i = 0; i < 5; i++) {
                try {
                    return File.ReadAllText(path);
                } catch (Exception) {
                    await Task.Delay(500);
                }
            }
            return string.Empty;
        }

        private async Task statusFileCleanup() {
            foreach (var file in scriptStatusFiles) {
                await TryDeleteFile(Path.Combine(workingDir, guid + "_" + file));
            }
        }

        protected async Task<string> RunPixInsightScript(string arguments, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            await statusFileCleanup();

            var psi = new ProcessStartInfo($"\"{Properties.Settings.Default.PixInsightLocation}\"", arguments);
            Process.Start(psi);
            Logger.Info($"Starting process: {psi.FileName} {psi.Arguments}");

            bool fileExists = false;
            var fileContent = string.Empty;
            while(!fileExists) {
                foreach (var file in scriptStatusFiles) {
                    if(!File.Exists(Path.Combine(workingDir, guid + "_" + file))) {
                        await Task.Delay(1000, ct);
                    } else {
                        fileExists = true;

                        fileContent = await TryReadFile(Path.Combine(workingDir, guid + "_" + file));
                    }
                }
            }

            await statusFileCleanup();
            return fileContent;
        }

    }
}
