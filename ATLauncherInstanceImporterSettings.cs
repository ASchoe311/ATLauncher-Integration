using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace ATLauncherInstanceImporter
{
    public class ATLauncherInstanceImporterSettings : ObservableObject
    {

        private static readonly ILogger logger = LogManager.GetLogger();
        
        private string aTLauncherLoc = string.Empty;
        private bool showATLauncherConsole = false;
        private bool closeATLOnLaunch = true;
        private bool _AddMetadataOnImport = true;
        private bool _AutoIgnoreInstances = true;
        private int _PluginVersion = 1;

        public string ATLauncherLoc { get => aTLauncherLoc; set => SetValue(ref aTLauncherLoc, value); }
        public bool ShowATLauncherConsole { get => showATLauncherConsole; set => SetValue(ref showATLauncherConsole, value); }
        public bool CloseATLOnLaunch { get => closeATLOnLaunch; set => SetValue(ref closeATLOnLaunch, value); }
        public bool AddMetadataOnImport { get => _AddMetadataOnImport; set => SetValue(ref _AddMetadataOnImport, value); }
        public ObservableCollection<string> InstanceIgnoreList { get; set; } = new ObservableCollection<string>();
        public int PluginVersion { get => _PluginVersion; set => SetValue(ref _PluginVersion, value);  }
        public bool AutoIgnoreInstances { get => _AutoIgnoreInstances; set => SetValue(ref _AutoIgnoreInstances, value); }

    }

    public class ATLauncherInstanceImporterSettingsViewModel : ObservableObject, ISettings
    {
        private readonly ATLauncherInstanceImporter plugin;
        private ATLauncherInstanceImporterSettings editingClone { get; set; }

        private ATLauncherInstanceImporterSettings settings;
        public ATLauncherInstanceImporterSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        private ILogger logger = LogManager.GetLogger();

        [DontSerialize]
        public RelayCommand AddIgnoreCommand
        {
            get => new RelayCommand(() =>
            {
                var folder = Playnite.SDK.API.Instance.Dialogs.SelectFolder();
                if (!string.IsNullOrEmpty(folder))
                {
                    Settings.InstanceIgnoreList.Add(folder);
                }
            });
        }

        [DontSerialize]
        public RelayCommand<string> RemoveIgnoreCommand
        {
            get => new RelayCommand<string>((a) =>
            {
                if (a != null)
                {
                    Settings.InstanceIgnoreList.Remove(a);
                }
            });
        }

        private string TryGetATLauncherPath()
        {
            foreach (var user in Registry.Users.GetSubKeyNames())
            {
                //Console.WriteLine(user);
                var subkey = Registry.Users.OpenSubKey(user + @"\Software\Microsoft\Windows\CurrentVersion\Uninstall\{2F5FDA11-45A5-4CC3-8E51-5E11E2481697}_is1");
                if (subkey != null)
                {
                    if (subkey.GetValue("InstallLocation") != null)
                    {
                        logger.Debug("Got ATLauncher location from registry");
                        return subkey.GetValue("InstallLocation").ToString();
                    }
                }
            }
            logger.Debug("Couldn't get ATLauncher location from registry, leaving blank");
            return string.Empty;
        }

        public ATLauncherInstanceImporterSettingsViewModel(ATLauncherInstanceImporter plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<ATLauncherInstanceImporterSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new ATLauncherInstanceImporterSettings();
                Settings.ATLauncherLoc = TryGetATLauncherPath();
                plugin.SavePluginSettings(Settings);
            }

        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
            if (Settings.CloseATLOnLaunch != editingClone.CloseATLOnLaunch || Settings.ShowATLauncherConsole != editingClone.ShowATLauncherConsole)
            {
                logger.Info("ATLauncher Integration launch options changed, updating games");
                plugin.UpdateLaunchArgs();
            }
            if (Settings.ATLauncherLoc != editingClone.ATLauncherLoc)
            {
                plugin.SetClient();
            }
        }

        public bool VerifySettings(out List<string> errors)
        {
            
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();

            if (File.Exists(Settings.ATLauncherLoc + "\\ATLauncher.exe") || Settings.ATLauncherLoc == string.Empty)
            {
                return true;
            }
            logger.Error(Settings.ATLauncherLoc + "ATLauncher.exe does not contain ATLauncher.exe");
            errors.Add("ATLauncher executable not found at given location");
            return false;
        }
    }

}