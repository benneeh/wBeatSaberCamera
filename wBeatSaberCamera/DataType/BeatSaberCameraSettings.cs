using System;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.Models;

namespace wBeatSaberCamera.DataType
{
    [DataContract]
    public class BeatSaberCameraSettings
    {
        private ChatConfigModel _chatConfigModel;

        [DataMember]
        public AppConfigModel AppConfigModel { get; set; }

        [DataMember]
        public TwitchBotConfigModel TwitchBotConfigModel { get; set; }

        [DataMember]
        public CameraConfigModel CameraConfigModel { get; set; }

        [DataMember]
        public ChatConfigModel ChatConfigModel
        {
            get => _chatConfigModel ?? (_chatConfigModel = new ChatConfigModel());
            set => _chatConfigModel = value;
        }

        [CanBeNull]
        public static BeatSaberCameraSettings LoadFromFile()
        {
            var fileInfo = new FileInfo($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\wBeatSaberCamera\\config.json");
            if (!fileInfo.Exists)
            {
                return null;
            }
            using (var fileStream = fileInfo.OpenRead())
            using (var streamReader = new StreamReader(fileStream))
            {
                var beatSaberCameraSettings = JsonConvert.DeserializeObject<BeatSaberCameraSettings>(streamReader.ReadToEnd());
                beatSaberCameraSettings.Clean();
                return beatSaberCameraSettings;
            }
        }

        public static void Save(AppConfigModel appConfigModel, CameraConfigModel cameraConfigModel, TwitchBotConfigModel twitchBotConfigModel, ChatConfigModel chatConfigModel)
        {
            var settingsInstance = new BeatSaberCameraSettings()
            {
                AppConfigModel = appConfigModel,
                CameraConfigModel = cameraConfigModel,
                TwitchBotConfigModel = twitchBotConfigModel,
                ChatConfigModel = chatConfigModel
            };
            var fileInfo = new FileInfo($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\wBeatSaberCamera\\config.json");
            if (!fileInfo.Directory?.Exists ?? false)
            {
                fileInfo.Directory.Create();
            }
            using (var fileStream = fileInfo.Open(FileMode.Create))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                streamWriter.Write(JsonConvert.SerializeObject(settingsInstance, Formatting.Indented));
            }

            settingsInstance.Clean();
        }

        private void Clean()
        {
            AppConfigModel.Clean();
            CameraConfigModel.Clean();
            TwitchBotConfigModel.Clean();
            ChatConfigModel.Clean();
        }
    }
}
