using wBeatSaberCamera.DataType;
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
        private ChatViewModel _chatViewModel = new ChatViewModel();
        private Chatter _selectedChatter;

        public ChatViewModel ChatViewModel
        {
            get => _chatViewModel;
            set
            {
                if (Equals(value, _chatViewModel))
                {
                    return;
                }

                _chatViewModel = value;
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
                {
                    return;
                }

                _twitchBot = value;
                OnPropertyChanged();
            }
        }

        public Chatter SelectedChatter
        {
            get => _selectedChatter;
            set
            {
                if (Equals(value, _selectedChatter))
                    return;

                _selectedChatter = value;
                OnPropertyChanged();
            }
        }

        public SpeechToTextModule SpeechToEmojiModule
        {
            get;
            set;
        }

        public BeatSaberCameraSettings AsSettings()
        {
            return new BeatSaberCameraSettings()
            {
                CameraConfigModel = CameraConfigModel,
                TwitchBotConfigModel = TwitchBotConfigModel,
                AppConfigModel = AppConfigModel,
                ChatConfigModel = ChatViewModel.AsConfigModel()
            };
        }
    }
}