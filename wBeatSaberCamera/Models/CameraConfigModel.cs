using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Models
{
    [DataContract]
    public class CameraConfigModel : DirtyBase
    {
        private CameraProfile _currentCameraProfile;
        private ObservableCollection<CameraProfile> _profiles;

        public CameraConfigModel()
        {
            Profiles = new ObservableCollection<CameraProfile>();
        }

        [DataMember]
        public ObservableCollection<CameraProfile> Profiles
        {
            get => _profiles;
            set
            {
                if (Equals(value, _profiles))
                {
                    return;
                }

                UnsubscribeDirtyCollection(_profiles);
                _profiles = value;
                SubscribeDirtyCollection(_profiles);

                OnPropertyChanged();
            }
        }

        public CameraProfile CurrentCameraProfile
        {
            get => _currentCameraProfile ?? (_currentCameraProfile = Profiles.FirstOrDefault());
            set
            {
                if (Equals(value, _currentCameraProfile))
                {
                    return;
                }

                _currentCameraProfile = value;
                OnPropertyChanged();
            }
        }

        public CameraProfile GameCameraProfile
        {
            get;
            set;
        }

        protected override bool ShouldBeDirty(string propertyName) => propertyName != nameof(CurrentCameraProfile);

        public override void Clean()
        {
            foreach (var cp in Profiles)
            {
                cp.Clean();
            }
            base.Clean();
        }
    }
}