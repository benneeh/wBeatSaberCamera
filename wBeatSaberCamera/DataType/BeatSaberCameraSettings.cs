using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.Models;

namespace wBeatSaberCamera.DataType
{
    [DataContract]
    public class BeatSaberCameraSettings
    {
        [DataMember]
        public AppConfigModel AppConfigModel { get; set; }

        [DataMember]
        public TwitchBotConfigModel TwitchBotConfigModel { get; set; }

        [DataMember]
        public CameraConfigModel CameraConfigModel { get; set; }

        [DataMember]
        public ChatConfigModel ChatConfigModel { get; set; }


        private static object s_fileLock = new object();

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
                beatSaberCameraSettings?.Clean();
                return beatSaberCameraSettings;
            }
        }

        public static void Save(BeatSaberCameraSettings beatSaberCameraSettings)
        {
            var fileInfo = new FileInfo($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\wBeatSaberCamera\\config.json");
            if (!fileInfo.Directory?.Exists ?? false)
            {
                fileInfo.Directory.Create();
            }

            lock (s_fileLock)
            {
                using (var fileStream = fileInfo.Open(FileMode.Create))
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.Write(JsonConvert.SerializeObject(beatSaberCameraSettings, Formatting.Indented));
                }
            }

            beatSaberCameraSettings.Clean();
        }

        private void Clean()
        {
            AppConfigModel.Clean();
            CameraConfigModel.Clean();
            TwitchBotConfigModel.Clean();
        }
    }
}