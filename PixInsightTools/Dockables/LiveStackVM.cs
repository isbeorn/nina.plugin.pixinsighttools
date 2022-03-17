using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.FileFormat.FITS;
using NINA.Image.FileFormat.XISF;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel;
using Nito.AsyncEx;
using PixInsightTools.Model;
using PixInsightTools.Model.QualityGate;
using PixInsightTools.Prompts;
using PixInsightTools.Scripts;
using PixInsightTools.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PixInsightTools.Dockables {

    [Export(typeof(IDockableVM))]
    public class LiveStackVM : DockableVM {
        private IImageSaveMediator imageSaveMediator;
        private AsyncProducerConsumerQueue<LiveStackItem> queue;
        private CancellationTokenSource workerCTS;

        private IApplicationStatusMediator applicationStatusMediator;
        private IWindowServiceFactory windowServiceFactory;
        private IImageDataFactory imageDataFactory;
        private TaskManager workManager;
        private int slot;

        private string workingDir { get => PixInsightToolsMediator.Instance.ToolsPlugin.WorkingDirectory; }

        [ImportingConstructor]
        public LiveStackVM(IProfileService profileService, IImageSaveMediator imageSaveMediator, IApplicationStatusMediator applicationStatusMediator, IWindowServiceFactory windowServiceFactory, IImageDataFactory imageDataFactory) : base(profileService) {
            this.Title = "PixInsight Live Stacking";
            var dict = new ResourceDictionary();
            dict.Source = new Uri("PixInsightTools;component/Options.xaml", UriKind.RelativeOrAbsolute);
            ImageGeometry = (System.Windows.Media.GeometryGroup)dict["PixInsightTools_StackSVG"];
            ImageGeometry.Freeze();

            this.imageSaveMediator = imageSaveMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            this.windowServiceFactory = windowServiceFactory;
            this.imageDataFactory = imageDataFactory;

            this.workManager = new TaskManager();

            PixInsightToolsMediator.Instance.RegisterLiveStackVM(this);

            queue = new AsyncProducerConsumerQueue<LiveStackItem>(1000);
            FilterTabs = new AsyncObservableCollection<FilterTab>();
            QualityGates = new AsyncObservableCollection<IQualityGate>();

            StartLiveStackCommand = new AsyncCommand<bool>(async () => { await workManager.ExecuteOnceAsync(DoWork); return true; });
            CancelLiveStackCommand = new GalaSoft.MvvmLight.Command.RelayCommand(CancelLiveStack);
            AddFlatFrameCommand = new AsyncCommand<bool>(async () => { await AddFlatFrame(); return true; });
            DeleteFlatMasterCommand = new GalaSoft.MvvmLight.Command.RelayCommand<CalibrationFrame>(DeleteFlatMaster);
            RemoveTabCommand = new AsyncCommand<bool>((object o) => RemoveTab(o), (object o) => !SelectedTab?.Locked ?? false);
            AddColorCombinationCommand = new AsyncCommand<bool>(AddColorCombination, (object o) => FilterTabs?.Count > 1);
            AddQualityGateCommand = new AsyncCommand<bool>(AddQualityGate);
            DeleteQualityGateCommand = new GalaSoft.MvvmLight.Command.RelayCommand<IQualityGate>(DeleteQualityGate);

        }

        private void DeleteQualityGate(IQualityGate obj) {
            QualityGates.Remove(obj);
        }

        private async Task<bool> AddQualityGate() {
            var service = windowServiceFactory.Create();
            var prompt = new QualityGatePrompt();
            await service.ShowDialog(prompt, "Quality Gate Addition", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);

            if (prompt.Continue && prompt.SelectedGate != null) {
                QualityGates.Add(prompt.SelectedGate);
            }
            return prompt.Continue;
        }

        private async Task<bool> AddColorCombination(object arg) {
            var service = windowServiceFactory.Create();
            var prompt = new ColorCombinationPrompt(FilterTabs);
            await service.ShowDialog(prompt, "Color Combination Wizard", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);

            if (prompt.Continue) {
                if (!string.IsNullOrEmpty(prompt.Target)) {
                    var colorTab = new ColorTab(prompt.Target, prompt.RedChannel.PngPath, prompt.GreenChannel.PngPath, prompt.BlueChannel.PngPath);
                    colorTab.Locked = false;
                    FilterTabs.Add(colorTab);
                }
            }
            return prompt.Continue;
        }

        private void DeleteFlatMaster(CalibrationFrame c) {
            FlatLibrary.Remove(c);
        }

        private async Task<bool> RemoveTab(object obj) {
            if (obj is FilterTab selected) {
                var tab = FilterTabs.FirstOrDefault(x => x == selected);
                if (tab != null) {
                    FilterTabs.Remove(tab);
                    await TryDeleteFile(tab.Path);
                }
            }
            return true;
        }

        public void AddFlatFrame(CalibrationFrame flat) {
            FlatLibrary.Add(flat);
        }

        private async Task AddFlatFrame() {
            var dialog = new OpenFileDialog();
            dialog.Title = "Add Flat Frame";
            dialog.FileName = "";
            dialog.DefaultExt = ".xisf";
            dialog.Filter = "Extensible Image Serialization Format|*.xisf|Flexible Image Transport System|*.fits;*.fit";

            if (dialog.ShowDialog() == true) {
                var extension = Path.GetExtension(dialog.FileName).ToLower();
                ImageMetaData metaData;
                ImageProperties properties;
                if (extension == ".xisf") {
                    var xisf = await XISF.Load(new Uri(dialog.FileName), false, imageDataFactory, default);

                    metaData = xisf.MetaData;
                    properties = xisf.Properties;
                } else if (extension == ".fits" || extension == ".fit") {
                    var fits = await FITS.Load(new Uri(dialog.FileName), false, imageDataFactory, default);

                    metaData = fits.MetaData;
                    properties = fits.Properties;
                } else {
                    Notification.ShowError("Unsupported file format");
                    return;
                }

                CalibrationFrame frame = new CalibrationFrame(dialog.FileName);
                if (metaData != null) {
                    frame.Type = CalibrationFrameType.FLAT;

                    frame.Gain = metaData.Camera.Gain;
                    frame.Offset = metaData.Camera.Offset;
                    frame.Filter = metaData.FilterWheel.Filter;
                    frame.ExposureTime = double.IsNaN(metaData.Image.ExposureTime) ? 0 : metaData.Image.ExposureTime;
                }
                if (properties != null) {
                    frame.Width = properties.Width;
                    frame.Height = properties.Height;
                }

                var service = windowServiceFactory.Create();
                var prompt = new CalibrationFramePrompt(frame);
                await service.ShowDialog(prompt, "Calibration Frame Wizard", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);

                if (prompt.Continue) {
                    if (frame.Type == CalibrationFrameType.FLAT) {
                        AddFlatFrame(frame);
                    }
                }
            }
        }

        private void CancelLiveStack() {
            try {
                workerCTS?.Cancel();
            } catch (Exception) { }
        }

        public IAsyncCommand StartLiveStackCommand { get; }
        public ICommand CancelLiveStackCommand { get; }
        public IAsyncCommand AddFlatFrameCommand { get; }
        public ICommand DeleteFlatMasterCommand { get; }
        public ICommand RemoveTabCommand { get; }
        public IAsyncCommand AddColorCombinationCommand { get; }
        public ICommand AddQualityGateCommand { get; }
        public ICommand DeleteQualityGateCommand { get; }

        private async void ImageSaveMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            try {
                if (e.MetaData.Image.ImageType == NINA.Equipment.Model.CaptureSequence.ImageTypes.LIGHT || e.MetaData.Image.ImageType == NINA.Equipment.Model.CaptureSequence.ImageTypes.SNAPSHOT) {
                    await queue.EnqueueAsync(new LiveStackItem(e.PathToImage.LocalPath,
                                                               e.MetaData.Target.Name,
                                                               e.Filter,
                                                               e.MetaData.Image.ExposureTime,
                                                               e.MetaData.Camera.Gain,
                                                               e.MetaData.Camera.Offset,
                                                               e.Image.PixelWidth,
                                                               e.Image.PixelHeight,
                                                               e.IsBayered,
                                                               e.MetaData.Camera.BayerPattern,
                                                               e.MetaData,
                                                               e.Statistics,
                                                               e.StarDetectionAnalysis));
                }
            } catch (Exception) {
            }
        }

        private BitmapSource stackImage;

        public BitmapSource StackImage {
            get => stackImage;
            private set {
                stackImage = value;
                RaisePropertyChanged();
            }
        }

        private string GetStackName(string target, string filter) {
            return $"MASTER_LIGHT_{target}_{filter}.xisf";
        }

        private async Task TryDeleteFile(string path) {
            for (int i = 0; i < 3; i++) {
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

        private FilterTab GetFilterTab(string target, string filter) {
            return filterTabs.FirstOrDefault(x => x.Filter == filter && x.Target == target);
        }

        private async Task DoWork() {
            using (workerCTS = new CancellationTokenSource()) {
                try {
                    IsExpanded = false;

                    queue = new AsyncProducerConsumerQueue<LiveStackItem>(1000);
                    this.imageSaveMediator.ImageSaved += ImageSaveMediator_ImageSaved;

                    if (!Directory.Exists(workingDir)) {
                        Directory.CreateDirectory(workingDir);
                    }

                    slot = 153;
                    var progress = new Progress<ApplicationStatus>((p) => applicationStatusMediator.StatusUpdate(p));
                    await new PixInsightStartup(workingDir, slot).Run(progress, workerCTS.Token);

                    while (!workerCTS.IsCancellationRequested) {
                        FilterTab tab = null;
                        try {
                            applicationStatusMediator.StatusUpdate(new ApplicationStatus() { Source = "Live Stack", Status = "Waiting for next frame" });

                            var item = await queue.DequeueAsync(workerCTS.Token);

                            var failedGates = qualityGates.Where(x => !x.Passes(item));
                            if (failedGates.Count() > 0) {
                                var failedGatesInfo = "Live Stack - Image ignored as it does not meet quality gate critera." + Environment.NewLine + string.Join(Environment.NewLine, failedGates.Select(x => $"{x.Name}: {x.Value}"));
                                Logger.Warning(failedGatesInfo);
                                Notification.ShowWarning(failedGatesInfo);
                            } else { 
                                // Just in case it crashed or was accidentally closed
                                await new PixInsightStartup(workingDir, slot).Run(progress, workerCTS.Token);

                                var target = string.IsNullOrWhiteSpace(item.Target) ? FilterTab.NOTARGET : item.Target;
                                var filter = string.IsNullOrWhiteSpace(item.Filter) ? FilterTab.NOFILTER : item.Filter;

                                tab = GetFilterTab(target, filter);
                                if(tab != null) { 
                                    tab.Locked = true;
                                }

                                CalibrationFrame flat = GetFlatMaster(item);
                                CalibrationFrame dark = GetDarkMaster(item);
                                CalibrationFrame bias = null;
                                if (dark == null) {
                                    bias = GetBiasMaster(item);
                                }

                                bool calibrated = false;
                                var referenceFile = item.Path;
                                var calibratedFile = string.Empty;
                                var resampledFile = string.Empty;
                                var debayeredFile = string.Empty;
                                var alignedFile = string.Empty;
                                if (flat != null || dark != null || bias != null) {
                                    var compress = false;
                                    if (PixInsightToolsMediator.Instance.ToolsPlugin.KeepCalibratedFiles && PixInsightToolsMediator.Instance.ToolsPlugin.CompressCalibratedFiles) {
                                        compress = true;
                                    }

                                    var pedestal = PixInsightToolsMediator.Instance.ToolsPlugin.Pedestal;
                                    var saveAs16Bit = PixInsightToolsMediator.Instance.ToolsPlugin.SaveAs16Bit;
                                    
                                    calibratedFile = await new PixInsightCalibration(
                                            workingDir,
                                            slot,
                                            compress,
                                            pedestal,
                                            saveAs16Bit,
                                            flat?.Path ?? "",
                                            dark?.Path ?? "",
                                            bias?.Path ?? "",
                                            PixInsightToolsMediator.Instance.ToolsPlugin.CalibrationPrefix,
                                            PixInsightToolsMediator.Instance.ToolsPlugin.CalibrationPostfix)
                                        .Run(item.Path, progress, workerCTS.Token);

                                    if (PixInsightToolsMediator.Instance.ToolsPlugin.KeepCalibratedFiles) {
                                        var destinationFolder = Path.Combine(workingDir, "calibrated", target, filter);
                                        if (!Directory.Exists(destinationFolder)) {
                                            Directory.CreateDirectory(destinationFolder);
                                        }

                                        var destinationFileName = Path.Combine(destinationFolder, Path.GetFileName(calibratedFile));
                                        File.Move(calibratedFile, destinationFileName);
                                        calibratedFile = destinationFileName;
                                    }

                                    referenceFile = calibratedFile;
                                    calibrated = true;
                                }

                                if (item.IsBayered) {
                                    string pattern = item.BayerPattern.ToString();
                                    if (item.BayerPattern == BayerPatternEnum.Auto) {
                                        switch(item.MetaData.Camera.SensorType) {
                                            case SensorType.RGBG:
                                                pattern = BayerPatternEnum.RGBG.ToString();
                                                break;
                                            case SensorType.GRGB:
                                                pattern = BayerPatternEnum.GRGB.ToString();
                                                break;
                                            case SensorType.GRBG:
                                                pattern = BayerPatternEnum.GRBG.ToString();
                                                break;
                                            case SensorType.GBRG:
                                                pattern = BayerPatternEnum.GBRG.ToString();
                                                break;
                                            case SensorType.GBGR:
                                                pattern = BayerPatternEnum.GBGR.ToString();
                                                break;
                                            case SensorType.BGRG:
                                                pattern = BayerPatternEnum.BGRG.ToString();
                                                break;
                                            case SensorType.BGGR:
                                                pattern = BayerPatternEnum.BGGR.ToString();
                                                break;
                                            default:
                                                pattern = BayerPatternEnum.RGGB.ToString();
                                                break;
                                        }
                                    }
                                    debayeredFile = await new PixInsightDebayer(workingDir, slot).Run(referenceFile, pattern, progress, workerCTS.Token);
                                    referenceFile = debayeredFile;
                                }

                                if (PixInsightToolsMediator.Instance.ToolsPlugin.ResampleAmount > 1) {
                                    resampledFile = await new PixInsightResample(referenceFile, PixInsightToolsMediator.Instance.ToolsPlugin.ResampleAmount, workingDir, slot).Run(progress, workerCTS.Token);
                                    referenceFile = resampledFile;
                                }

                                var stackPath = string.Empty;
                                var count = tab?.Count ?? 1;
                                if (!(tab == null)) {
                                    stackPath = tab.Path;
                                    alignedFile = await new PixInsightAlign(referenceFile, stackPath, workingDir, slot).Run(progress, workerCTS.Token);

                                    count = await new PixInsightStack(alignedFile, stackPath, workingDir, slot).Run(item.IsBayered, progress, workerCTS.Token);
                                } else {
                                    stackPath = Path.Combine(workingDir, GetStackName(target, filter));

                                    // Delete stack if it doesn't have a specific target and copy the reference file to be the new stack. If a specific target is present and a stack exists already, continue on that stack instead
                                    if (!File.Exists(stackPath) || target == FilterTab.NOTARGET) {
                                        await TryDeleteFile(stackPath);
                                        File.Copy(referenceFile, Path.Combine(workingDir, GetStackName(target, filter)), true);
                                        Logger.Info($"Creating new stack {stackPath}");
                                    } else {
                                        Logger.Info($"Continuing on existing stack {stackPath}");
                                    }

                                    tab = new FilterTab(filter, stackPath, target);
                                    FilterTabs.Add(tab);
                                }

                                if (PixInsightToolsMediator.Instance.ToolsPlugin.EvaluateNoise) {
                                    var (noiseSigma, noisePercentage) = await new PixInsightNoiseEvaluation(stackPath, item.IsBayered, workingDir, slot).Run(progress, workerCTS.Token);
                                    tab.AddNoiseEvaluation(noiseSigma, noisePercentage);
                                }

                                if (item.IsBayered) {
                                    await TryDeleteFile(debayeredFile);
                                }

                                if (calibrated && !PixInsightToolsMediator.Instance.ToolsPlugin.KeepCalibratedFiles) {
                                    await TryDeleteFile(calibratedFile);
                                }

                                if (PixInsightToolsMediator.Instance.ToolsPlugin.ResampleAmount > 1) {
                                    await TryDeleteFile(resampledFile);
                                }

                                if (!string.IsNullOrEmpty(alignedFile)) {
                                    await TryDeleteFile(alignedFile);
                                }

                                var imagePath = await new PixInsightPostProcessing(stackPath, tab.EnableABE ? tab.ABEDegree : 0, workingDir, slot).Run(progress, workerCTS.Token);

                                applicationStatusMediator.StatusUpdate(new ApplicationStatus() { Source = "Live Stack", Status = $"Reloading image {target} - {filter} Frame" });

                                var decoder = new PngBitmapDecoder(new Uri(imagePath), BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                                tab.PngPath = imagePath;
                                tab.Stack = decoder.Frames[0];
                                tab.Stack.Freeze();

                                if (FilterTabs?.Count > 2) {
                                    var colorTab = GetFilterTab(target, ColorTab.RGB);
                                    if (colorTab != null && colorTab is ColorTab c 
                                            && (c.RedPath == tab.PngPath || c.GreenPath == tab.PngPath || c.BluePath == tab.PngPath) // Skip color combination if the current tab doesn't contain relevant data for the combination
                                    ) {
                                        c.Locked = true;
                                        var colorImage = await new PixInsightColorCombine(c.RedPath, c.GreenPath, c.BluePath, c.EnableSCNR, c.SCNRAmount, target, workingDir, slot).Run(progress, workerCTS.Token);

                                        var decoder2 = new PngBitmapDecoder(new Uri(colorImage), BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                                        c.Stack = decoder2.Frames[0];
                                        c.Stack.Freeze();
                                        c.Locked = false;
                                    }
                                }

                                if (count > 0) {
                                    tab.Count = count;
                                }

                                if (SelectedTab == null) {
                                    SelectedTab = tab;
                                }

                                tab.Locked = false;
                            }
                        } catch (OperationCanceledException) {
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        } finally {
                            if (tab != null) {
                                tab.Locked = false;
                            }

                            applicationStatusMediator.StatusUpdate(new ApplicationStatus() { Source = "Live Stack", Status = string.Empty });
                        }
                    }
                } finally {
                    this.imageSaveMediator.ImageSaved -= ImageSaveMediator_ImageSaved;
                    IsExpanded = true;
                }
            }
        }

        private FilterTab selectedTab;

        public FilterTab SelectedTab {
            get => selectedTab;
            set {
                selectedTab = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<FilterTab> filterTabs;

        public AsyncObservableCollection<FilterTab> FilterTabs {
            get => filterTabs;
            set {
                filterTabs = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<IQualityGate> qualityGates;
        public AsyncObservableCollection<IQualityGate> QualityGates {
            get => qualityGates;
            set {
                qualityGates = value;
                RaisePropertyChanged();
            }
        }

        private CalibrationFrame GetFlatMaster(LiveStackItem item) {
            var filter = string.IsNullOrWhiteSpace(item.Filter) ? FilterTab.NOFILTER : item.Filter;
            if (FlatLibrary?.Count > 0) {
                return FlatLibrary.FirstOrDefault(x => x.Filter == filter && x.Width == item.Width && x.Height == item.Height);
            }
            if (CrossSessionFlatLibrary?.Count > 0) {
                return CrossSessionFlatLibrary.FirstOrDefault(x => x.Filter == filter && x.Width == item.Width && x.Height == item.Height);
            }
            return null;
        }

        private CalibrationFrame GetDarkMaster(LiveStackItem item) {
            if (DarkLibrary?.Count > 0) {
                return DarkLibrary.FirstOrDefault(x => x.Gain == item.Gain && x.Offset == item.Offset && x.ExposureTime == item.ExposureTime && x.Width == item.Width && x.Height == item.Height);
            }
            return null;
        }

        private CalibrationFrame GetBiasMaster(LiveStackItem item) {
            if (PixInsightToolsMediator.Instance.ToolsPlugin.UseBiasForLights && BiasLibrary?.Count > 0) {
                return BiasLibrary.FirstOrDefault(x => x.Gain == item.Gain && x.Offset == item.Offset && x.Width == item.Width && x.Height == item.Height);
            }
            return null;
        }

        public IList<CalibrationFrame> FlatLibrary { get; } = new AsyncObservableCollection<CalibrationFrame>();
        public IList<CalibrationFrame> CrossSessionFlatLibrary { get => PixInsightToolsMediator.Instance.ToolsPlugin.FlatLibrary; }
        public IList<CalibrationFrame> DarkLibrary { get => PixInsightToolsMediator.Instance.ToolsPlugin.DarkLibrary; }
        public IList<CalibrationFrame> BiasLibrary { get => PixInsightToolsMediator.Instance.ToolsPlugin.BiasLibrary; }

        private bool isExpanded = true;

        public bool IsExpanded {
            get => isExpanded;
            set {
                isExpanded = value;
                RaisePropertyChanged();
            }
        }
    }
}