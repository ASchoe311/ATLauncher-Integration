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
using Newtonsoft.Json.Linq;

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

        private class Mod
        {
            private string _Name = string.Empty;
            private List<string> _Authors = new List<string>();
            private string _Link = string.Empty;
            private string _Summary = string.Empty;

            public string Name { get => _Name; set => _Name = value; }
            public List<string> Authors { get => _Authors; set => _Authors = value; }
            public string Link { get => _Link; set => _Link = value; }
            public string Summary { get => _Summary; set => _Summary = value; }

        }

        private List<Mod> GetModList(string instanceDir)
        {
            List<Mod> modList = new List<Mod>();
            logger.Info($"Getting mod list for {Path.GetFileName(instanceDir)}");
            string jsonFile = File.ReadAllText(Path.Combine(instanceDir, "instance.json"));
            logger.Debug($"Attempting to deserialize JSON for {Path.Combine(instanceDir, "instance.json")}");
            dynamic json = JsonConvert.DeserializeObject(jsonFile);
            logger.Debug($"{json["launcher"]["name"]}");
            foreach (var mod in json["launcher"]["mods"])
            {
                //logger.Debug($"Mod name is {mod["name"]}");
                List<string> authors = new List<string>();
                foreach (var auth in mod["curseForgeProject"]["authors"])
                {
                    //logger.Debug($"Author is {(string)auth["name"]}");
                    authors.Add((string)auth["name"]);
                }
                modList.Add(new Mod()
                {
                    Name = mod["name"],
                    Summary = mod["description"],
                    Authors = authors,
                    Link = mod["curseForgeProject"]["links"]["websiteUrl"]
                });


            }
            return modList;
        }

        private string GenerateInstanceDescription(string instanceDir)
        {
            logger.Info($"Generating description for instance {Path.GetFileName(instanceDir)}");
            string description = "<h1>Mod List</h1><br>";
            foreach (Mod mod in GetModList(instanceDir))
            {
                description += $"<p><h2><a href={mod.Link}>{mod.Name}</a></h2>";
                string authString = "By";
                //logger.Debug($"{mod.Authors.Count()}");
                for (int i = 0; i < mod.Authors.Count(); i++)
                {
                    if (i == mod.Authors.Count() - 1 && i != 0)
                    {
                        authString += " and";
                    }
                    authString += " " + mod.Authors[i];
                    if (mod.Authors.Count() > 2 && i != mod.Authors.Count() - 1)
                    {
                        authString += $",";
                    }

                }
                description += $"<i>{authString}</i>";
                description += $"<h3>{mod.Summary}</h3></p><br>";
            }
            return description;
        }

        private string GetLaunchString(string instanceDir)
        {
            return "-launch " + Path.GetFileName(instanceDir) + GetCLIArgs();
        }

        private MetadataFile GetCoverImage(string instanceDir)
        {
            if (File.Exists(Path.Combine(instanceDir, "instance.png")))
            {
                return new MetadataFile(Path.Combine(instanceDir, "instance.png"));
            }
            return new MetadataFile(Path.Combine(settings.Settings.ATLauncherLoc, "configs\\images", "defaultimage.png"));
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            // Return list of user's games.
            List<GameMetadata> games = new List<GameMetadata>();
            foreach (var dir in GetInstanceDirs())
            {
                var instName = GetInstanceName(dir);
                logger.Info($"Discovered instance \"{instName}\", adding to library");
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
                    IsInstalled = true,
                    Source = new MetadataNameProperty("ATLauncher"),
                    Icon = new MetadataFile(Path.Combine(settings.Settings.ATLauncherLoc, "ATLauncher.exe")),
                    CoverImage = GetCoverImage(dir),
                    BackgroundImage = GetCoverImage(dir),
                    Description = GenerateInstanceDescription(dir)
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

        public void UpdateLaunchArgs()
        {
            PlayniteApi.Database.Games.BeginBufferUpdate();
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.PluginId != Id)
                {
                    continue;
                }
                logger.Info($"Updating launch arguments for instance {game.Name}");
                var action = game.GameActions[0];
                game.GameActions[0] = new GameAction
                {
                    Type = GameActionType.File,
                    Path = action.Path,
                    Arguments = GetLaunchString(game.InstallDirectory),
                    WorkingDir = settings.Settings.ATLauncherLoc,
                    TrackingMode = TrackingMode.Default,
                    IsPlayAction = true
                };
                PlayniteApi.Database.Games.Update(game);
            }
            PlayniteApi.Database.Games.EndBufferUpdate();
        }
        //public void SetLauncher()
        //{

        //}

    }
}