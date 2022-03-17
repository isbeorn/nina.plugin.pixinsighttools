using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.FileFormat.XISF;
using NINA.Image.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces.Mediator;
using Nito.AsyncEx;
using PixInsightTools.Dockables;
using PixInsightTools.Model;
using PixInsightTools.Scripts;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixInsightTools.Instructions {

    [ExportMetadata("Name", "Stack flats")]
    [ExportMetadata("Description", "This instruction will calibrate and stack flat frames that are taken inside the current instruction set (and child instruction sets) and register the stacked flats master for the live stack. Place it after your flats.")]
    [ExportMetadata("Icon", "PixInsightTools_StackFlatsSVG")]
    [ExportMetadata("Category", "PixInsight Tools")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StackFlats : SequenceItem {
        private IImageSaveMediator imageSaveMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private IImageDataFactory imageDataFactory;
        private CancellationTokenSource cts;
        private AsyncProducerConsumerQueue<ImageSavedEventArgs> queue = new AsyncProducerConsumerQueue<ImageSavedEventArgs>(1000);
        private Dictionary<string, List<string>> FlatsToIntegrate = new Dictionary<string, List<string>>();
        private string workingDir { get => Properties.Settings.Default.WorkingDirectory; }
        private IList<CalibrationFrame> BiasLibrary { get => PixInsightToolsMediator.Instance.ToolsPlugin.BiasLibrary; }
        private Task workerTask;
        private int queueEntries;

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

        public int QueueEntries {
            get => queueEntries;
            set {
                queueEntries = value;
                RaisePropertyChanged();
            }
        }

        public override void SequenceBlockInitialize() {
            try {
                cts?.Dispose();
            } catch (Exception) { }

            cts = new CancellationTokenSource();

            FlatsToIntegrate.Clear();
            queue = new AsyncProducerConsumerQueue<ImageSavedEventArgs>(1000);
            imageSaveMediator.ImageSaved += ImageSaveMediator_ImageSaved;
            workerTask = StartCalibrationQueueWorker();
        }

        public override void SequenceBlockTeardown() {
            imageSaveMediator.ImageSaved -= ImageSaveMediator_ImageSaved;
            try {
                cts?.Cancel();
            } catch (Exception) { }
        }

        private void ImageSaveMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            if (e.MetaData.Image.ImageType == NINA.Equipment.Model.CaptureSequence.ImageTypes.FLAT) {
                try {
                    queue.Enqueue(e);
                    Interlocked.Increment(ref queueEntries);
                    RaisePropertyChanged(nameof(QueueEntries));
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
                    try {
                        cts.Token.ThrowIfCancellationRequested();
                        var item = await queue.DequeueAsync(cts.Token);

                        Logger.Info("Prepareing flat frame for Master Stack");
                        var bias = GetBiasMaster(item);

                        var flatToIntegrate = item.PathToImage.LocalPath;
                        if (bias != null) {
                            flatToIntegrate = await new PixInsightCalibration(workingDir,
                                    slot,
                                    false,
                                    0,
                                    false,
                                    string.Empty,
                                    string.Empty,
                                    bias.Path,
                                    PixInsightToolsMediator.Instance.ToolsPlugin.CalibrationPrefix,
                                    PixInsightToolsMediator.Instance.ToolsPlugin.CalibrationPostfix)
                                .Run(flatToIntegrate, default, default);
                        }

                        var filter = string.IsNullOrWhiteSpace(item.Filter) ? FilterTab.NOFILTER : item.Filter;
                        var destinationFolder = Path.Combine(workingDir, "calibrated", "flat", filter);
                        if (!Directory.Exists(destinationFolder)) {
                            Directory.CreateDirectory(destinationFolder);
                        }

                        var destinationFile = Path.Combine(destinationFolder, Path.GetFileName(flatToIntegrate));
                        if(bias != null) {
                            if(File.Exists(destinationFile)) { 
                                File.Delete(destinationFile); 
                            }
                            File.Move(flatToIntegrate, destinationFile);
                        } else {
                            File.Copy(flatToIntegrate, destinationFile, true);
                        }
                        

                        if (!FlatsToIntegrate.ContainsKey(filter)) {
                            FlatsToIntegrate[filter] = new List<string>();
                        }
                        FlatsToIntegrate[filter].Add(destinationFile);
                        Interlocked.Decrement(ref queueEntries);
                        RaisePropertyChanged(nameof(QueueEntries));
                    } catch(OperationCanceledException) {
                        throw;
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }
            } catch (OperationCanceledException) { }
        }
        private string RetrieveTarget(ISequenceContainer parent) {
            if (parent != null) {
                var container = parent as IDeepSkyObjectContainer;
                if (container != null) {
                    if(string.IsNullOrWhiteSpace(container.Target.DeepSkyObject.NameAsAscii)) {
                        return FilterTab.NOTARGET;
                    } else {
                        return container.Target.DeepSkyObject.NameAsAscii;
                    }
                    
                } else {
                    return RetrieveTarget(parent.Parent);
                }
            } else {
                return FilterTab.NOTARGET;
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            await Task.Run(async () => {
                Logger.Info("Stop listening for flat frames to calibrate");
                queue.CompleteAdding();
                imageSaveMediator.ImageSaved -= ImageSaveMediator_ImageSaved;

                if (queueEntries <= 0 && FlatsToIntegrate.Keys.Count == 0) {
                    Logger.Info("No flat frames to stack");
                } else {

                    Logger.Info("Finishing up remaining flat calibration");
                    progress?.Report(new ApplicationStatus() { Status = $"Waiting for flat calibration to finish" });
                    await workerTask;

                    foreach (var filter in FlatsToIntegrate.Keys) {
                        try {

                            var list = FlatsToIntegrate[filter].ToArray();

                            if (list.Length < 3) {
                                throw new Exception($"Not enough flats to generate masters for {filter}");
                            }

                            Logger.Info($"Generating flat master for filter {filter}");
                            progress?.Report(new ApplicationStatus() { Status = $"Generating flat master for filter {filter}" });
                            var flatsForIntegration = string.Join("|", list);
                            var target = RetrieveTarget(this.Parent);

                            var slot = 153;
                            var master = await new PixInsightFlatIntegration(workingDir, slot).Run(target, filter, flatsForIntegration, progress, token);

                            var xisf = await XISF.Load(new Uri(master), false, imageDataFactory, token);

                            Logger.Info($"Adding new flat master to live stack for filter {filter}");
                            PixInsightToolsMediator.Instance.AddFlatMaster(new CalibrationFrame(master, 0, 0, 0, filter) { Width = xisf.Properties.Width, Height = xisf.Properties.Height });

                            Logger.Info($"Cleaning up flat files for filter {filter}");
                            foreach (var file in list) {
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