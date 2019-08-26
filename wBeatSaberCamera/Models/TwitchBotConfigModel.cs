using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using wBeatSaberCamera.Twitch;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Models
{
    [DataContract]
    public class TwitchBotConfigModel : DirtyBase
    {
        private string _userName = "";
        private string _accessToken;
        private string _channel = "";

        private ObservableSet<char> _commandIdentifiers = new ObservableSet<char>()
        {
            '!', '@'
        };

        private ObservableCollection<TwitchChatCommand> _commands = new ObservableCollection<TwitchChatCommand>();
        private string _followerAnnouncementTemplate = "'{User.Name}' is now following!";
        private string _raidAnnouncementTemplate = "'{Raider.Name}' has raided this channel with '{ViewerCount}' viewers!";
        private string _hostAnnouncementTemplate = "'{HostedByChannel}' is now hosting your channel with '{ViewerCount}' viewers!";
        private string _subscriberAnnouncementTemplate = "'{User.Name}' has subscribed!";
        private bool _isFollowerAnnouncementsEnabled;
        private bool _isRaidAnnouncementsEnabled;
        private bool _isHostAnnouncementsEnabled;
        private bool _isSubscriberAnnouncementsEnabled;
        private bool _isRaidNotificationSuddenlyWorking;

        public ObservableCollection<TwitchChatCommand> Commands
        {
            get => _commands;
            set
            {
                if (Equals(value, _commands))
                {
                    return;
                }

                _commands = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string UserName
        {
            get => _userName;
            set
            {
                if (value == _userName)
                {
                    return;
                }

                _userName = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string AccessToken
        {
            get => _accessToken;
            set
            {
                if (value?.StartsWith("oauth:") ?? false)
                {
                    value = value.Substring(6);
                }

                if (value == _accessToken)
                {
                    return;
                }

                _accessToken = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string Channel
        {
            get => _channel;
            set
            {
                if (value == _channel)
                {
                    return;
                }

                _channel = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool IsFollowerAnnouncementsEnabled
        {
            get => _isFollowerAnnouncementsEnabled;
            set
            {
                if (value == _isFollowerAnnouncementsEnabled)
                {
                    return;
                }

                _isFollowerAnnouncementsEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool IsSubscriberAnnouncementsEnabled
        {
            get => _isSubscriberAnnouncementsEnabled;
            set
            {
                if (value == _isSubscriberAnnouncementsEnabled)
                    return;

                _isSubscriberAnnouncementsEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool IsHostAnnouncementsEnabled
        {
            get => _isHostAnnouncementsEnabled;
            set
            {
                if (value == _isHostAnnouncementsEnabled)
                {
                    return;
                }

                _isHostAnnouncementsEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool IsRaidAnnouncementsEnabled
        {
            get => _isRaidAnnouncementsEnabled;
            set
            {
                if (value == _isRaidAnnouncementsEnabled)
                {
                    return;
                }

                _isRaidAnnouncementsEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string HostAnnouncementTemplate
        {
            get => _hostAnnouncementTemplate;
            set
            {
                if (value == _hostAnnouncementTemplate)
                {
                    return;
                }

                _hostAnnouncementTemplate = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string RaidAnnouncementTemplate
        {
            get => _raidAnnouncementTemplate;
            set
            {
                if (value == _raidAnnouncementTemplate)
                {
                    return;
                }

                _raidAnnouncementTemplate = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string FollowerAnnouncementTemplate
        {
            get => _followerAnnouncementTemplate;
            set
            {
                if (value == _followerAnnouncementTemplate)
                {
                    return;
                }

                _followerAnnouncementTemplate = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string SubscriberAnnouncementTemplate
        {
            get => _subscriberAnnouncementTemplate;
            set
            {
                if (value == _subscriberAnnouncementTemplate)
                    return;

                _subscriberAnnouncementTemplate = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public ObservableSet<char> CommandIdentifiers
        {
            get => _commandIdentifiers;
            set
            {
                if (Equals(value, _commandIdentifiers))
                {
                    return;
                }

                _commandIdentifiers = value;
                OnPropertyChanged();
            }
        }

        public bool IsRaidNotificationSuddenlyWorking
        {
            get => _isRaidNotificationSuddenlyWorking;
            set
            {
                if (value == _isRaidNotificationSuddenlyWorking)
                    return;

                _isRaidNotificationSuddenlyWorking = value;
                OnPropertyChanged();
            }
        }
    }
}