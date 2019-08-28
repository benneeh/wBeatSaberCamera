using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.DataType;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Models
{
    [DataContract]
    public class CameraProfile : DirtyBase
    {
        private ObservableCollection<CameraProfileAlias> _aliases;
        private CameraPlusConfig _cameraPlusConfig;
        private bool _isReadOnly;
        private string _name;
        private bool _isChoosableByViewers = true;

        public CameraProfile()
        {
            Aliases = new ObservableCollection<CameraProfileAlias>();
        }

        [DataMember]
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name)
                {
                    return;
                }

                _name = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (value == _isReadOnly)
                {
                    return;
                }

                _isReadOnly = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool IsChoosableByViewers
        {
            get => _isChoosableByViewers;
            set
            {
                if (value == _isChoosableByViewers)
                {
                    return;
                }

                _isChoosableByViewers = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public CameraPlusConfig CameraPlusConfig
        {
            get => _cameraPlusConfig;
            set
            {
                if (Equals(value, _cameraPlusConfig))
                {
                    return;
                }

                UnsubscribeDirtyChild(_cameraPlusConfig);
                _cameraPlusConfig = value;
                SubscribeDirtyChild(_cameraPlusConfig);
                OnPropertyChanged();
            }
        }

        [DataMember]
        public ObservableCollection<CameraProfileAlias> Aliases
        {
            get => _aliases;
            set
            {
                if (Equals(value, _aliases))
                {
                    return;
                }

                UnsubscribeDirtyCollection(_aliases);
                _aliases = value;
                SubscribeDirtyCollection(_aliases);

                OnPropertyChanged();
            }
        }

        public override void Clean()
        {
            foreach (var cameraProfileAlias in Aliases)
            {
                cameraProfileAlias.Clean();
            }

            CameraPlusConfig?.Clean();
            base.Clean();
        }
    }

    public class CameraProfileAlias : DirtyBase
    {
        private string _alias;

        [UsedImplicitly]
        public CameraProfileAlias()
        {
        }

        public CameraProfileAlias(string alias)
        {
            _alias = alias;
        }

        public string Alias
        {
            get => _alias;
            set
            {
                if (value == _alias)
                {
                    return;
                }

                _alias = value;
                OnPropertyChanged();
            }
        }
    }
}