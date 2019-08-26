using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using wBeatSaberCamera.DataType;
using wBeatSaberCamera.Models;

namespace wBeatSaberCamera.Views
{
    /// <summary>
    /// Interaction logic for CameraSettings.xaml
    /// </summary>
    public partial class CameraSettings : UserControl
    {
        private MainViewModel MainViewModel => Application.Current.Resources["MainViewModel"] as MainViewModel;
        private CameraConfigModel CameraConfigModel => MainViewModel.CameraConfigModel;
        private AppConfigModel AppConfigModel => MainViewModel.AppConfigModel;

        public CameraSettings()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CameraConfigModel.CurrentCameraProfile.CameraPlusConfig.LoadDataFromBeatSaber(AppConfigModel);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            CameraConfigModel.CurrentCameraProfile.CameraPlusConfig.SaveToBeatSaber(AppConfigModel);
        }

        private void CreateNewProfile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CameraConfigModel.Profiles.Add(new CameraProfile()
            {
                Name = $"Copy of '{CameraConfigModel.CurrentCameraProfile.Name}'",
                CameraPlusConfig = JsonConvert.DeserializeObject<CameraPlusConfig>(JsonConvert.SerializeObject(CameraConfigModel.CurrentCameraProfile.CameraPlusConfig))
            });
            CameraConfigModel.CurrentCameraProfile = CameraConfigModel.Profiles[CameraConfigModel.Profiles.Count - 1];

            ProfileNameTextBox.SelectAll();
            ProfileNameTextBox.Focus();
        }

        private void DeleteProfile_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                e.CanExecute = CameraConfigModel.Profiles.Count > 1;
            }
            catch
            {
                // ignored
            }
        }

        private void DeleteProfile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CameraConfigModel.Profiles.Remove(CameraConfigModel.CurrentCameraProfile);
            CameraConfigModel.CurrentCameraProfile = CameraConfigModel.Profiles[CameraConfigModel.Profiles.Count - 1];
        }

        private void AddAliasCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var cameraProfile = e.Parameter as CameraProfile;

            cameraProfile?.Aliases.Add(new CameraProfileAlias("new alias"));
        }

        private void RemoveAliasCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var array = (object[])e.Parameter;
            var cameraProfile = array[0] as CameraProfile;
            var alias = array[1] as CameraProfileAlias;
            cameraProfile?.Aliases.Remove(alias);
        }
    }
}