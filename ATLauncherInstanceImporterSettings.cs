using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ATLauncherInstanceImporter
{
    public class ATLauncherInstanceImporterSettings : ObservableObject
    {
        private string aTLauncherLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ATLauncher");
        private bool showATLauncherConsole = false;
        private bool closeATLOnLaunch = true;

        public string ATLauncherLoc { get => aTLauncherLoc; set => SetValue(ref aTLauncherLoc, value); }
        public bool ShowATLauncherConsole { get => showATLauncherConsole; set => SetValue(ref showATLauncherConsole, value); }
        public bool CloseATLOnLaunch { get => closeATLOnLaunch; set => SetValue(ref closeATLOnLaunch, value); }
        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        //[DontSerialize]
        //public bool OptionThatWontBeSaved { get => optionThatWontBeSaved; set => SetValue(ref optionThatWontBeSaved, value); }
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
            if (Settings != editingClone)
            {
                logger.Info("ATLauncher Integration settings changed, updating games");
                plugin.UpdateLaunchArgs();
            }
        }

        public bool VerifySettings(out List<string> errors)
        {
            
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();

            if (File.Exists(Settings.ATLauncherLoc + "\\ATLauncher.exe"))
            {
                return true;
            }
            logger.Error(Settings.ATLauncherLoc + "ATLauncher.exe does not contain ATLauncher.exe");
            errors.Add("ATLauncher executable not found at given location");
            return false;
        }
    }

}