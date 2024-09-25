using Playnite.SDK;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;

namespace ATLauncherInstanceImporter
{
    public class ATLauncherInstanceImporterClient : LibraryClient
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly ATLauncherInstanceImporter _ATLauncherClient;
        public override string Icon => Path.Combine(ATLauncherInstanceImporter.AssemblyPath, "icon.png");

        public override bool IsInstalled => (!string.IsNullOrEmpty(_ATLauncherClient.Launcher.ExePath)
            && File.Exists(_ATLauncherClient.Launcher.ExePath));
        


        public ATLauncherInstanceImporterClient(ATLauncherInstanceImporter ATLint)
        {
            _ATLauncherClient = ATLint;
        }

        public override void Open()
        {
            logger.Debug("trying to open ATLauncher");
            if (_ATLauncherClient.Launcher == null)
            {
                _ATLauncherClient.DisplayLauncherError();
                return;
            }
            logger.Debug("Got past checks");
            logger.Debug($"Attempting to start {_ATLauncherClient.Launcher.ExePath}");
            Process.Start(_ATLauncherClient.Launcher.ExePath);
        }

        public override void Shutdown()
        {
            if (_ATLauncherClient.Launcher == null)
            {
                _ATLauncherClient.DisplayLauncherError();
                return;
            }
            var procs = Process.GetProcessesByName("javaw");
            foreach (var proc in procs)
            {
                if (proc.MainWindowTitle == "ATLauncher")
                {
                    proc.CloseMainWindow();
                    return;
                }
            }
            logger.Info("ATLauncher not running, no need to shut down");
        }
    }
}