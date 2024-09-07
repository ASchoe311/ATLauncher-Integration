using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.IO;
using Newtonsoft.Json;

namespace ATLauncherInstanceImporter
{

    //public class ATLauncher
    //{
    //    public string InstalLDir { get; }

    //    public string ExePath { get; }

    //    public string InstancePath { get; }

    //}

    public class ATLauncherInstanceImporter : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ATLauncherInstanceImporterSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("40e56f44-4955-40ec-9bf3-682c4007e55b");

        // Change to something more appropriate
        public override string Name => "ATLauncher";

        // Implementing Client adds ability to open it via special menu in playnite.
        public override LibraryClient Client { get; } = new ATLauncherInstanceImporterClient();

        public ATLauncherInstanceImporter(IPlayniteAPI api) : base(api)
        {
            settings = new ATLauncherInstanceImporterSettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                HasSettings = true
            };
        }

        private string GetCLIArgs()
        {
            string args = "";
            if (!settings.Settings.ShowATLauncherConsole)
            {
                args += " -no-console";
            }
            if (settings.Settings.CloseATLOnLaunch)
            {
                args += " -close-launcher";
            }
            return args;
        }

        private List<string> GetInstanceDirs()
        {
            return new List<string>(Directory.EnumerateDirectories(Path.Combine(settings.Settings.ATLauncherLoc, "Instances")));

        }

        private string GetInstanceName(string instanceDir)
        {
            JsonTextReader reader = new JsonTextReader(new StreamReader(Path.Combine(instanceDir, "instance.json")));
            while (reader.Read())
            {
                if ((string)reader.Value == "name")
                {
                    reader.Read();
                    return (string)reader.Value;
                }
            }
            return null;
        }

        private string GetLaunchString(string instanceDir)
        {
            return "-launch " + Path.GetFileName(instanceDir) + GetCLIArgs();
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            // Return list of user's games.
            List<GameMetadata> games = new List<GameMetadata>();
            foreach (var dir in GetInstanceDirs())
            {
                var instName = GetInstanceName(dir);
                games.Add(new GameMetadata()
                {
                    Name = instName != null ? instName : dir,
                    InstallDirectory = dir,
                    GameId = Path.GetFileName(dir).ToLower(),
                    GameActions = new List<GameAction>
                    {
                        new GameAction()
                        {
                            Type = GameActionType.File,
                            Path = Path.Combine(settings.Settings.ATLauncherLoc, "ATLauncher.exe"),
                            Arguments = GetLaunchString(dir),
                            WorkingDir = settings.Settings.ATLauncherLoc,
                            TrackingMode = TrackingMode.Default,
                            IsPlayAction = true
                        }
                    },
                    IsInstalled = true

                });
            }
            return games;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new ATLauncherInstanceImporterSettingsView();
        }

        //public void UpdateLaunchArgs()
        //{
        //    using (PlayniteApi.Database.BufferedUpdate())
        //    {
        //        foreach (var game in PlayniteApi.Database.Games)
        //        {
        //            //if (game.Source.ToString() == "ATLauncher")
        //            //{
        //            //    var actions = game.GameActions;
        //            //    actions[0].Arguments = GetLaunchString(game.InstallDirectory);
        //            //}
        //            logger.Debug(game.Source.ToString());
        //        }
        //    }
        //}

        //public void SetLauncher()
        //{

        //}

    }
}