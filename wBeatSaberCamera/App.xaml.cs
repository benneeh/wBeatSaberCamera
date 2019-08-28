using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using wBeatSaberCamera.DataType;
using wBeatSaberCamera.Models;

namespace wBeatSaberCamera
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoadConfiguration();
        }

        public void LoadConfiguration()
        {
            var mainViewModel = (MainViewModel)Resources["MainViewModel"];
            var settings = BeatSaberCameraSettings.LoadFromFile();
            if (settings == null)
            {
                settings = new BeatSaberCameraSettings()
                {
                    AppConfigModel = new AppConfigModel(),
                    CameraConfigModel = new CameraConfigModel(),
                    TwitchBotConfigModel = new TwitchBotConfigModel(),
                    ChatConfigModel = new ChatConfigModel()
                };

                settings.AppConfigModel.CameraPlusConfig = SearchCameraPlusConfig();

                settings.CameraConfigModel.Profiles.Add(new CameraProfile()
                {
                    Name = "Default",
                    CameraPlusConfig = new CameraPlusConfig()
                });

                if (File.Exists(settings.AppConfigModel.CameraPlusConfig))
                {
                    settings.CameraConfigModel.CurrentCameraProfile.CameraPlusConfig.LoadDataFromBeatSaber(settings.AppConfigModel);
                }

                settings.AppConfigModel.IsDirty = true;
                settings.CameraConfigModel.IsDirty = true;
                settings.TwitchBotConfigModel.IsDirty = true;
                settings.ChatConfigModel.IsDirty = true;
            }

            mainViewModel.AppConfigModel = settings.AppConfigModel;
            mainViewModel.CameraConfigModel = settings.CameraConfigModel;
            mainViewModel.TwitchBotConfigModel = settings.TwitchBotConfigModel;
            mainViewModel.ChatConfigModel = settings.ChatConfigModel;
        }

        public string SearchCameraPlusConfig()
        {
            string steam32 = "SOFTWARE\\VALVE\\";
            string steam64 = "SOFTWARE\\Wow6432Node\\Valve\\";
            var relevantKey = Registry.LocalMachine.OpenSubKey(steam64) ?? Registry.LocalMachine.OpenSubKey(steam32);
            if (relevantKey == null)
            {
                return null;
            }

            var libraryDirectories = new List<string>();

            foreach (string k32SubKey in relevantKey.GetSubKeyNames())
            {
                using (var subKey = relevantKey.OpenSubKey(k32SubKey))
                {
                    if (subKey == null)
                    {
                        continue;
                    }

                    var steamPath = subKey.GetValue("InstallPath").ToString();
                    var configPath = Path.Combine(steamPath, "steamapps\\libraryfolders.vdf");
                    string driveRegex = "\"([A-Z]:\\\\.*?)\"";
                    if (File.Exists(configPath))
                    {
                        string[] configLines = File.ReadAllLines(configPath);
                        foreach (var item in configLines)
                        {
                            Match match = Regex.Match(item, driveRegex);
                            if (item != string.Empty && match.Success)
                            {
                                string matched = match.Groups[1].ToString();
                                libraryDirectories.Add(matched.Replace("\\\\", "\\"));
                            }
                        }
                        libraryDirectories.Add(steamPath);
                    }
                }
            }

            foreach (var libraryDirectory in libraryDirectories)
            {
                var commonPath = Path.Combine(libraryDirectory, "steamapps\\common");
                var beatSaberPath = Path.Combine(commonPath, "Beat Saber\\");
                if (Directory.Exists(beatSaberPath))
                {
                    return Path.Combine(beatSaberPath, "UserData\\CameraPlus\\cameraplus.cfg");
                }
            }

            return null;
        }
    }
}