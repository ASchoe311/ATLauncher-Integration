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
using System.Windows;

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
        private int vNum = 2;
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

        public void SetClient()
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
            if (Client.IsInstalled)
            {
                List<string> dirs = new List<string>();
                foreach (var dir in Directory.EnumerateDirectories(Path.Combine(settings.Settings.ATLauncherLoc, "instances")).Except(settings.Settings.InstanceIgnoreList))
                {
                    dirs.Add(dir);
                }
                return dirs;
                //return new List<string>(Directory.EnumerateDirectories(Path.Combine(settings.Settings.ATLauncherLoc, "Instances")));
            }
            PlayniteApi.Notifications.Add(new NotificationMessage(Guid.NewGuid().ToString(), ResourceProvider.GetString("LOCATLauncherNotFound"), NotificationType.Error));
            logger.Warn("Playnite tried to get ATLauncher instances, but ATLauncher location is not set");
            return new List<string>();

        }

        private string GetLaunchString(string instanceDir)
        {
            return "-launch " + Path.GetFileName(instanceDir) + GetCLIArgs();
        }

        public Models.Instance GetInstance(string instanceDir)
        {
            return Models.Instance.FromJson(File.ReadAllText(Path.Combine(instanceDir, "instance.json")));
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            // Return list of user's games.
            List<GameMetadata> games = new List<GameMetadata>();
            foreach (var dir in GetInstanceDirs())
            {
                logger.Info($"Discovered instance folder\"{dir}\", trying to add to library");
                try
                {
                    Models.Instance instance = GetInstance(dir);
                    Tuple<MetadataFile, MetadataFile, MetadataFile> imgs = Models.Instance.GetPackImages(instance, dir);
                    if (settings.Settings.AddMetadataOnImport)
                    {
                        games.Add(new GameMetadata()
                        {
                            Name = instance.Launcher.Name ?? instance.Launcher.Pack ?? Path.GetFileName(dir),
                            InstallDirectory = dir,
                            IsInstalled = true,
                            GameId = "atl-" + Path.GetFileName(dir).ToLower(),
                            Description = ATLauncherMetadataProvider.GenerateInstanceDescription(instance),
                            Source = new MetadataNameProperty("ATLauncher"),
                            Developers = instance.GetPackAuthors(),
                            Links = instance.GetPackLinks(),
                            ReleaseDate = instance.GetReleaseDate(),
                            Publishers = instance.GetInstancePublishers(),
                            Features = new HashSet<MetadataProperty> { new MetadataNameProperty("Single Player"), new MetadataNameProperty("Multiplayer") },
                            Genres = new HashSet<MetadataProperty> { new MetadataNameProperty("Sandbox"), new MetadataNameProperty("Survival") },
                            Platforms = new HashSet<MetadataProperty> { ATLauncherMetadataProvider.GetOS() },
                            Icon = imgs.Item1,
                            CoverImage = imgs.Item2,
                            BackgroundImage = imgs.Item3
                        });
                    }
                    else
                    {
                        games.Add(new GameMetadata()
                        {
                            Name = instance.Launcher.Name ?? instance.Launcher.Pack ?? Path.GetFileName(dir),
                            InstallDirectory = dir,
                            IsInstalled = true,
                            GameId = "atl-" + Path.GetFileName(dir).ToLower(),
                            Source = new MetadataNameProperty("ATLauncher")
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"An unrecoverable error occurred while trying to add instance located at {dir} to library:\n{ex.StackTrace}");
                    //PlayniteApi.Notifications.Add(new NotificationMessage(Path.GetFileName(dir), $"An unrecoverable error occurred while importing the ATLauncher instance at {dir}, please add it to the ignore list", NotificationType.Error));
                    string errMsg = $"{ResourceProvider.GetString("LOCATLauncherInstanceAt")}\n\n{dir}\n\n{ResourceProvider.GetString("LOCATLauncherAddError")}\n\n{ex.Message}.\n\n";
                    if (settings.Settings.AutoIgnoreInstances)
                    {
                        errMsg += $"{ResourceProvider.GetString("LOCATLauncherIgnoreAdded")}.\n\n";
                    }
                    errMsg += ResourceProvider.GetString("LOCATLauncherReportIssue");
                    PlayniteApi.Dialogs.ShowErrorMessage(errMsg, ResourceProvider.GetString("LOCATLauncherImportError"));
                    if (settings.Settings.AutoIgnoreInstances)
                    {
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            settings.Settings.InstanceIgnoreList.Add(dir);
                            SavePluginSettings(settings.Settings);
                        });
                    }
                    continue;
                }
            }
            return games;
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            if (settings.Settings.PluginVersion != vNum)
            {
                logger.Info("Detected first run of new plugin version, removing old actions and updating descriptions");
                PlayniteApi.Database.Games.BeginBufferUpdate();
                foreach (var game in PlayniteApi.Database.Games)
                {
                    if (game.PluginId != Id)
                    {
                        continue;
                    }
                    List<GameAction> actions = new List<GameAction>();
                    if (game.GameActions == null)
                    {
                        continue;
                    }
                    foreach (var action in game.GameActions)
                    {
                        if (action.IsPlayAction)
                        {
                            actions.Add(action);
                        }
                    }
                    foreach (var action in actions)
                    {
                        game.GameActions.Remove(action);
                    }
                    var inst = GetInstance(game.InstallDirectory);
                    game.Description = ATLauncherMetadataProvider.GenerateInstanceDescription(inst);
                }
                PlayniteApi.Database.Games.EndBufferUpdate();
                settings.Settings.PluginVersion = vNum;
                SavePluginSettings(settings.Settings);
                
            }
            base.OnApplicationStarted(args);
        }

        public override LibraryMetadataProvider GetMetadataDownloader()
        {
            return new ATLauncherMetadataProvider(this);
        }
        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new ATLauncherInstanceImporterSettingsView();
        }

        internal void DisplayLauncherError()
        {
            PlayniteApi.Dialogs.ShowErrorMessage(
                $"{ResourceProvider.GetString("LOCATLauncherInvalidInstall")}:\n{Launcher.InstancePath}",
                ResourceProvider.GetString("LOCATLauncherPluginError")
            );
        }

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != Id)
            {
                yield break;
            }
            AutomaticPlayController playController = new AutomaticPlayController(args.Game);
            playController.Name = ResourceProvider.GetString("LOCPlayGame");
            playController.Path = Path.Combine(settings.Settings.ATLauncherLoc, "ATLauncher.exe");
            playController.Arguments = GetLaunchString(args.Game.InstallDirectory);
            playController.WorkingDir = settings.Settings.ATLauncherLoc;
            playController.TrackingMode = TrackingMode.Default;

            yield return playController;
        }

        public class ATLauncherUninstallController : UninstallController
        {
            public ATLauncherUninstallController(Game game) : base(game)
            {
                Name = $"{ResourceProvider.GetString("LOCATLauncherUninstallName")} {game.Name}";
            }

            public override void Uninstall(UninstallActionArgs args)
            {
                logger.Info($"Deleting instance folder for {Game.Name} ({Game.InstallDirectory})");
                if (!Directory.Exists(Game.InstallDirectory))
                {
                    Playnite.SDK.API.Instance.Dialogs.ShowMessage($"{ResourceProvider.GetString("LOCATLauncherCantLocate")} {Game.Name}, {ResourceProvider.GetString("LOCATLauncherRemovingInstance")}");
                    InvokeOnUninstalled(new GameUninstalledEventArgs());
                    Playnite.SDK.API.Instance.Database.Games.Remove(Game.Id);
                    return;
                }
                Directory.Delete(Game.InstallDirectory, true);
                if (!Directory.Exists(Game.InstallDirectory))
                {
                    InvokeOnUninstalled(new GameUninstalledEventArgs());
                    Playnite.SDK.API.Instance.Dialogs.ShowMessage($"{ResourceProvider.GetString("LOCATLauncherRemovedInstance")} {Game.Name} {ResourceProvider.GetString("LOCATLauncherDeletedFiles")}");
                    Playnite.SDK.API.Instance.Database.Games.Remove(Game.Id);
                    return;
                }
                Playnite.SDK.API.Instance.Dialogs.ShowErrorMessage(
                    $"{ResourceProvider.GetString("LOCATLauncherUninstallWrong")} {Game.Name}",
                    ResourceProvider.GetString("LOCATLauncherUninstallerError")
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