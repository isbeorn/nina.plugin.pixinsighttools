using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.FileFormat.XISF;
using NINA.Image.Interfaces;
using NINA.Plugins.PixInsightTools.Dockables;
using NINA.Plugins.PixInsightTools.Model;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces.Mediator;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugins.PixInsightTools.Instructions {

    [ExportMetadata("Name", "Stack flats")]
    [ExportMetadata("Description", "This instruction will calibrate and stack flat frames that are taken inside the current instruction set (and child instruction sets) and register the stacked flats for the live stack. Place it after your flats.")]
    [ExportMetadata("Icon", "")]
    [ExportMetadata("Category", "PixInsight Tools")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StackFlats : SequenceItem {
        private IImageSaveMediator imageSaveMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private IImageDataFactory imageDataFactory;
        private CancellationTokenSource cts;
        private AsyncProducerConsumerQueue<ImageSavedEventArgs> queue = new AsyncProducerConsumerQueue<ImageSavedEventArgs>(1000);
        private bool waitForProcessing;
        private Dictionary<string, List<string>> FlatsToIntegrate = new Dictionary<string, List<string>>();
        private string workingDir { get => Properties.Settings.Default.WorkingDirectory; }
        private IList<CalibrationFrame> BiasLibrary { get => PixInsightToolsMediator.Instance.ToolsPlugin.BiasLibrary; }
        private Task workerTask;

        [ImportingConstructor]
        public StackFlats(IImageSaveMediator imageSaveMediator, IApplicationStatusMediator applicationStatusMediator, IImageDataFactory imageDataFactory) {
            this.imageSaveMediator = imageSaveMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            this.imageDataFactory = imageDataFactory;
        }

        private StackFlats(StackFlats copyMe) : this(copyMe.imageSaveMediator, copyMe.applicationStatusMediator, copyMe.imageDataFactory) {
            CopyMetaData(copyMe);
        }

        public override object Clone() {
            var clone = new StackFlats(this) {
            };

            return clone;
        }

        public override void SequenceBlockInitialize() {
            imageSaveMediator.ImageSaved += ImageSaveMediator_ImageSaved;
            try {
                cts?.Dispose();
            } catch (Exception) { }

            cts = new CancellationTokenSource();
            workerTask = StartCalibrationQueueWorker();
        }

        public override void SequenceBlockTeardown() {
            imageSaveMediator.ImageSaved -= ImageSaveMediator_ImageSaved;
            try {
                cts?.Cancel();
            } catch (Exception) { }
        }

        private void ImageSaveMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            if (e.MetaData.Image.ImageType == Equipment.Model.CaptureSequence.ImageTypes.FLAT) {
                try {
                    queue.Enqueue(e);
                } catch (Exception) {
                }
            }
        }

        private async Task StartCalibrationQueueWorker() {
            try {
                if (!Directory.Exists(workingDir)) {
                    Directory.CreateDirectory(workingDir);
                }

                var slot = 153;
                var progress = new Progress<ApplicationStatus>((p) => applicationStatusMediator.StatusUpdate(p));
                await new PixInsightStartup(workingDir, slot).Run(progress, cts.Token);

                while (await queue.OutputAvailableAsync(cts.Token)) {
                    cts.Token.ThrowIfCancellationRequested();
                    var item = await queue.DequeueAsync(cts.Token);

                    var bias = GetBiasMaster(item);

                    var flatToIntegrate = item.PathToImage.LocalPath;
                    if (bias != null) {
                        flatToIntegrate = await new PixInsightCalibration(workingDir, slot, false, 0, false, string.Empty, string.Empty, bias.Path).Run(flatToIntegrate, default, default);
                    }

                    var filter = string.IsNullOrWhiteSpace(item.Filter) ? LiveStackVM.NOFILTER : item.Filter;
                    var destinationFolder = Path.Combine(workingDir, "calibrated", "flat", filter);
                    if (!Directory.Exists(destinationFolder)) {
                        Directory.CreateDirectory(destinationFolder);
                    }

                    var destinationFile = Path.Combine(destinationFolder, Path.GetFileName(flatToIntegrate));
                    File.Copy(flatToIntegrate, destinationFile, true);

                    if (!FlatsToIntegrate.ContainsKey(filter)) {
                        FlatsToIntegrate[filter] = new List<string>();
                    }
                    FlatsToIntegrate[filter].Add(destinationFile);
                }
            } catch (OperationCanceledException) { }
        }

        [JsonProperty]
        public bool WaitForProcessing {
            get => waitForProcessing;
            set {
                waitForProcessing = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var task = Task.Run(async () => {
                if (FlatsToIntegrate.Keys.Count == 0) {
                    Logger.Info("No flat frames to stack");
                } else {
                    Logger.Info("Stop listening for flat frames to calibrate");

                    queue.CompleteAdding();
                    imageSaveMediator.ImageSaved -= ImageSaveMediator_ImageSaved;

                    Logger.Info("Finishing up remaining flat calibration");
                    await workerTask;

                    foreach (var filter in FlatsToIntegrate.Keys) {
                        try {
                            Logger.Info($"Generating flat master for filter {filter}");
                            var flatsForIntegration = string.Join("|", FlatsToIntegrate[filter]);

                            var slot = 153;
                            var master = await new PixInsightFlatIntegration(workingDir, slot).Run(filter, flatsForIntegration, progress, token);

                            var xisf = await XISF.Load(new Uri(master), false, imageDataFactory, token);

                            Logger.Info($"Adding new flat master to live stack for filter {filter}");
                            PixInsightToolsMediator.Instance.AddFlatMaster(new CalibrationFrame(master, 0, 0, 0, filter) { Width = xisf.Properties.Width, Height = xisf.Properties.Height });

                            Logger.Info($"Cleaning up flat files for filter {filter}");
                            foreach (var file in FlatsToIntegrate[filter]) {
                                try {
                                    File.Delete(file);
                                } catch (Exception) {
                                }
                            }
                        } catch (Exception ex) {
                            Logger.Error($"Failed to generate flat master for filter {filter}", ex);
                        }
                    }
                }
            });

            if (WaitForProcessing) {
                await task;
            }
        }

        private CalibrationFrame GetBiasMaster(ImageSavedEventArgs item) {
            if (BiasLibrary?.Count > 0) {
                return BiasLibrary.FirstOrDefault(x => x.Gain == item.MetaData.Camera.Gain && x.Offset == item.MetaData.Camera.Offset && x.Width == item.Image.Width && x.Height == item.Image.Height);
            }
            return null;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(StackFlats)}";
        }
    }
}