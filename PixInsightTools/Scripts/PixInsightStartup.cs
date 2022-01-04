using NINA.Core.Model;
using NINA.Core.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace PixInsightTools.Scripts {

    internal class PixInsightStartup : PixInsightScript {

        static PixInsightStartup() {
        }

        private static readonly string startupScript = Path.Combine(scriptFolder, "startup.js");

        public PixInsightStartup(string workingDir, int pixInsightSlot) : base(workingDir, pixInsightSlot, new string[] { "started.txt" }) {
        }

        public Task Run(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return Task.Run(async () => {
                // Ensure that only one instance will be spawned when multiple application instances are started or multiple invocations are done via instructions in parallel
                // Ref: https://docs.microsoft.com/en-us/dotnet/api/system.threading.mutex?redirectedfrom=MSDN&view=net-5.0
                var mutexid = $"Global\\{{{PixInsightToolsMediator.Instance.ToolsPlugin.Identifier}}}";
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);

                using (var mutex = new Mutex(false, mutexid, out var createNew, securitySettings)) {
                    var hasHandle = false;
                    try {
                        try {
                            // Wait for 5 seconds to receive the mutex
                            hasHandle = mutex.WaitOne(5000, false);
                            if (hasHandle == false) {
                                throw new TimeoutException("Timeout waiting for exclusive access");
                            }

                            try {
                                var pixInsightProcessIsRunning = Process.GetProcessesByName("PixInsight")?.FirstOrDefault(x => x.MainWindowTitle == $"PixInsight ({pixInsightSlot})") != null;

                                if (!pixInsightProcessIsRunning) {
                                    progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = "Starting up PixInsight" });
                                    await RunPixInsightScript($"--run=\"{startupScript},'{guid}',{workingDir}\" --automation-mode --no-startup-gui-messages --no-splash --new-instance={pixInsightSlot} --no-startup-scripts --no-startup-check-updates", progress, ct);

                                    progress?.Report(new ApplicationStatus() { Source = "Live Stack", Status = string.Empty });
                                }
                            } catch (Exception ex) {
                                Logger.Error(ex);
                            }
                        } catch (AbandonedMutexException) {
                            hasHandle = true;
                        } catch (TimeoutException) {
                            Logger.Debug("Waiting for exclusive access to start pixinsight has timed out");
                        } finally {
                            if (hasHandle) {
                                mutex.ReleaseMutex();
                            }
                        }
                    } catch(Exception ex) {
                        Logger.Error(ex);
                    }
                }
            });
        }
    }
}