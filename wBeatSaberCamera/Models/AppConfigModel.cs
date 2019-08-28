using System.Runtime.Serialization;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Models
{
    [DataContract]
    public class AppConfigModel : DirtyBase
    {
        private string _cameraPlusConfig;

        [DataMember]
        public string CameraPlusConfig
        {
            get => _cameraPlusConfig;
            set
            {
                if (value == _cameraPlusConfig)
                    return;

                _cameraPlusConfig = value;
                OnPropertyChanged();
            }
        }
    }
}