using Microsoft.Win32;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.FileFormat.FITS;
using NINA.Image.FileFormat.XISF;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using PixInsightTools.Model;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
            this.imageSaveMediator = imageSaveMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            this.windowServiceFactory = windowServiceFactory;
            this.imageDataFactory = imageDataFactory;

            this.workManager = new TaskManager();

            PixInsightToolsMediator.Instance.RegisterLiveStackVM(this);

            queue = new AsyncProducerConsumerQueue<LiveStackItem>(1000);
            FilterTabs = new AsyncObservableCollection<FilterTab>();

            StartLiveStackCommand = new AsyncCommand<bool>(async () => { await workManager.ExecuteOnceAsync(DoWork); return true; });
            CancelLiveStackCommand = new RelayCommand(CancelLiveStack);
            AddFlatFrameCommand = new AsyncCommand<bool>(async () => { await AddFlatFrame(); return true; });
            DeleteFlatMasterCommand = new RelayCommand(DeleteFlatMaster);
            RemoveTabCommand = new AsyncCommand<bool>((object o) => RemoveTab(o), (object o) => !SelectedTab?.Locked ?? false);
            AddColorCombinationCommand = new AsyncCommand<bool>(AddColorCombination, (object o) => FilterTabs?.Count > 1);
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

        private void DeleteFlatMaster(object obj) {
            if (obj is CalibrationFrame c) {
                FlatLibrary.Remove(c);
            }
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

        private void CancelLiveStack(object obj) {
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

        private async void ImageSaveMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            try {
                if(e.MetaData.Image.ImageType == NINA.Equipment.Model.CaptureSequence.ImageTypes.LIGHT || e.MetaData.Image.ImageType == NINA.Equipment.Model.CaptureSequence.ImageTypes.SNAPSHOT) { 
                    await queue.EnqueueAsync(new LiveStackItem(e.PathToImage.LocalPath, e.MetaData.Target.Name, e.Filter, e.MetaData.Image.ExposureTime, e.MetaData.Camera.Gain, e.MetaData.Camera.Offset, e.Image.PixelWidth, e.Image.PixelHeight));
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
            return $"STACK_{target}_{filter}.xisf";
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
            var tab = filterTabs.FirstOrDefault(x => x.Filter == filter && x.Target == target);

            if (tab != null) {
                tab.Locked = true;
            }

            return tab;
        }

        public static readonly string NOTARGET = "No target";
        public static readonly string NOFILTER = "No filter";
        public static readonly string RGB = "RGB";

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

                            // Just in case it crashed or was accidentally closed
                            await new PixInsightStartup(workingDir, slot).Run(progress, workerCTS.Token);

                            var target = string.IsNullOrWhiteSpace(item.Target) ? NOTARGET : item.Target;
                            var filter = string.IsNullOrWhiteSpace(item.Filter) ? NOFILTER : item.Filter;

                            tab = GetFilterTab(target, filter);

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
                            var alignedFile = string.Empty;
                            if (flat != null || dark != null || bias != null) {
                                var compress = false;
                                if (PixInsightToolsMediator.Instance.ToolsPlugin.KeepCalibratedFiles && PixInsightToolsMediator.Instance.ToolsPlugin.CompressCalibratedFiles) {
                                    compress = true;
                                }

                                var pedestal = PixInsightToolsMediator.Instance.ToolsPlugin.Pedestal;
                                var saveAs16Bit = PixInsightToolsMediator.Instance.ToolsPlugin.SaveAs16Bit;
                                calibratedFile = await new PixInsightCalibration(workingDir, slot, compress, pedestal, saveAs16Bit, flat?.Path ?? "", dark?.Path ?? "", bias?.Path ?? "").Run(item.Path, progress, workerCTS.Token);

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

                            if (PixInsightToolsMediator.Instance.ToolsPlugin.ResampleAmount > 1) {
                                resampledFile = await new PixInsightResample(referenceFile, PixInsightToolsMediator.Instance.ToolsPlugin.ResampleAmount, workingDir, slot).Run(progress, workerCTS.Token);
                                referenceFile = resampledFile;
                            }

                            var stackPath = string.Empty;
                            var count = tab?.Count ?? 1;
                            if (!(tab == null)) {
                                stackPath = tab.Path;
                                alignedFile = await new PixInsightAlign(referenceFile, stackPath, workingDir, slot).Run(progress, workerCTS.Token);

                                count = await new PixInsightStack(alignedFile, stackPath, workingDir, slot).Run(progress, workerCTS.Token);
                            } else {
                                stackPath = Path.Combine(workingDir, GetStackName(target, filter));

                                // Delete stack if it doesn't have a specific target and copy the reference file to be the new stack. If a specific target is present and a stack exists already, continue on that stack instead
                                if (!File.Exists(stackPath) || target == NOTARGET) {
                                    await TryDeleteFile(stackPath);
                                    File.Copy(referenceFile, Path.Combine(workingDir, GetStackName(target, filter)), true);
                                    Logger.Info($"Creating new stack {stackPath}");
                                } else {
                                    Logger.Info($"Continuing on existing stack {stackPath}");
                                }

                                tab = new FilterTab(filter, stackPath, target);
                                FilterTabs.Add(tab);
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
                                var colorTab = GetFilterTab(target, RGB);
                                if (colorTab != null && colorTab is ColorTab c) {
                                    c.Locked = true;
                                    var colorImage = await new PixInsightColorCombine(c.RedPath, c.GreenPath, c.BluePath, target, workingDir, slot).Run(progress, workerCTS.Token);

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

        private CalibrationFrame GetFlatMaster(LiveStackItem item) {
            if (FlatLibrary?.Count > 0) {
                var filter = string.IsNullOrWhiteSpace(item.Filter) ? NOFILTER : item.Filter;
                return FlatLibrary.FirstOrDefault(x => x.Filter == filter && x.Width == item.Width && x.Height == item.Height);
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
            if (BiasLibrary?.Count > 0) {
                return BiasLibrary.FirstOrDefault(x => x.Gain == item.Gain && x.Offset == item.Offset && x.Width == item.Width && x.Height == item.Height);
            }
            return null;
        }

        public IList<CalibrationFrame> FlatLibrary { get; } = new AsyncObservableCollection<CalibrationFrame>();
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

    public class ColorTab : FilterTab {

        public ColorTab(string target, string redPath, string greenPath, string bluePath) : base(LiveStackVM.RGB, "", target) {
            RedPath = redPath;
            GreenPath = greenPath;
            BluePath = bluePath;
        }

        public string RedPath { get; }
        public string GreenPath { get; }
        public string BluePath { get; }
    }

    public class FilterTab : BaseINPC {
        public string Filter { get; }
        public string Path { get; }
        public string Target { get; }
        private int count;

        public int Count {
            get => count;
            set {
                count = value;
                RaisePropertyChanged();
            }
        }

        public bool EnableABE { get; set; }
        public int ABEDegree { get; set; }

        private bool locked;

        public bool Locked {
            get {
                lock (lockobj) {
                    return locked;
                }
            }
            set {
                lock (lockobj) {
                    locked = value;
                }
                RaisePropertyChanged();
            }
        }

        private object lockobj = new object();

        public FilterTab(string filter, string path, string target) {
            Filter = string.IsNullOrWhiteSpace(filter) ? LiveStackVM.NOFILTER : filter;
            Path = path;
            Target = string.IsNullOrWhiteSpace(target) ? LiveStackVM.NOTARGET : target;
            Count = 0;
            Locked = false;
            EnableABE = false;
            ABEDegree = 3;
        }

        private BitmapSource stack;

        public BitmapSource Stack {
            get => stack;
            set {
                stack = value;
                RaisePropertyChanged();
            }
        }

        public string PngPath { get; set; }
    }

    public class LiveStackItem {

        public LiveStackItem(string path, string target, string filter, double exposureTime, int gain, int offset, int width, int height) {
            Path = path;
            Filter = filter;
            ExposureTime = exposureTime;
            Gain = gain;
            Offset = offset;
            Target = target;
            Width = width;
            Height = height;
        }

        public string Path { get; }
        public string Target { get; }
        public string Filter { get; }
        public double ExposureTime { get; }
        public int Gain { get; }
        public int Offset { get; }
        public int Width { get; }
        public int Height { get; }
    }

    public class TaskManager {
        private Task _currentTask;
        private object _lock = new object();

        public Task ExecuteOnceAsync(Func<Task> taskFactory) {
            if (_currentTask == null) {
                lock (_lock) {
                    if (_currentTask == null) {
                        Task concreteTask = taskFactory();
                        concreteTask.ContinueWith(o => { RemoveTask(); });
                        _currentTask = concreteTask;
                    }
                }
            }

            return _currentTask;
        }

        private void RemoveTask() {
            _currentTask = null;
        }
    }
}