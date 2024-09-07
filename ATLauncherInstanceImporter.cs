using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ATLauncherInstanceImporter
{
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

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            // Return list of user's games.
            return new List<GameMetadata>()
            {
                new GameMetadata()
                {
                    Name = "Notepad",
                    GameId = "notepad",
                    GameActions = new List<GameAction>
                    {
                        new GameAction()
                        {
                            Type = GameActionType.File,
                            Path = "notepad.exe",
                            IsPlayAction = true
                        }
                    },
                    IsInstalled = true,
                    Icon = new MetadataFile(@"c:\Windows\notepad.exe")
                },
                new GameMetadata()
                {
                    Name = "Calculator",
                    GameId = "calc",
                    GameActions = new List<GameAction>
                    {
                        new GameAction()
                        {
                            Type = GameActionType.File,
                            Path = "calc.exe",
                            IsPlayAction = true
                        }
                    },
                    IsInstalled = true,
                    Icon = new MetadataFile(@"https://playnite.link/applogo.png"),
                    BackgroundImage = new MetadataFile(@"https://playnite.link/applogo.png")
                }
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new ATLauncherInstanceImporterSettingsView();
        }
    }
}