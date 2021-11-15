using Microsoft.Win32;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Image.FileFormat.FITS;
using NINA.Image.FileFormat.XISF;
using NINA.Image.ImageData;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugins.PixInsightTools.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Newtonsoft.Json;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NINA.Plugins.PixInsightTools.Dockables;
using NINA.Image.Interfaces;

namespace NINA.Plugins.PixInsightTools {
    public class PixInsightToolsMediator {

        private PixInsightToolsMediator() { }

        private static readonly Lazy<PixInsightToolsMediator> lazy = new Lazy<PixInsightToolsMediator>(() => new PixInsightToolsMediator());

        public static PixInsightToolsMediator Instance { get => lazy.Value; }

        private LiveStackVM stackVM;
        public void RegisterLiveStackVM(LiveStackVM liveStackVM) {
            stackVM = liveStackVM;
        }

        public void AddFlatMaster(CalibrationFrame flat) {
            stackVM.AddFlatFrame(flat);
        }

        public Task StartLiveStack() {
            return stackVM.StartLiveStackCommand.ExecuteAsync(null);
        }

        public void StopLiveStack() {
            stackVM.CancelLiveStackCommand.Execute(null);
        }

    }

    [Export(typeof(IPluginManifest))]
    public class PixInsightToolsPlugin : PluginBase, INotifyPropertyChanged {
        [ImportingConstructor]
        public PixInsightToolsPlugin(IWindowServiceFactory windowServiceFactory, IImageDataFactory imageDataFactory) {
            if (Properties.Settings.Default.UpdateSettings) {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings( Properties.Settings.Default);
            }
            this.windowServiceFactory = windowServiceFactory;
            this.imageDataFactory = imageDataFactory;

            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.PixInsightLocation)) { 
                if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PixInsight", "bin"))) {
                    Properties.Settings.Default.PixInsightLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PixInsight", "bin", "PixInsight.exe");
                    CoreUtil.SaveSettings( Properties.Settings.Default);
                }
            }

            if (Properties.Settings.Default.DarkLibrary == null) {
                Properties.Settings.Default.DarkLibrary = new StringCollection();
                CoreUtil.SaveSettings( Properties.Settings.Default);
            }
            DarkLibrary = new AsyncObservableCollection<CalibrationFrame>(FromStringCollectionToList<CalibrationFrame>(Properties.Settings.Default.DarkLibrary));

            if(Properties.Settings.Default.BiasLibrary == null) {
                Properties.Settings.Default.BiasLibrary = new StringCollection();
                CoreUtil.SaveSettings( Properties.Settings.Default);
            }
            BiasLibrary = new AsyncObservableCollection<CalibrationFrame>(FromStringCollectionToList<CalibrationFrame>(Properties.Settings.Default.BiasLibrary));

            if (!Directory.Exists(Properties.Settings.Default.WorkingDirectory)) {
                Properties.Settings.Default.WorkingDirectory = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "N.I.N.A", "LiveStack");
                CoreUtil.SaveSettings( Properties.Settings.Default);
            }
            AddCalibrationFrameCommand = new AsyncCommand<bool>(async () => { await AddCalibrationFrame(); return true; });
            OpenPixinsightFileDiagCommand = new RelayCommand(OpenPixinsightFileDiag);
            OpenWorkingFolderDiagCommand = new RelayCommand(OpenWorkingFolderDiag);
            DeleteDarkMasterCommand = new RelayCommand(DeleteDarkMaster);
            DeleteBiasMasterCommand = new RelayCommand(DeleteBiasMaster);
        }

        private void DeleteBiasMaster(object obj) {
            if(obj is CalibrationFrame c) {
                BiasLibrary.Remove(c);
                Properties.Settings.Default.BiasLibrary = FromListToStringCollection(BiasLibrary);
                CoreUtil.SaveSettings( Properties.Settings.Default);
            }
        }

        private void DeleteDarkMaster(object obj) {
            if (obj is CalibrationFrame c) {
                DarkLibrary.Remove(c);
                Properties.Settings.Default.DarkLibrary = FromListToStringCollection(DarkLibrary);
                CoreUtil.SaveSettings( Properties.Settings.Default);
            }
        }

        private void OpenWorkingFolderDiag(object obj) {
            using (var diag = new System.Windows.Forms.FolderBrowserDialog()) {
                if(Directory.Exists(WorkingDirectory)) {
                    diag.SelectedPath = WorkingDirectory;
                }
                
                System.Windows.Forms.DialogResult result = diag.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK) {
                    if(Directory.Exists(diag.SelectedPath)) {
                        WorkingDirectory = diag.SelectedPath;
                    }
                }
            }
        }

        private void OpenPixinsightFileDiag(object obj) {
            var dialog = new OpenFileDialog();
            
            if(Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PixInsight", "bin"))) {
                dialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PixInsight", "bin");
            }
            
            dialog.Title = "PixInsight Executable";
            dialog.FileName = "PixInsight";
            dialog.DefaultExt = ".exe";
            dialog.Filter = "PixInsight Executable|*.exe";
            if (dialog.ShowDialog() == true) {
                if (File.Exists(dialog.FileName)) { 
                    PixInsightLocation = dialog.FileName;
                }
            }
        }

        private async Task AddCalibrationFrame() {
            var dialog = new OpenFileDialog();
            dialog.Title = "Add Calibration Frame";
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

                    if (metaData.Image.ImageType.ToLower() == "dark" || metaData.Image.ImageType.ToLower() == "'dark'") {
                        frame.Type = CalibrationFrameType.DARK;
                    } else  if (metaData.Image.ImageType.ToLower() == "bias" || metaData.Image.ImageType.ToLower() == "'bias'") {
                        frame.Type = CalibrationFrameType.BIAS;
                    }

                    frame.Gain = metaData.Camera.Gain;
                    frame.Offset = metaData.Camera.Offset;
                    frame.Filter = metaData.FilterWheel.Filter;
                    frame.ExposureTime = double.IsNaN(metaData.Image.ExposureTime) ? 0 : metaData.Image.ExposureTime;
                }
                if (properties != null) {
                    frame.Width = properties.Width;
                    frame.Height = properties.Height;
                }

                // todo - open wizard
                var service = windowServiceFactory.Create();
                var prompt = new CalibrationFramePrompt(frame);
                await service.ShowDialog(prompt, "Calibration Frame Wizard", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);

                if(prompt.Continue) { 
                    if(frame.Type == CalibrationFrameType.BIAS) {
                        BiasLibrary.Add(frame);
                        Properties.Settings.Default.BiasLibrary = FromListToStringCollection(BiasLibrary);
                        CoreUtil.SaveSettings( Properties.Settings.Default);
                    } else if(frame.Type == CalibrationFrameType.DARK) {
                        DarkLibrary.Add(frame);
                        Properties.Settings.Default.DarkLibrary = FromListToStringCollection(DarkLibrary);
                        CoreUtil.SaveSettings( Properties.Settings.Default);
                    }
                }
            }

        }

        public bool KeepCalibratedFiles {
            get => Properties.Settings.Default.KeepCalibratedFiles;
            set {
                Properties.Settings.Default.KeepCalibratedFiles = value;
                if(!value) {
                    CompressCalibratedFiles = false;
                }
                CoreUtil.SaveSettings( Properties.Settings.Default);
                RaisePropertyChanged();
            }
        }

        public bool CompressCalibratedFiles {
            get => Properties.Settings.Default.CompressCalibratedFiles;
            set {
                Properties.Settings.Default.CompressCalibratedFiles = value;
                CoreUtil.SaveSettings( Properties.Settings.Default);
                RaisePropertyChanged();
            }
        }

        public bool SaveAs16Bit {
            get => Properties.Settings.Default.SaveAs16Bit;
            set {
                Properties.Settings.Default.SaveAs16Bit = value;
                CoreUtil.SaveSettings( Properties.Settings.Default);
                RaisePropertyChanged();
            }
        }
        
        public int ResampleAmount {
            get => Properties.Settings.Default.ResampleAmount;
            set {
                Properties.Settings.Default.ResampleAmount = value;
                CoreUtil.SaveSettings( Properties.Settings.Default);
                RaisePropertyChanged();
            }
        }

        public int Pedestal {
            get => Properties.Settings.Default.Pedestal;
            set {
                Properties.Settings.Default.Pedestal = value;
                CoreUtil.SaveSettings( Properties.Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string PixInsightLocation {
            get {
                return Properties.Settings.Default.PixInsightLocation;
            }
            set {
                Properties.Settings.Default.PixInsightLocation = value;
                CoreUtil.SaveSettings( Properties.Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string WorkingDirectory {
            get {
                return Properties.Settings.Default.WorkingDirectory;
            }
            set {
                Properties.Settings.Default.WorkingDirectory = value;
                CoreUtil.SaveSettings( Properties.Settings.Default);
                RaisePropertyChanged();
            }
        }

        public static IList<T> FromStringCollectionToList<T>(StringCollection collection) {
            var l = new List<T>();
            foreach(var value in collection) {
                var item = JsonConvert.DeserializeObject<T>(value);
                l.Add(item);
            }
            return l;
        }

        public static StringCollection FromListToStringCollection<T>(IList<T> l) {
            var collection = new StringCollection();
            foreach (var value in l) {
                var item = JsonConvert.SerializeObject(value);
                collection.Add(item);
            }
            return collection;
        }

        public IAsyncCommand AddCalibrationFrameCommand { get; }
        public ICommand OpenPixinsightFileDiagCommand { get; }
        public ICommand OpenWorkingFolderDiagCommand  { get; }
        public ICommand DeleteDarkMasterCommand { get; }
        public ICommand DeleteBiasMasterCommand { get; }

        private AsyncObservableCollection<CalibrationFrame> darkLibrary;
        public AsyncObservableCollection<CalibrationFrame> DarkLibrary {
            get => darkLibrary;
            set {
                darkLibrary = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<CalibrationFrame> biasLibrary;
        private IWindowServiceFactory windowServiceFactory;
        private IImageDataFactory imageDataFactory;

        public AsyncObservableCollection<CalibrationFrame> BiasLibrary {
            get => biasLibrary;
            set {
                biasLibrary = value;
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
