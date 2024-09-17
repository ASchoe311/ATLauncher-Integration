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
using System.Net;
using System.Text.RegularExpressions;
using Playnite.SDK.Events;
using System.Reflection;

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

        internal static readonly string AssemblyPath = Path.GetDirectoryName(typeof(ATLauncherInstanceImporter).Assembly.Location);

        private static readonly string iconPath = Path.Combine(AssemblyPath, "icon.png");
        public override string LibraryIcon { get; } = iconPath;
        private ATLauncherInstanceImporterSettingsViewModel settings { get; set; }

        //private int _PluginVersion = 2;

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

        private string GenerateInstanceDescription(Models.Instance instance)
        {
            //logger.Info($"Generating description for instance {instance.Launcher.Name}");
            string description = string.Empty;
            if (instance.Launcher.Description != null)
            {
                description = $"<h2>{instance.Launcher.Description}</h2>";
            }
            description += $"<h1>Minecraft Version: {instance.McVersion}</h1>";
            if (instance.Launcher.IsVanilla.HasValue && instance.Launcher.IsVanilla.Value)
            {
                description += "<h1>No mods</h1>";
                return description;
            }
            description += $"<h1>Contains {instance.Launcher.Mods.Count} mods</h1>";
            description += "<h1>Mod List</h1><hr>";
            foreach (var mod in instance.Launcher.Mods)
            {
                description += "<p><h2>";
                if (mod.CurseForgeProject != null)
                {
                    description += mod.CurseForgeProject.Links.WebsiteUrl != string.Empty ? $"<a href={mod.CurseForgeProject.Links.WebsiteUrl}>{mod.Name}</a>" : $"{mod.Name}";
                }
                else if (mod.ModrinthProject != null)
                {
                    description += $"<a href=https://modrinth.com/mod/{mod.ModrinthProject.Slug}>{mod.Name}</a>";
                }
                else
                {
                    description += $"{mod.Name}";
                }
                description += "</h2>";
                var modAuths = Models.Instance.GetModAuthors(mod);
                string authString = modAuths.Count == 0 ? "No authors listed" : "By";
                //logger.Debug($"{mod.Authors.Count()}");
                for (int i = 0; i < modAuths.Count(); i++)
                {
                    if (i == modAuths.Count() - 1 && i != 0)
                    {
                        authString += " and";
                    }
                    authString += " " + modAuths[i];
                    if (modAuths.Count() > 2 && i != modAuths.Count() - 1)
                    {
                        authString += $",";
                    }

                }
                description += $"<i>{authString}</i>";
                description += $"<h3>{mod.Description}</h3></p><br>";
            }
            return description;
        }

        private string GetLaunchString(string instanceDir)
        {
            return "-launch " + Path.GetFileName(instanceDir) + GetCLIArgs();
        }

        private MetadataNameProperty GetOS()
        {
            int platform = (int)Environment.OSVersion.Platform;
            if (platform == 4 || platform == 128)
            {
                return new MetadataNameProperty("PC (Linux)");
            }
            if (platform == 6)
            {
                return new MetadataNameProperty("Macintosh");
            }
            if (platform == 2)
            {
                return new MetadataNameProperty("PC (Windows)");
            }
            return new MetadataNameProperty("Other");
        }

        //public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        //{
        //    base.OnApplicationStarted(args);
        //    logger.Info("Removing previously listed instances");
        //    if (settings.Settings.PluginVersion != null)
        //    {
        //        logger.Info($"Plugin version in settings is {settings.Settings.PluginVersion}, new version is {_PluginVersion}");
        //    }
        //    if (settings.Settings.PluginVersion == null || settings.Settings.PluginVersion < _PluginVersion)
        //    {
        //        PlayniteApi.Database.Games.BeginBufferUpdate();
        //        foreach (var game in PlayniteApi.Database.Games)
        //        {
        //            if (game.PluginId != Id)
        //            {
        //                continue;
        //            }
        //            Instance instance = GetInstanceInfo(game.InstallDirectory, settings.Settings.ATLauncherLoc);
        //            game.GameId = "atl-" + Path.GetFileName(game.InstallDirectory).ToLower();
        //            game.Icon = instance.PackIcon.ToString();
        //            //game.DeveloperIds = instance.Authors;
        //        }
        //        PlayniteApi.Database.Games.EndBufferUpdate();
        //    }
        //}

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            // Return list of user's games.
            List<GameMetadata> games = new List<GameMetadata>();
            foreach (var dir in GetInstanceDirs())
            {
                HashSet<MetadataProperty> defaultDevs = new HashSet<MetadataProperty>();
                HashSet<MetadataProperty> defaultPubs = new HashSet<MetadataProperty> { new MetadataNameProperty("Mojang Studios") };
                try
                {
                    string jsonStr = File.ReadAllText(Path.Combine(dir, "instance.json"));
                    logger.Info($"Discovered instance folder\"{dir}\", adding to library");
                    Models.Instance instance = Models.Instance.FromJson(jsonStr);
                    Tuple<MetadataFile, MetadataFile> imgs = Models.Instance.GetPackImages(instance, dir);
                    games.Add(new GameMetadata()
                    {
                        Name = instance.Launcher.Name != null ? instance.Launcher.Name : instance.Launcher.Pack,
                        InstallDirectory = dir,
                        GameId = "atl-" + Path.GetFileName(dir).ToLower(),
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
                        Icon = imgs.Item1,
                        CoverImage = imgs.Item2,
                        BackgroundImage = imgs.Item2,
                        Description = GenerateInstanceDescription(instance),
                        Developers = instance.GetPackAuthors(),
                        Links = instance.GetPackLinks(),
                        ReleaseDate = instance.GetReleaseDate(),
                        Features = new HashSet<MetadataProperty> { new MetadataNameProperty("Single Player"), new MetadataNameProperty("Multiplayer") },
                        Publishers = instance.GetInstancePublishers(),
                        Genres = new HashSet<MetadataProperty> { new MetadataNameProperty("Sandbox"), new MetadataNameProperty("Survival") },
                        Platforms = new HashSet<MetadataProperty> { GetOS() }

                    });
                }
                catch (Exception e)
                {
                    logger.Warn($"Skipping full metadata import of the instance at {dir} due to the following error: {e.StackTrace}");
                    games.Add(new GameMetadata()
                    {
                        Name = Path.GetFileName(dir),
                        InstallDirectory = dir,
                        GameId = "atl-" + Path.GetFileName(dir).ToLower(),
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
                        CoverImage = new MetadataFile(Path.Combine(settings.Settings.ATLauncherLoc, "configs\\images", "defaultimage.png")),
                        BackgroundImage = new MetadataFile(Path.Combine(settings.Settings.ATLauncherLoc, "configs\\images", "defaultimage.png")),
                        Developers = defaultDevs,
                        Features = new HashSet<MetadataProperty> { new MetadataNameProperty("Single Player"), new MetadataNameProperty("Multiplayer") },
                        Publishers = new HashSet<MetadataProperty> { new MetadataNameProperty("Mojang Studios") },
                        Genres = new HashSet<MetadataProperty> { new MetadataNameProperty("Sandbox"), new MetadataNameProperty("Survival") },
                        Description = $"<h2>No metadata imported due to error in instance scanning, please report the following stacktrace as an issue on github: \n{e.StackTrace}</h2>",
                        Platforms = new HashSet<MetadataProperty> { GetOS() }

                    });
                }
                //dynamic json = JsonConvert.SerializeObject(instance);
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
                "ATLauncher Integration Plugin Error"
            );
        }

        public class ATLauncherUninstallController : UninstallController
        {
            public ATLauncherUninstallController(Game game) : base(game)
            {
                Name = $"Uninstall (delete instance folder for) {game.Name}";
            }

            public override void Uninstall(UninstallActionArgs args)
            {
                logger.Info($"Deleting instance folder for {Game.Name} ({Game.InstallDirectory})");
                if (!Directory.Exists(Game.InstallDirectory))
                {
                    Playnite.SDK.API.Instance.Dialogs.ShowMessage($"Cannot locate instance folder for {Game.Name}, removing from Playnite");
                    InvokeOnUninstalled(new GameUninstalledEventArgs());
                    Playnite.SDK.API.Instance.Database.Games.Remove(Game.Id);
                    return;
                }
                Directory.Delete(Game.InstallDirectory, true);
                if (!Directory.Exists(Game.InstallDirectory))
                {
                    InvokeOnUninstalled(new GameUninstalledEventArgs());
                    Playnite.SDK.API.Instance.Dialogs.ShowMessage($"Removed instance {Game.Name} and deleted files");
                    Playnite.SDK.API.Instance.Database.Games.Remove(Game.Id);
                    return;
                }
                Playnite.SDK.API.Instance.Dialogs.ShowErrorMessage(
                    $"Something went wrong deleting instance folder for {Game.Name}",
                    "ATLauncher Integration Instance Uninstaller Error"
                );
            }
        }
            public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
        {
            if (args.Game.PluginId != Id)
            {
                yield break;
            }

            yield return new ATLauncherUninstallController(args.Game);
        }
    }
}