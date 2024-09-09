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
using System.Runtime;

namespace ATLauncherInstanceImporter
{

    public sealed class ATLauncher
    {
        public string InstallDir { get; }

        public string ExePath { get; }

        public string InstancePath { get; }

        public ATLauncher(string installDir)
        {
            InstallDir = installDir;
            ExePath = Path.Combine(installDir, "ATLauncher.exe");
            InstancePath = Path.Combine(installDir, "Instances");
        }

    }

    public class ATLauncherInstanceImporter : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ATLauncherInstanceImporterSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("40e56f44-4955-40ec-9bf3-682c4007e55b");

        // Change to something more appropriate
        public override string Name => "ATLauncher";


        // Implementing Client adds ability to open it via special menu in playnite.
        public override LibraryClient Client { get; }

        public ATLauncher Launcher { get; private set;  }

        public ATLauncherInstanceImporter(IPlayniteAPI api) : base(api)
        {
            settings = new ATLauncherInstanceImporterSettingsViewModel(this);
            SetClient();
            Properties = new LibraryPluginProperties
            {
                HasSettings = true
            };
            Client =  new ATLauncherInstanceImporterClient(this);
        }
        private void SetClient()
        {
            Launcher = new ATLauncher(settings.Settings.ATLauncherLoc);
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

        private class Instance
        {
            private string _Name = string.Empty;
            private string _MCVer = string.Empty;
            private List<Mod> _ModList = new List<Mod>();

            public string Name { get => _Name; set => _Name = value; }
            public string MCVer { get => _MCVer; set => _MCVer = value; }
            public List<Mod> ModList { get => _ModList; set => _ModList = value; }
            public class Mod
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

        }

        private string GenerateInstanceDescription(Instance instance)
        {
            logger.Info($"Generating description for instance {instance.Name}");
            string description = $"<h1>Minecraft Version: {instance.MCVer}</h1>";
            if (instance.ModList.Count == 0)
            {
                description += "<h1>No mods</h1>";
                return description;
            }
            description += "<h1>Mod List</h1><hr>";
            foreach (var mod in instance.ModList)
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

        private Instance GetInstanceInfo(string instanceDir)
        {
            logger.Info($"Getting instance information for {Path.GetFileName(instanceDir)}");
            List<Instance.Mod> modList = new List<Instance.Mod>();
            string jsonFile = File.ReadAllText(Path.Combine(instanceDir, "instance.json"));
            logger.Debug($"Attempting to deserialize JSON for {Path.Combine(instanceDir, "instance.json")}");
            dynamic json = JsonConvert.DeserializeObject(jsonFile);
            logger.Debug($"{json["launcher"]["name"]}");
            string instanceName = json["launcher"]["name"];
            string mcVersion = json["id"];
            foreach (var mod in json["launcher"]["mods"])
            {
                //logger.Debug($"Mod name is {mod["name"]}");
                List<string> authors = new List<string>();
                foreach (var auth in mod["curseForgeProject"]["authors"])
                {
                    //logger.Debug($"Author is {(string)auth["name"]}");
                    authors.Add((string)auth["name"]);
                }
                modList.Add(new Instance.Mod()
                {
                    Name = mod["name"],
                    Summary = mod["description"],
                    Authors = authors,
                    Link = mod["curseForgeProject"]["links"]["websiteUrl"]
                });
            }
            return new Instance()
            {
                Name = instanceName,
                MCVer = mcVersion,
                ModList = modList
            };
        }


        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            // Return list of user's games.
            List<GameMetadata> games = new List<GameMetadata>();
            foreach (var dir in GetInstanceDirs())
            {
                Instance instance = GetInstanceInfo(dir);
                logger.Info($"Discovered instance \"{instance.Name}\", adding to library");
                games.Add(new GameMetadata()
                {
                    Name = instance.Name != null ? instance.Name : dir,
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
                    Description = GenerateInstanceDescription(instance)
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
            SetClient();
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
        internal void DisplayLauncherError()
        {
            PlayniteApi.Dialogs.ShowErrorMessage(
                $"The path to your launcher installation isn't valid:\n{Launcher.InstancePath}",
                "ATLauncher Integration Plugin Erro"
            );
        }

        //public void SetLauncher()
        //{

        //}

    }
}