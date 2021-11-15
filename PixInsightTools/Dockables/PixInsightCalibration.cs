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

namespace NINA.Plugins.PixInsightTools.Dockables {
    internal class PixInsightCalibration : PixInsightScript {
        private readonly static string calibrateScript = Path.Combine(scriptFolder, "calibrate.js");
        private readonly static string calibrationPostfix = "_c";

        private string flat;
        private string dark;
        private string bias;
        private bool compress;
        private int pedestal;
        private bool saveAs16Bit;

        public PixInsightCalibration(string workingDir, int pixInsightSlot, bool compress, int pedestal, bool saveAs16Bit, string flat, string dark, string bias) : base(workingDir, pixInsightSlot, new string[] { "calibrated.txt" }) {
            this.flat = flat;
            this.dark = dark;
            this.bias = bias;
            this.compress = compress;
            this.pedestal = pedestal;
            this.saveAs16Bit = saveAs16Bit;

        }

        public async Task<string> Run(string item, IProgress<ApplicationStatus> progress, CancellationToken ct) {

            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = $"Calibrating" });
            await RunPixInsightScript($" --execute={pixInsightSlot}:\"{calibrateScript},'{item}','{workingDir}','{dark}','{flat}','{bias}',{compress},{pedestal},{saveAs16Bit}\" --automation-mode", progress, ct);
            progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = string.Empty });

            return Path.Combine(workingDir, Path.GetFileNameWithoutExtension(item) + $"{calibrationPostfix}.xisf");
        }

    }
}
