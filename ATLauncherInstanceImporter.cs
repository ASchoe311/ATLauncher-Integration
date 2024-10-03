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
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Threading;
using ATLauncherInstanceImporter.Models;

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
        private int vNum = 3;
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

        /// <summary>
        /// Gets optional arguments for ATLauncher CLI
        /// </summary>
        /// <returns>A string containing optional arguments to pass to ATLauncher CLI</returns>
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

        /// <summary>
        /// Enumerates all directories containing instances
        /// </summary>
        /// <returns>A list of strings representing paths to instance folders</returns>
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
            PlayniteApi.Notifications.Add(new NotificationMessage("ATLauncherNotFound", ResourceProvider.GetString("LOCATLauncherNotFound"), NotificationType.Error));
            logger.Warn("Playnite tried to get ATLauncher instances, but ATLauncher location is not set");
            return new List<string>();

        }

        /// <summary>
        /// Gets the string used to launch the instance located at <c>instanceDir</c>
        /// </summary>
        /// <param name="instanceDir">The directory containing the instance</param>
        /// <returns>A string representing the launch argument to pass to ATLauncher CLI</returns>
        private string GetLaunchString(string instanceDir)
        {
            return "-launch " + Path.GetFileName(instanceDir) + GetCLIArgs();
        }

        /// <summary>
        /// Deserealizes instance.json for an ATLauncher instance into an <c>Instance</c> object
        /// </summary>
        /// <param name="instanceDir">The directory containing the instance</param>
        /// <returns>An <c>Instance</c> object containing information about an ATLauncher instance</returns>
        public static Models.Instance GetInstance(string instanceDir)
        {
            return Models.Instance.FromJson(File.ReadAllText(Path.Combine(instanceDir, "instance.json")));
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            // Return list of user's games.
            List<GameMetadata> games = new List<GameMetadata>();
            foreach (var dir in GetInstanceDirs())
            {
                logger.Debug($"Discovered instance folder\"{dir}\", trying to add to library");
                try
                {
                    Models.Instance instance = GetInstance(dir);
                    Tuple<MetadataFile, MetadataFile, MetadataFile> imgs = Models.Instance.GetPackImages(instance, dir, settings.Settings.ResizeCovers, GetPluginUserDataPath());
                    string instanceName = ChangeInstanceName(settings.Settings.NameFormat, dir);
                    if (settings.Settings.AddMetadataOnImport)
                    {
                        games.Add(new GameMetadata()
                        {
                            Name = instanceName,
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
                            Name = instanceName,
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
                        //Pass back to foreground thread
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            settings.Settings.InstanceIgnoreList.Add(dir);
                            SavePluginSettings(settings.Settings);
                            //PlayniteApi.MainView.OpenPluginSettings(Id);
                        });
                    }
                    continue;
                }
            }
            return games;
        }

        /// <summary>
        /// Asynchronously removes old play actions and updates instance descriptions with new data on first application start with new plugin version
        /// </summary>
        /// <param name="args"></param>
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Ensure image cache directory exists
            if (!Directory.Exists(Path.Combine(GetPluginUserDataPath(), "ImageCache")))
            {
                Directory.CreateDirectory(Path.Combine(GetPluginUserDataPath(), "ImageCache"));
            }

            // Only run on first install of new plugin version
            if (settings.Settings.PluginVersion != vNum)
            {
                logger.Info("Detected first run of new plugin version, removing old actions and updating descriptions");
                AsyncFRU();
                settings.Settings.PluginVersion = vNum;
                SavePluginSettings(settings.Settings);
                
            }
            base.OnApplicationStarted(args);
        }

        /// <summary>
        /// Async started for first run update
        /// </summary>
        private async void AsyncFRU()
        {
            await Task.Run(() => FRU());
        }

        /// <summary>
        /// Handles removal of old play actions and updating of descriptions
        /// </summary>
        private void FRU()
        {
            PlayniteApi.Database.Games.BeginBufferUpdate();
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.PluginId != Id)
                {
                    continue;
                }
                List<GameAction> actions = new List<GameAction>();
                if (game.GameActions != null)
                {
                    foreach (var action in game.GameActions)
                    {
                        if (action.IsPlayAction)
                        {
                            actions.Add(action);
                        }
                    }
                }
                foreach (var action in actions)
                {
                    game.GameActions.Remove(action);
                }
                var inst = GetInstance(game.InstallDirectory);                
                game.Description = ATLauncherMetadataProvider.GenerateInstanceDescription(inst);
                PlayniteApi.Database.Games.Update(game);
            }
            PlayniteApi.Database.Games.EndBufferUpdate();
        }

        public override LibraryMetadataProvider GetMetadataDownloader()
        {
            return new ATLauncherMetadataProvider(this, settings.Settings.ResizeCovers);
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
            string _dataPath;
            public ATLauncherUninstallController(Game game, string dataPath) : base(game)
            {
                Name = $"{ResourceProvider.GetString("LOCATLauncherUninstallName")} {game.Name}";
                _dataPath = dataPath;
            }

            public override void Uninstall(UninstallActionArgs args)
            {
                logger.Info($"Deleting instance folder for {Game.Name} ({Game.InstallDirectory})");
                if (!Directory.Exists(Game.InstallDirectory))
                {
                    Playnite.SDK.API.Instance.Dialogs.ShowMessage($"{ResourceProvider.GetString("LOCATLauncherCantLocate")} {Game.Name}\n\n{ResourceProvider.GetString("LOCATLauncherRemovingInstance")}");
                    InvokeOnUninstalled(new GameUninstalledEventArgs());
                    Playnite.SDK.API.Instance.Database.Games.Remove(Game.Id);
                    return;
                }
                try
                {
                    // Get uuid of instance
                    var input = File.ReadAllText(Path.Combine(Game.InstallDirectory, "instance.json"));
                    var template = new {uuid = string.Empty};
                    var result = JsonConvert.DeserializeAnonymousType(input, template);

                    // Use uuid to delete any cached cover images
                    if (result != null)
                    {
                        foreach (var file in Directory.EnumerateFiles(Path.Combine(_dataPath, "ImageCache")))
                        {
                            if (file.Contains(result.uuid))
                            {
                                File.Delete(file);
                            }
                        }
                    }

                    // Delete instance folder
                    Directory.Delete(Game.InstallDirectory, true);
                    if (!Directory.Exists(Game.InstallDirectory))
                    {
                        InvokeOnUninstalled(new GameUninstalledEventArgs());
                        Playnite.SDK.API.Instance.Dialogs.ShowMessage($"{ResourceProvider.GetString("LOCATLauncherRemovedInstance")} {Game.Name} {ResourceProvider.GetString("LOCATLauncherDeletedFiles")}");
                        Playnite.SDK.API.Instance.Database.Games.Remove(Game.Id);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Playnite.SDK.API.Instance.Dialogs.ShowErrorMessage(
                        $"{ResourceProvider.GetString("LOCATLauncherUninstallWrong")} {Game.Name}\n\n{e.Message}",
                        ResourceProvider.GetString("LOCATLauncherUninstallerError")
                    );
                    Game.IsUninstalling = false;
                }
            }
        }
            public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
        {
            if (args.Game.PluginId != Id)
            {
                yield break;
            }

            yield return new ATLauncherUninstallController(args.Game, GetPluginUserDataPath());
        }

        /// <summary>
        /// Updates cover images for ATLauncher instances and displays a progress bar
        /// </summary>
        /// <param name="toPortrait"><c>Bool</c> determining if the new cover should be default or portrait</param>
        public void ResizeCoversProgress(bool toPortrait)
        {
            List<Game> instances = new List<Game>();
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.PluginId == Id)
                {
                    instances.Add(game);
                }
            }
            logger.Debug($"changing {instances.Count} instance covers");
            GlobalProgressOptions gpo = new GlobalProgressOptions(ResourceProvider.GetString("LOCATLauncherChangingCovers"), false);
            gpo.IsIndeterminate = false;

            // Change covers with progress dialog
            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                activateGlobalProgress.ProgressMaxValue = instances.Count;
                activateGlobalProgress.CurrentProgressValue = -1;
                PlayniteApi.Database.Games.BeginBufferUpdate();
                foreach(var i in instances)
                {
                    activateGlobalProgress.CurrentProgressValue += 1;
                    string imgPath = ResizeCover(i.InstallDirectory, toPortrait, GetPluginUserDataPath());
                    i.CoverImage = imgPath;
                    PlayniteApi.Database.Games.Update(i);
                    //Thread.Sleep(2000);
                }
                PlayniteApi.Database.Games.EndBufferUpdate();
            }, gpo);
        }

        /// <summary>
        /// Gets the new cover for the instance
        /// </summary>
        /// <param name="g"><c>Game</c> object representing an ATLauncher instance</param>
        /// <param name="toPortrait"><c>Bool</c> determining if the new cover should be default or portrait</param>
        /// <returns>A string containing the path to the new cover image</returns>
        internal string ResizeCover(string installDir,  bool toPortrait, string dataPath)
        {
            //logger.Debug("Resizing cover for " + g.Name);
            var instance = GetInstance(installDir);
            // Try to get the desired cover from the image cache
            if (toPortrait && File.Exists(Path.Combine(dataPath, "ImageCache", $"{instance.Uuid}_portrait_cover.png")))
            {
                return Path.Combine(dataPath, "ImageCache", $"{instance.Uuid}_portrait_cover.png");
            }
            if (!toPortrait && File.Exists(Path.Combine(dataPath, "ImageCache", $"{instance.Uuid}_cover.png")))
            {
                return Path.Combine(dataPath, "ImageCache", $"{instance.Uuid}_cover.png");
            }

            // Generate new cached cover
            var packImgs = Models.Instance.GetPackImages(instance, installDir, toPortrait, dataPath);
            return packImgs.Item2.Path;
        }

        /// <summary>
        /// Changes names for all instances based on provided token formatted string, shows a progress bar
        /// </summary>
        /// <param name="tokenString">The string containing the name format for instances</param>
        public void ChangeInstanceNames(string tokenString)
        {
            List<Game> instances = new List<Game>();
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.PluginId == Id)
                {
                    instances.Add(game);
                }
            }
            
            logger.Debug($"changing instance names");
            GlobalProgressOptions gpo = new GlobalProgressOptions(ResourceProvider.GetString("LOCATLauncherChangingNames"), false);
            gpo.IsIndeterminate = false;

            // Change covers with progress dialog
            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                activateGlobalProgress.ProgressMaxValue = instances.Count;
                activateGlobalProgress.CurrentProgressValue = -1;
                PlayniteApi.Database.Games.BeginBufferUpdate();
                foreach (var inst in instances)
                {
                    activateGlobalProgress.CurrentProgressValue += 1;
                    inst.Name = ChangeInstanceName(tokenString, inst.InstallDirectory);
                    PlayniteApi.Database.Games.Update(inst);
                    //Thread.Sleep(2000);
                }
                PlayniteApi.Database.Games.EndBufferUpdate();
            }, gpo);
        
        }


        internal static string ChangeInstanceName(string tokenString, string installDir)
        {
            Instance instance = GetInstance(installDir);
            Dictionary<string, string> tokens = new Dictionary<string, string>()
            {
                { "{instancename}", instance.Launcher?.Name ?? string.Empty },
                { "{packname}", instance.Launcher?.Pack ?? string.Empty },
                { "{packversion}", instance.Launcher?.Version ?? string.Empty },
                { "{mcversion}", instance.McVersion ?? string.Empty },
                { "{modloader}", instance.Launcher?.LoaderVersion?.Type ?? ((instance.Launcher.IsVanilla.HasValue && instance.Launcher.IsVanilla.Value) ? "Vanilla" : string.Empty) }
            };
            Regex TokenRegex = new Regex($"({string.Join("|", tokens.Keys.ToArray())})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            string newName = tokenString;
            newName = TokenRegex.Replace(newName, match => tokens[match.Groups[0].Value.ToLowerInvariant()]);
            return newName;
        }
        /// <summary>
        /// Resizes the cover images on demand without blocking UI thread
        /// </summary>
        /// <param name="toPortrait">Determines if cover images will be standard (false) or portait (true)</param>
        //public async void ResizeCoversAsync(bool toPortrait)
        //{
        //    await Task.Run(() => ResizeCovers(toPortrait));
        //}

        /// <summary>
        /// Changes cover images for instances between standard and portrait mode
        /// </summary>
        /// <param name="toPortrait">Determines if cover images will be standard (false) or portait (true)</param>
        //private void ResizeCovers(bool toPortrait)
        //{
        //    PlayniteApi.Database.Games.BeginBufferUpdate();
        //    foreach (var game in PlayniteApi.Database.Games)
        //    {
        //        if (game.PluginId != Id)
        //        {
        //            continue;
        //        }
        //        //logger.Debug($"Changing cover for {game.Name}");
        //        string imgPath = ResizeCover(game, toPortrait);
        //        game.CoverImage = imgPath;
        //        PlayniteApi.Database.Games.Update(game);
        //        //logger.Debug($"Successfully changed cover for {game.Name}");
        //    }
        //    PlayniteApi.Database.Games.EndBufferUpdate();
        //}

    }
}