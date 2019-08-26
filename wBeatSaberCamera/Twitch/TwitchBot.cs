using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.Models;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Twitch
{
    public class TwitchBot : ObservableBase, IDisposable
    {
        private readonly ChatConfigModel _chatConfigModel;
        private readonly TwitchBotConfigModel _configModel;

        [PublicAPI]
        public bool IsConnecting
        {
            get => _isConnecting;
            private set
            {
                if (value == _isConnecting)
                {
                    return;
                }

                _isConnecting = value;
                OnPropertyChanged();
            }
        }

        [PublicAPI]
        public bool IsConnected => _twitchClient?.IsConnected ?? false;

        [PublicAPI]
        public bool IsJoined
        {
            get => _isJoined;
            private set
            {
                if (value == _isJoined)
                {
                    return;
                }

                _isJoined = value;
                OnPropertyChanged();
            }
        }

        private TwitchClient _twitchClient;
        private bool _isConnecting;
        private bool _isJoined;
        private FollowerService _followerService;

        public IEnumerable<string> HostKeys => OnBeingHostedParameters.Union(ChannelParameters).Select(x => $"{{{x}}}");

        public IEnumerable<string> RaidKeys => OnRaidNotificationParameters.Union(ChannelParameters).Select(x => $"{{{x}}}");

        public IEnumerable<string> SubscribeKeys => OnNewSubscriberParameters.Union(ChannelParameters).Select(x => $"{{{x}}}");

        public IEnumerable<string> FollowKeys => FollowParameters.Union(UserParameters).Union(ChannelParameters).Select(x => $"{{{x}}}");

        public TwitchBot(ChatConfigModel chatConfigModel, TwitchBotConfigModel configModel)
        {
            _chatConfigModel = chatConfigModel;
            _configModel = configModel;

            // ReSharper disable UseObjectOrCollectionInitializer
            FollowParameters = new PublicPropertyAccessorCache<Follow>();
            FollowParameters["FollowedAt"] = _ => _.FollowedAt;

            UserParameters = new PublicPropertyAccessorCache<User>();
            UserParameters["User.Id"] = _ => _.Id;
            UserParameters["User.BroadcasterType"] = _ => _.BroadcasterType;
            UserParameters["User.Name"] = _ => _.DisplayName;
            UserParameters["User.Description"] = _ => _.Description;
            UserParameters["User.Login"] = _ => _.Login;
            UserParameters["User.Type"] = _ => _.Type;

            ChannelParameters = new PublicPropertyAccessorCache<Channel>();
            ChannelParameters["Channel"] = _ => _.Name;
            ChannelParameters["Channel.Id"] = _ => _.Id;
            ChannelParameters["Channel.DisplayName"] = _ => _.DisplayName;
            ChannelParameters["Channel.CreatedAt"] = _ => _.CreatedAt;
            ChannelParameters["Channel.Followers"] = _ => _.Followers;
            ChannelParameters["Channel.BroadcasterLanguage"] = _ => _.BroadcasterLanguage;
            ChannelParameters["Channel.BroadcasterType"] = _ => _.BroadcasterType;
            ChannelParameters["Channel.Game"] = _ => _.Game;
            ChannelParameters["Channel.Language"] = _ => _.Language;
            ChannelParameters["Channel.Status"] = _ => _.Status;
            ChannelParameters["Channel.Url"] = _ => _.Url;
            ChannelParameters["Channel.Views"] = _ => _.Views;

            OnNewSubscriberParameters = new PublicPropertyAccessorCache<OnNewSubscriberArgs>();
            OnNewSubscriberParameters["User.Name"] = _ => _.Subscriber.DisplayName;
            OnNewSubscriberParameters["User.Id"] = _ => _.Subscriber.UserId;
            OnNewSubscriberParameters["User.Type"] = _ => _.Subscriber.UserType;
            OnNewSubscriberParameters["User.IsSubscriber"] = _ => _.Subscriber.IsSubscriber;
            OnNewSubscriberParameters["User.IsModerator"] = _ => _.Subscriber.IsModerator;
            OnNewSubscriberParameters["User.IsPartner"] = _ => _.Subscriber.IsPartner;
            OnNewSubscriberParameters["User.IsTurbo"] = _ => _.Subscriber.IsTurbo;
            OnNewSubscriberParameters["User.Login"] = _ => _.Subscriber.Login;
            OnNewSubscriberParameters["User.SubscriptionPlan"] = _ => _.Subscriber.SubscriptionPlan;
            // ReSharper disable once StringLiteralTypo
            OnNewSubscriberParameters["ResubMessage"] = _ => _.Subscriber.ResubMessage;

            OnBeingHostedParameters = new PublicPropertyAccessorCache<OnBeingHostedArgs>();
            OnBeingHostedParameters["HostedByChannel"] = _ => _.BeingHostedNotification.HostedByChannel;
            OnBeingHostedParameters["IsAutoHosted"] = _ => _.BeingHostedNotification.IsAutoHosted;
            OnBeingHostedParameters["BotUsername"] = _ => _.BeingHostedNotification.BotUsername;
            OnBeingHostedParameters["ViewerCount"] = _ => _.BeingHostedNotification.Viewers;

            OnRaidNotificationParameters = new PublicPropertyAccessorCache<OnRaidNotificationArgs>();
            OnRaidNotificationParameters["Raider.Id"] = _ => _.RaidNotificaiton.UserId;
            OnRaidNotificationParameters["Raider.Name"] = _ => _.RaidNotificaiton.DisplayName;
            OnRaidNotificationParameters["Raider.Type"] = _ => _.RaidNotificaiton.UserType;
            OnRaidNotificationParameters["Raider.IsSubscriber"] = _ => _.RaidNotificaiton.Subscriber;
            OnRaidNotificationParameters["Raider.IsModerator"] = _ => _.RaidNotificaiton.Moderator;
            OnRaidNotificationParameters["Raider.IsTurbo"] = _ => _.RaidNotificaiton.Turbo;
            OnRaidNotificationParameters["Raider.Login"] = _ => _.RaidNotificaiton.Login;
            OnRaidNotificationParameters["ViewerCount"] = _ => _.RaidNotificaiton.MsgParamViewerCount;
            // ReSharper restore UseObjectOrCollectionInitializer
        }

        [PublicAPI]
        public PublicPropertyAccessorCache<Channel> ChannelParameters { get; }

        [PublicAPI]
        public PublicPropertyAccessorCache<User> UserParameters { get; }

        [PublicAPI]
        public PublicPropertyAccessorCache<Follow> FollowParameters { get; }

        [PublicAPI]
        public PublicPropertyAccessorCache<OnRaidNotificationArgs> OnRaidNotificationParameters { get; }

        [PublicAPI]
        public PublicPropertyAccessorCache<OnBeingHostedArgs> OnBeingHostedParameters { get; }

        [PublicAPI]
        public PublicPropertyAccessorCache<OnNewSubscriberArgs> OnNewSubscriberParameters { get; }

        private DateTime _followServiceInitializationDate;
        private readonly Dictionary<string, Channel> _channelIdToChannelCache = new Dictionary<string, Channel>();
        private readonly Dictionary<string, Channel> _channelNameToChannelCache = new Dictionary<string, Channel>();
        private TwitchAPI _twitchApi;

        public async void Start()
        {
            if (IsConnecting | IsConnected)
            {
                Log.Warn("TwitchBot is already started");
                return;
            }

            _configModel.PropertyChanged += ConfigModelPropertyChanged;
            _twitchClient = new TwitchClient(logger: new TwitchBotLogger<TwitchClient>());
            _twitchClient.Initialize(new ConnectionCredentials(_configModel.UserName, $"oauth:{_configModel.AccessToken}"), _configModel.Channel);
            _twitchClient.OnMessageReceived += async (s, e) =>
            {
                if (_chatConfigModel.IsTextToSpeechEnabled && !_configModel.CommandIdentifiers.Contains(e.ChatMessage.Message.FirstOrDefault()))
                {
                    _chatConfigModel.Spek(e.ChatMessage);
                }
            };
            _twitchClient.OnWhisperReceived += (s, e) =>
            {
            };
            _twitchClient.OnWhisperCommandReceived += (s, e) =>
            {
            };

            _twitchClient.OnLog += (s, e) =>
            {
                if (e.Data.IndexOf("msg-id=raid;", StringComparison.InvariantCultureIgnoreCase) < 0)
                {
                    return;
                }

                var raidNotification = GetRaidNotificationFromRawMessage(e.Data.Substring(10));

                // muahahaha
                _twitchClient_OnRaidNotification(raidNotification);

                Console.WriteLine($"{e.DateTime} RAW(?): {e.BotUsername} - {e.Data}");
            };

            _twitchApi = new TwitchAPI();
            // ReSharper disable StringLiteralTypo
            _twitchApi.Settings.ClientId = "ijyc8kmvhaoa1wtfz9ys90a37u3wr2";
            // ReSharper restore StringLiteralTypo
            _twitchApi.Settings.AccessToken = _configModel.AccessToken;

            var channel = await GetChannelByName(_configModel.Channel);

            _followerService = new FollowerService(_twitchApi);
            _followerService.OnNewFollowersDetected += FollowerService_OnNewFollowersDetected;
            _followerService.SetChannelsById(new List<string>()
            {
                channel.Id
            });
            _followServiceInitializationDate = DateTime.UtcNow;
            _followerService.Start();

            _configModel.CommandIdentifiers.CollectionChanged += (s, e) =>
            {
                if (e.OldItems != null)
                {
                    foreach (char identifier in e.OldItems)
                    {
                        _twitchClient.RemoveChatCommandIdentifier(identifier);
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (char identifier in e.NewItems)
                    {
                        _twitchClient.AddChatCommandIdentifier(identifier);
                    }
                }
            };

            _twitchClient.OnJoinedChannel += (s, e) => { IsJoined = true; };

            _twitchClient.OnDisconnected += (s, e) => { OnPropertyChanged(nameof(IsConnected)); };
            _twitchClient.OnConnected += (s, e) =>
            {
                foreach (var identifier in _configModel.CommandIdentifiers)
                {
                    _twitchClient.AddChatCommandIdentifier(identifier);
                }

                OnPropertyChanged(nameof(IsConnected));
                //_twitchClient.JoinChannel("snaccyy");
            };

            _twitchClient.OnChatCommandReceived += async (s, e) =>
            {
                foreach (var command in _configModel.Commands)
                {
                    foreach (var commandAlias in command.Commands)
                    {
                        if (commandAlias == e.Command.CommandText.ToLower())
                        {
                            await command.Execute(this, e.Command);
                            return;
                        }
                    }
                }
            };

            _twitchClient.OnBeingHosted += _twitchClient_OnBeingHosted;
            _twitchClient.OnRaidNotification += (s, e) =>
            {
                _configModel.IsRaidNotificationSuddenlyWorking = true;
                //_twitchClient_OnRaidNotification(s, e);
            };
            _twitchClient.OnNewSubscriber += _twitchClient_OnNewSubscriber;

            _twitchClient.Connect();
            IsConnecting = true;

            await SendMessage(_configModel.Channel, "bot started");
        }

        private OnRaidNotificationArgs GetRaidNotificationFromRawMessage(string rawMessage)
        {
            // workaround for broken raid event:
            // ReSharper disable CommentTypo
            /*28.05.2019 00:48:48 RAW(?): benneeeh - Received:
             @badge-info=;
             badges=;
             color=#FF0000;
             display-name=InfiniteThoughts;
             emotes=;
             flags=;
             id=07c86c98-e15f-4944-b720-8f8ac7fef946;
             login=infinitethoughts;
             mod=0;
             msg-id=raid;
             msg-param-displayName=InfiniteThoughts;
             msg-param-login=infinitethoughts;
             msg-param-profileImageURL=https://static-cdn.jtvnw.net/jtv_user_pictures/infinitethoughts-profile_image-3b455f9760ac38d6-70x70.jpeg;
             msg-param-viewerCount=3;
             room-id=41692643;
             subscriber=0;
             system-msg=3\sraiders\sfrom\sInfiniteThoughts\shave\sjoined!;
             tmi-sent-ts=1559004532733;
             user-id=39462410;
             user-type= :tmi.twitch.tv USERNOTICE #benneeeh
            */
            // ReSharper restore CommentTypo

            var splitMessage = rawMessage.Split(';');

            string badges = "";
            string color = "";
            string displayName = "";
            string emotes = "";
            string id = "";
            string login = "";
            bool moderator = false;
            string msgId = "";
            string msgParamDisplayName = "";
            string msgParamLogin = "";
            string msgParamViewerCount = "";
            string roomId = "";
            bool subscriber = false;
            string systemMsg = "";
            string systemMsgParsed = "";
            string tmiSentTs = "";
            bool turbo = false;
            UserType userType = 0;
            //string badgeInfo = "";
            //string flags = "";
            //string msgParamProfileImageUrl = "";
            //string userId = "";

            foreach (var messageParam in splitMessage)
            {
                var splitParam = messageParam.Split('=');
                var leftPart = splitParam[0];
                var rightPart = splitParam[1];

                // oof
                switch (leftPart)
                {
                    //case "@badge-info":
                    //    badgeInfo = rightPart;
                    //    break;
                    //case "flags":
                    //    // what are those?
                    //    flags = rightPart;
                    //    break;
                    //case "msg-param-profileImageURL":
                    //    msgParamProfileImageUrl = rightPart;
                    //    break;
                    //case "user-id":
                    //    userId = rightPart;
                    //    break;
                    case "badges":
                        badges = rightPart;
                        break;

                    case "color":
                        color = rightPart;
                        break;

                    case "display-name":
                        displayName = rightPart;
                        break;

                    case "emotes":
                        emotes = rightPart;
                        break;

                    case "id":
                        // message id?
                        id = rightPart;
                        break;

                    case "login":
                        login = rightPart;
                        break;

                    case "mod":
                        moderator = rightPart != "0";
                        break;

                    case "msg-id":
                        // type, -> raid in this case (WTF TWITCH, NAMING!!!!)
                        msgId = rightPart;
                        break;

                    case "msg-param-displayName":
                        msgParamDisplayName = rightPart;
                        break;

                    case "msg-param-login":
                        msgParamLogin = rightPart;
                        break;

                    case "msg-param-viewerCount":
                        msgParamViewerCount = rightPart;
                        break;

                    case "room-id":
                        roomId = rightPart;
                        break;

                    case "subscriber":
                        subscriber = rightPart != "0";
                        break;

                    case "system-msg":
                        systemMsg = rightPart;
                        break;

                    case "tmi-sent-ts":
                        tmiSentTs = rightPart;
                        break;

                    case "user-type":
                        Enum.TryParse(rightPart.Split(':')[0], true, out userType);
                        break;
                }
            }

            return new OnRaidNotificationArgs()
            {
                Channel = roomId,
                RaidNotificaiton = new RaidNotification(
                    badges,
                    color,
                    displayName,
                    emotes,
                    id,
                    login,
                    moderator,
                    msgId,
                    msgParamDisplayName,
                    msgParamLogin,
                    msgParamViewerCount,
                    roomId,
                    subscriber,
                    systemMsg,
                    systemMsgParsed,
                    tmiSentTs,
                    turbo,
                    userType)
            };
        }

        private async void FollowerService_OnNewFollowersDetected(object sender, OnNewFollowersDetectedArgs e)
        {
            var channel = await GetChannelById(e.Channel);
            foreach (var newFollower in e.NewFollowers)
            {
                if (_followServiceInitializationDate > newFollower.FollowedAt)
                {
                    continue;
                }

                var twitchUsers = await _twitchApi.Helix.Users.GetUsersAsync(new List<string> { newFollower.FromUserId });
                var twitchUser = twitchUsers.Users.FirstOrDefault();
                if (twitchUser == null)
                {
                    Log.Warn($"Found a new follower: '{newFollower.FromUserId}' but twitch API didn't return a user for it");
                    continue;
                }

                await HandleMessageThing(
                    channel,
                    _configModel.IsFollowerAnnouncementsEnabled,
                    _configModel.FollowerAnnouncementTemplate,
                    EnumerableFromMessageThing(newFollower, FollowParameters)
                        .Union(EnumerableFromMessageThing(channel, ChannelParameters))
                        .Union(EnumerableFromMessageThing(twitchUser, UserParameters)));
            }
        }

        private async void _twitchClient_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            var channel = await GetChannelById(e.Channel);
            await HandleMessageThing(channel, _configModel.IsSubscriberAnnouncementsEnabled, _configModel.SubscriberAnnouncementTemplate, e, OnNewSubscriberParameters);
        }

        private async Task<Channel> GetChannelById(string channelId)
        {
            if (!_channelIdToChannelCache.TryGetValue(channelId, out var channel))
            {
                channel = await _twitchApi.V5.Channels.GetChannelByIDAsync(channelId);

                _channelIdToChannelCache[channelId] = channel;
                _channelNameToChannelCache[channel.Name] = channel;
            }

            return channel;
        }

        private async Task<Channel> GetChannelByName(string channelName)
        {
            if (!_channelNameToChannelCache.TryGetValue(channelName, out var channel))
            {
                var users = await _twitchApi.Helix.Users.GetUsersAsync(logins: new List<string>() { _configModel.Channel });
                var user = users.Users.FirstOrDefault();

                if (user == null)
                {
                    throw new InvalidOperationException($"Channel '{channelName}' not found");
                }

                return await GetChannelById(user.Id);
            }

            return channel;
        }

        private async void _twitchClient_OnRaidNotification(OnRaidNotificationArgs e)
        {
            var channel = await GetChannelById(e.Channel);
            await HandleMessageThing(channel, _configModel.IsRaidAnnouncementsEnabled, _configModel.RaidAnnouncementTemplate, e, OnRaidNotificationParameters);
        }

        private async void _twitchClient_OnBeingHosted(object sender, OnBeingHostedArgs e)
        {
            var channel = await GetChannelByName(e.BeingHostedNotification.Channel);
            await HandleMessageThing(channel, _configModel.IsHostAnnouncementsEnabled, _configModel.HostAnnouncementTemplate, e, OnBeingHostedParameters);
        }

        private IEnumerable<(string Key, Func<string> ValueFactory)> EnumerableFromMessageThing<T>(T eventArgs, PublicPropertyAccessorCache<T> propertyAccessorCache)
        {
            foreach (var item in propertyAccessorCache)
            {
                yield return (item, () => propertyAccessorCache[item](eventArgs).ToString());
            }
        }

        private Task HandleMessageThing<T>(Channel channel, bool isEnabled, string template, T eventArgs, PublicPropertyAccessorCache<T> propertyAccessorCache)
        {
            return HandleMessageThing(channel, isEnabled, template, EnumerableFromMessageThing(eventArgs, propertyAccessorCache).Union(EnumerableFromMessageThing(channel, ChannelParameters)));
        }

        private async Task HandleMessageThing(Channel channel, bool isEnabled, string template, IEnumerable<(string Key, Func<string> ValueFactory)> propertyAccessorCache)
        {
            if (!isEnabled)
            {
                return;
            }

            var sb = new StringBuilder(template);
            foreach (var item in propertyAccessorCache)
            {
                if (template.Contains($"{{{item.Key}}}"))
                {
                    sb.Replace($"{{{item.Key}}}", item.ValueFactory());
                }
            }

            await SendMessage(channel.Name, sb.ToString());
        }

        private void ConfigModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_configModel.Channel) && !string.IsNullOrEmpty(_configModel.Channel))
            {
                LeaveAllChannels();
                _twitchClient.JoinChannel(_configModel.Channel);
            }
        }

        [PublicAPI]
        public void LeaveAllChannels()
        {
            foreach (var channel in _twitchClient.JoinedChannels)
            {
                _twitchClient.LeaveChannel(channel);
            }
        }

        public async Task SendMessage(string channel, string message)
        {
            return;
            var rs = new RetryPolicy();
            await rs.ExecuteAsync(() =>
            {
                if (!IsConnected)
                {
                    throw new TransientException($"Cant send message '{message}', TwitchBot is not connected");
                }

                if (!IsJoined)
                {
                    throw new TransientException($"Cant send message '{message}', TwitchBot has not joined channel '{_configModel.Channel}'");
                }

                _twitchClient.SendMessage(channel, message);
                return Task.CompletedTask;
            });
        }

        public void Dispose()
        {
            if (IsConnected)
            {
                Task.WhenAll(_twitchClient.JoinedChannels.Select(channel => Task.Run(() => SendMessage(channel.Channel, $"'{_configModel.UserName}' stopping")))).Wait();
                _twitchClient.Disconnect();
                IsConnecting = false;
                _twitchClient = null;
            }
        }
    }
}