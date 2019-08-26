using wBeatSaberCamera.Twitch;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Models
{
    public class MainViewModel : ObservableBase
    {
        private AppConfigModel _appConfigModel = new AppConfigModel();
        private CameraConfigModel _cameraConfigModel = new CameraConfigModel();
        private TwitchBotConfigModel _twitchBotConfigModel = new TwitchBotConfigModel();
        private TwitchBot _twitchBot;
        private ChatConfigModel _chatConfigModel = new ChatConfigModel();

        public ChatConfigModel ChatConfigModel
        {
            get => _chatConfigModel;
            set
            {
                if (Equals(value, _chatConfigModel))
                    return;

                _chatConfigModel = value;
                OnPropertyChanged();
            }
        }

        public AppConfigModel AppConfigModel
        {
            get => _appConfigModel;
            set
            {
                if (Equals(value, _appConfigModel))
                {
                    return;
                }

                _appConfigModel = value;
                OnPropertyChanged();
            }
        }

        public CameraConfigModel CameraConfigModel
        {
            get => _cameraConfigModel;
            set
            {
                if (Equals(value, _cameraConfigModel))
                {
                    return;
                }

                _cameraConfigModel = value;
                OnPropertyChanged();
            }
        }

        public TwitchBotConfigModel TwitchBotConfigModel
        {
            get => _twitchBotConfigModel;
            set
            {
                if (Equals(value, _twitchBotConfigModel))
                {
                    return;
                }

                _twitchBotConfigModel = value;
                OnPropertyChanged();
            }
        }

        public TwitchBot TwitchBot
        {
            get => _twitchBot;
            set
            {
                if (Equals(value, _twitchBot))
                    return;

                _twitchBot = value;
                OnPropertyChanged();
            }
        }
    }
}