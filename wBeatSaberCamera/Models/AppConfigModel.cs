using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
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
