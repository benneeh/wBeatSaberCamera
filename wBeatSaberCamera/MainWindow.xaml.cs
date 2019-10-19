using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Windows;
using System.Windows.Input;
using wBeatSaberCamera.DataType;
using wBeatSaberCamera.Models;

namespace wBeatSaberCamera
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window
    {
        private MainViewModel MainViewModel => Application.Current.Resources["MainViewModel"] as MainViewModel;
        private AppConfigModel AppConfigModel => MainViewModel.AppConfigModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = false;
            dialog.Multiselect = false;
            var currentConfigFileInfo = new FileInfo(AppConfigModel.CameraPlusConfig);
            dialog.InitialDirectory = currentConfigFileInfo.Directory?.FullName;
            dialog.DefaultFileName = currentConfigFileInfo.Name;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                AppConfigModel.CameraPlusConfig = dialog.FileName;
            }
        }

        private void SaveCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            BeatSaberCameraSettings.Save(MainViewModel.AsSettings());
            MainViewModel.ChatViewModel.Clean();
        }

        private void LoadConfigurationCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ((App)Application.Current).LoadConfiguration();
        }
    }
}