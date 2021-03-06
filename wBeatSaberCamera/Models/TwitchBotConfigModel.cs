﻿using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.Twitch;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Models
{
    [DataContract]
    public class TwitchBotConfigModel : DirtyBase
    {
        private OAuthAccessToken _oAuthAccessToken;
        private string _channel = "";

        private ObservableSet<char> _commandIdentifiers = new ObservableSet<char>()
        {
            '!'
        };

        private ObservableCollection<TwitchChatCommand> _commands = new ObservableCollection<TwitchChatCommand>();
        private string _followerAnnouncementTemplate = "'{User.Name}' is now following!";
        private string _raidAnnouncementTemplate = "'{Raider.Name}' has raided this channel with '{ViewerCount}' viewers!";
        private string _hostAnnouncementTemplate = "'{HostedByChannel}' is now hosting your channel with '{ViewerCount}' viewers!";
        private string _subscriberAnnouncementTemplate = "'{User.Name}' has subscribed!";
        private string _welcomeChattersTemplate = "Hi {User.Name}!";
        private string _giftedSubscriptionAnnouncementTemplate = "{Gifter.Name} hast gifted {Gifted.Name} a {Gifted.SubscriptionPlan} subscription! Thank you!";
        private string _bitsReceivedAnnouncementTemplate = "{User.Name}, thank you for the bits!";
        private bool _isFollowerAnnouncementsEnabled;
        private bool _isRaidAnnouncementsEnabled;
        private bool _isHostAnnouncementsEnabled;
        private bool _isSubscriberAnnouncementsEnabled;
        private bool _isWelcomeChattersEnabled;
        private bool _isGiftedSubscriptionAnnouncementsEnabled;
        private bool _isBitsAnnouncementsEnabled;

        public ObservableCollection<TwitchChatCommand> Commands
        {
            get => _commands;
            [UsedImplicitly]
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
        public OAuthAccessToken OAuthAccessToken
        {
            get => _oAuthAccessToken;
            set
            {
                if (value == _oAuthAccessToken)
                {
                    return;
                }

                UnsubscribeDirtyChild(_oAuthAccessToken);
                _oAuthAccessToken = value;
                SubscribeDirtyChild(_oAuthAccessToken);
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
                {
                    return;
                }

                _isSubscriberAnnouncementsEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool IsGiftedSubscriptionAnnouncementsEnabled
        {
            get => _isGiftedSubscriptionAnnouncementsEnabled;
            set
            {
                if (value == _isGiftedSubscriptionAnnouncementsEnabled)
                    return;

                _isGiftedSubscriptionAnnouncementsEnabled = value;
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
        public bool IsWelcomeChattersEnabled
        {
            get => _isWelcomeChattersEnabled;
            set
            {
                if (value == _isWelcomeChattersEnabled)
                    return;

                _isWelcomeChattersEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string WelcomeChattersTemplate
        {
            get => _welcomeChattersTemplate;
            set
            {
                if (value == _welcomeChattersTemplate)
                    return;

                _welcomeChattersTemplate = value;
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
                {
                    return;
                }

                _subscriberAnnouncementTemplate = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string GiftedSubscriptionAnnouncementTemplate
        {
            get => _giftedSubscriptionAnnouncementTemplate;
            set
            {
                if (value == _giftedSubscriptionAnnouncementTemplate)
                    return;

                _giftedSubscriptionAnnouncementTemplate = value;
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

        [DataMember]
        public bool IsBitsAnnouncementsEnabled
        {
            get => _isBitsAnnouncementsEnabled;
            set
            {
                if (value == _isBitsAnnouncementsEnabled)
                    return;

                _isBitsAnnouncementsEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string BitsReceivedAnnouncementTemplate
        {
            get => _bitsReceivedAnnouncementTemplate;
            set
            {
                if (value == _bitsReceivedAnnouncementTemplate)
                    return;

                _bitsReceivedAnnouncementTemplate = value;
                OnPropertyChanged();
            }
        }

        public override void Clean()
        {
            OAuthAccessToken.Clean();
            base.Clean();
        }
    }
}