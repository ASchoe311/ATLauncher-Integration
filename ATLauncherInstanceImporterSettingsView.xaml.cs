using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Windows.Shapes;
using Playnite;
using Playnite.SDK;

namespace ATLauncherInstanceImporter
{
    public partial class ATLauncherInstanceImporterSettingsView : UserControl
    {
        private IPlayniteAPI playniteAPI = API.Instance;
        public ATLauncherInstanceImporterSettingsView()
        {
            InitializeComponent();
        }

        private void ChooseATLauncherBtn(Object sender, RoutedEventArgs e)
        {
            var folder = playniteAPI.Dialogs.SelectFolder();
            if (File.Exists(System.IO.Path.Combine(folder, "ATLauncher.exe")))
            {
                ATLauncherPathTxt.Text = folder;
            }
            else if (folder != string.Empty)
            {
                playniteAPI.Dialogs.ShowMessage("ATLauncher.exe not found at selected folder, setting will not be changed.");
            }
        }

    }
}