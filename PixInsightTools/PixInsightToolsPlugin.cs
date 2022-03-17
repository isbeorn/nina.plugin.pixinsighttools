using Microsoft.Win32;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Image.FileFormat.FITS;
using NINA.Image.FileFormat.XISF;
using NINA.Image.ImageData;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using PixInsightTools.Model;
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
using PixInsightTools.Dockables;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Profile;
using PixInsightTools.Prompts;

namespace PixInsightTools {
    public class PixInsightToolsMediator {

        private PixInsightToolsMediator() { }

        private static readonly Lazy<PixInsightToolsMediator> lazy = new Lazy<PixInsightToolsMediator>(() => new PixInsightToolsMediator());

        public static PixInsightToolsMediator Instance { get => lazy.Value; }
        public void RegisterPlugin(PixInsightToolsPlugin toolsPlugin) {
            this.ToolsPlugin = toolsPlugin;
        }

        private LiveStackVM stackVM;
        public PixInsightToolsPlugin ToolsPlugin { get; private set; }

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
        public PixInsightToolsPlugin(IProfileService profileService, IWindowServiceFactory windowServiceFactory, IImageDataFactory imageDataFactory) {
            if(DllLoader.IsX86()) {
                throw new Exception("This plugin is not available for x86 version of N.I.N.A.");
            }

            if (Properties.Settings.Default.UpdateSettings) {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings( Properties.Settings.Default);
            }
            PixInsightToolsMediator.Instance.RegisterPlugin(this);
            this.windowServiceFactory = windowServiceFactory;
            this.imageDataFactory = imageDataFactory;
            this.profileService = profileService;
            this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(this.Identifier));

            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.PixInsightLocation)) { 
                if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PixInsight", "bin"))) {
                    Properties.Settings.Default.PixInsightLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PixInsight", "bin", "PixInsight.exe");
                    CoreUtil.SaveSettings( Properties.Settings.Default);
                }
            }
                        
            DarkLibrary = new AsyncObservableCollection<CalibrationFrame>(FromStringToList<CalibrationFrame>(pluginSettings.GetValueString(nameof(DarkLibrary), "")));                        
            BiasLibrary = new AsyncObservableCollection<CalibrationFrame>(FromStringToList<CalibrationFrame>(pluginSettings.GetValueString(nameof(BiasLibrary), "")));
            FlatLibrary = new AsyncObservableCollection<CalibrationFrame>(FromStringToList<CalibrationFrame>(pluginSettings.GetValueString(nameof(FlatLibrary), "")));

            if (!Directory.Exists(Properties.Settings.Default.WorkingDirectory)) {
                Properties.Settings.Default.WorkingDirectory = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "N.I.N.A", "LiveStack");
                CoreUtil.SaveSettings( Properties.Settings.Default);
            }
            AddCalibrationFrameCommand = new AsyncCommand<bool>(async () => { await AddCalibrationFrame(); return true; });
            OpenPixinsightFileDiagCommand = new GalaSoft.MvvmLight.Command.RelayCommand(OpenPixinsightFileDiag);
            OpenWorkingFolderDiagCommand = new GalaSoft.MvvmLight.Command.RelayCommand(OpenWorkingFolderDiag);
            DeleteDarkMasterCommand = new GalaSoft.MvvmLight.Command.RelayCommand<CalibrationFrame>(DeleteDarkMaster);
            DeleteBiasMasterCommand = new GalaSoft.MvvmLight.Command.RelayCommand<CalibrationFrame>(DeleteBiasMaster);
            DeleteFlatMasterCommand = new GalaSoft.MvvmLight.Command.RelayCommand<CalibrationFrame>(DeleteFlatMaster);
            profileService.ProfileChanged += ProfileService_ProfileChanged;
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            DarkLibrary = new AsyncObservableCollection<CalibrationFrame>(FromStringToList<CalibrationFrame>(pluginSettings.GetValueString(nameof(DarkLibrary), "")));
            BiasLibrary = new AsyncObservableCollection<CalibrationFrame>(FromStringToList<CalibrationFrame>(pluginSettings.GetValueString(nameof(BiasLibrary), "")));
            FlatLibrary = new AsyncObservableCollection<CalibrationFrame>(FromStringToList<CalibrationFrame>(pluginSettings.GetValueString(nameof(FlatLibrary), "")));
            RaisePropertyChanged(nameof(KeepCalibratedFiles));
            RaisePropertyChanged(nameof(UseBiasForLights));
            RaisePropertyChanged(nameof(CompressCalibratedFiles));
            RaisePropertyChanged(nameof(SaveAs16Bit));
            RaisePropertyChanged(nameof(ResampleAmount));
            RaisePropertyChanged(nameof(Pedestal));
        }

        public override Task Teardown() {
            profileService.ProfileChanged -= ProfileService_ProfileChanged;
            return base.Teardown();
        }

        private void DeleteBiasMaster(CalibrationFrame c) {
            BiasLibrary.Remove(c);
            pluginSettings.SetValueString(nameof(BiasLibrary), FromListToString(BiasLibrary));
        }

        private void DeleteDarkMaster(CalibrationFrame c) {
            DarkLibrary.Remove(c);
            pluginSettings.SetValueString(nameof(DarkLibrary), FromListToString(DarkLibrary));
        }

        private void DeleteFlatMaster(CalibrationFrame c) {
            FlatLibrary.Remove(c);
            pluginSettings.SetValueString(nameof(FlatLibrary), FromListToString(FlatLibrary));
        }

        private void OpenWorkingFolderDiag() {
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

        private void OpenPixinsightFileDiag() {
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

                var service = windowServiceFactory.Create();
                var prompt = new CalibrationFramePrompt(frame);
                await service.ShowDialog(prompt, "Calibration Frame Wizard", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);

                if(prompt.Continue) { 
                    if(frame.Type == CalibrationFrameType.BIAS) {
                        BiasLibrary.Add(frame);
                        pluginSettings.SetValueString(nameof(BiasLibrary), FromListToString(BiasLibrary));
                    } else if(frame.Type == CalibrationFrameType.DARK) {
                        DarkLibrary.Add(frame);
                        pluginSettings.SetValueString(nameof(DarkLibrary), FromListToString(DarkLibrary));
                    } else if(frame.Type == CalibrationFrameType.FLAT) {
                        FlatLibrary.Add(frame);
                        pluginSettings.SetValueString(nameof(FlatLibrary), FromListToString(FlatLibrary));
                    }
                }
            }

        }

        public bool KeepCalibratedFiles {
            get => pluginSettings.GetValueBoolean(nameof(KeepCalibratedFiles), false);
            set {
                pluginSettings.SetValueBoolean(nameof(KeepCalibratedFiles), value);
                RaisePropertyChanged();
            }
        }

        public string CalibrationPrefix {
            get => pluginSettings.GetValueString(nameof(CalibrationPrefix), "");
            set {
                pluginSettings.SetValueString(nameof(CalibrationPrefix), CoreUtil.ReplaceInvalidFilenameChars(value));
                RaisePropertyChanged();
            }
        }

        public string CalibrationPostfix {
            get => pluginSettings.GetValueString(nameof(CalibrationPostfix), "_c");
            set {
                pluginSettings.SetValueString(nameof(CalibrationPostfix), CoreUtil.ReplaceInvalidFilenameChars(value));
                RaisePropertyChanged();
            }
        }

        public bool UseBiasForLights {
            get => pluginSettings.GetValueBoolean(nameof(UseBiasForLights), false);
            set {
                pluginSettings.SetValueBoolean(nameof(UseBiasForLights), value);
                RaisePropertyChanged();
            }
        }
       
        public bool CompressCalibratedFiles {
            get => pluginSettings.GetValueBoolean(nameof(CompressCalibratedFiles), false);
            set {
                pluginSettings.SetValueBoolean(nameof(CompressCalibratedFiles), value);
                RaisePropertyChanged();
            }
        }
        
        public bool SaveAs16Bit {
            get => pluginSettings.GetValueBoolean(nameof(SaveAs16Bit), false);
            set {
                pluginSettings.SetValueBoolean(nameof(SaveAs16Bit), value);
                RaisePropertyChanged();
            }
        }
        
        public int ResampleAmount {
            get => pluginSettings.GetValueInt32(nameof(ResampleAmount), 2);
            set {
                pluginSettings.SetValueInt32(nameof(ResampleAmount), value);
                RaisePropertyChanged();
            }
        }
        
        public int Pedestal {
            get => pluginSettings.GetValueInt32(nameof(Pedestal), 0);
            set {
                pluginSettings.SetValueInt32(nameof(Pedestal), value);
                RaisePropertyChanged();
            }
        }

        public bool EvaluateNoise {
            get => pluginSettings.GetValueBoolean(nameof(EvaluateNoise), true);
            set {
                pluginSettings.SetValueBoolean(nameof(EvaluateNoise), value);
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

        public static IList<T> FromStringToList<T>(string collection) {
            try {
                return JsonConvert.DeserializeObject<IList<T>>(collection) ?? new List<T>();
            } catch(Exception) {
                return new List<T>();
            }
            
        }

        public static string FromListToString<T>(IList<T> l) {       
            try { 
                return JsonConvert.SerializeObject(l) ?? "";
            } catch(Exception) {
                return "";
            }
        }

        public IAsyncCommand AddCalibrationFrameCommand { get; }
        public ICommand OpenPixinsightFileDiagCommand { get; }
        public ICommand OpenWorkingFolderDiagCommand  { get; }
        public ICommand DeleteDarkMasterCommand { get; }
        public ICommand DeleteBiasMasterCommand { get; }
        public ICommand DeleteFlatMasterCommand { get; }

        private AsyncObservableCollection<CalibrationFrame> darkLibrary;
        public AsyncObservableCollection<CalibrationFrame> DarkLibrary {
            get => darkLibrary;
            set {
                darkLibrary = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<CalibrationFrame> flatLibrary;
        public AsyncObservableCollection<CalibrationFrame> FlatLibrary {
            get => flatLibrary;
            set {
                flatLibrary = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<CalibrationFrame> biasLibrary;
        private IWindowServiceFactory windowServiceFactory;
        private IImageDataFactory imageDataFactory;
        private IProfileService profileService;
        private IPluginOptionsAccessor pluginSettings;

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
