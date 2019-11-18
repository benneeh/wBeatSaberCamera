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
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.Models;
using wBeatSaberCamera.Utils;
using wBeatSaberCamera.Utils.OAuth;

namespace wBeatSaberCamera.Twitch
{
    public class TwitchBot : ObservableBase, IDisposable
    {
        private readonly ChatViewModel _chatViewModel;
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

        public IEnumerable<string> WelcomeChattersKeys => WelcomeChattersParameters.Union(ChannelParameters).Select(x => $"{{{x}}}");

        public IEnumerable<string> FollowKeys => FollowParameters.Union(UserParameters).Union(ChannelParameters).Select(x => $"{{{x}}}");

        public TwitchBot(ChatViewModel chatViewModel, TwitchBotConfigModel configModel)
        {
            _chatViewModel = chatViewModel;
            _configModel = configModel;

            // ReSharper disable UseObjectOrCollectionInitializer
            FollowParameters = new PublicPropertyAccessorCache<Follow>();
            FollowParameters["FollowedAt"] = _ => _.FollowedAt;

            UserParameters = new PublicPropertyAccessorCache<User>();
            UserParameters["User.Id"] = _ => _.Id;
            UserParameters["User.Name"] = _ => _.DisplayName;
            UserParameters["User.Login"] = _ => _.Login;
            UserParameters["User.BroadcasterType"] = _ => _.BroadcasterType;
            UserParameters["User.Description"] = _ => _.Description;
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
            OnNewSubscriberParameters["User.Id"] = _ => _.Subscriber.UserId;
            OnNewSubscriberParameters["User.Name"] = _ => _.Subscriber.DisplayName;
            OnNewSubscriberParameters["User.Login"] = _ => _.Subscriber.Login;
            OnNewSubscriberParameters["User.Type"] = _ => _.Subscriber.UserType;
            OnNewSubscriberParameters["User.IsSubscriber"] = _ => _.Subscriber.IsSubscriber;
            OnNewSubscriberParameters["User.IsModerator"] = _ => _.Subscriber.IsModerator;
            OnNewSubscriberParameters["User.IsPartner"] = _ => _.Subscriber.IsPartner;
            OnNewSubscriberParameters["User.IsTurbo"] = _ => _.Subscriber.IsTurbo;
            OnNewSubscriberParameters["User.SubscriptionMonths"] = _ => 1;
            OnNewSubscriberParameters["User.SubscriptionPlan"] = _ => _.Subscriber.SubscriptionPlan;
            OnNewSubscriberParameters["ResubMessage"] = _ => _.Subscriber.ResubMessage;

            OnReSubscriberParameters = new PublicPropertyAccessorCache<OnReSubscriberArgs>();
            OnReSubscriberParameters["User.Id"] = _ => _.ReSubscriber.UserId;
            OnReSubscriberParameters["User.Name"] = _ => _.ReSubscriber.DisplayName;
            OnReSubscriberParameters["User.Login"] = _ => _.ReSubscriber.Login;
            OnReSubscriberParameters["User.Type"] = _ => _.ReSubscriber.UserType;
            OnReSubscriberParameters["User.IsSubscriber"] = _ => _.ReSubscriber.IsSubscriber;
            OnReSubscriberParameters["User.IsModerator"] = _ => _.ReSubscriber.IsModerator;
            OnReSubscriberParameters["User.IsPartner"] = _ => _.ReSubscriber.IsPartner;
            OnReSubscriberParameters["User.IsTurbo"] = _ => _.ReSubscriber.IsTurbo;
            OnReSubscriberParameters["User.SubscriptionMonths"] = _ => _.ReSubscriber.Months;
            OnReSubscriberParameters["User.SubscriptionPlan"] = _ => _.ReSubscriber.SubscriptionPlan;
            OnReSubscriberParameters["ResubMessage"] = _ => _.ReSubscriber.ResubMessage;

            OnGiftedSubscriptionParameters = new PublicPropertyAccessorCache<OnGiftedSubscriptionArgs>();
            OnGiftedSubscriptionParameters["Gifter.Login"] = _ => _.GiftedSubscription.Login;
            OnGiftedSubscriptionParameters["Gifter.Name"] = _ => _.GiftedSubscription.DisplayName;
            OnGiftedSubscriptionParameters["Gifter.Id"] = _ => _.GiftedSubscription.UserId;
            OnGiftedSubscriptionParameters["Gifter.Type"] = _ => _.GiftedSubscription.UserType;
            OnGiftedSubscriptionParameters["Gifter.IsSubscriber"] = _ => _.GiftedSubscription.IsSubscriber;
            OnGiftedSubscriptionParameters["Gifter.IsModerator"] = _ => _.GiftedSubscription.IsModerator;
            OnGiftedSubscriptionParameters["Gifter.IsTurbo"] = _ => _.GiftedSubscription.IsTurbo;
            OnGiftedSubscriptionParameters["Gifted.Login"] = _ => _.GiftedSubscription.MsgParamRecipientUserName;
            OnGiftedSubscriptionParameters["Gifted.Name"] = _ => _.GiftedSubscription.MsgParamRecipientDisplayName;
            OnGiftedSubscriptionParameters["Gifted.Id"] = _ => _.GiftedSubscription.MsgParamRecipientId;
            OnGiftedSubscriptionParameters["Gifted.SubscriptionMonths"] = _ => _.GiftedSubscription.MsgParamCumulativeMonths;
            OnGiftedSubscriptionParameters["Gifted.SubscriptionStreakMonths"] = _ => _.GiftedSubscription.MsgParamStreakMonths;
            OnGiftedSubscriptionParameters["Gifted.SubscriptionPlan"] = _ => _.GiftedSubscription.MsgParamSubPlan;

            OnAnonGiftedSubscriptionParameters = new PublicPropertyAccessorCache<OnAnonGiftedSubscriptionArgs>();
            OnAnonGiftedSubscriptionParameters["Gifter.Login"] = _ => _.AnonGiftedSubscription.Login;
            OnAnonGiftedSubscriptionParameters["Gifter.Name"] = _ => _.AnonGiftedSubscription.DisplayName;
            OnAnonGiftedSubscriptionParameters["Gifter.Id"] = _ => _.AnonGiftedSubscription.UserId;
            OnAnonGiftedSubscriptionParameters["Gifter.Type"] = _ => _.AnonGiftedSubscription.UserType;
            OnAnonGiftedSubscriptionParameters["Gifter.IsSubscriber"] = _ => _.AnonGiftedSubscription.IsSubscriber;
            OnAnonGiftedSubscriptionParameters["Gifter.IsModerator"] = _ => _.AnonGiftedSubscription.IsModerator;
            OnAnonGiftedSubscriptionParameters["Gifter.IsTurbo"] = _ => _.AnonGiftedSubscription.IsTurbo;
            OnAnonGiftedSubscriptionParameters["Gifted.Login"] = _ => _.AnonGiftedSubscription.MsgParamRecipientUserName;
            OnAnonGiftedSubscriptionParameters["Gifted.Name"] = _ => _.AnonGiftedSubscription.MsgParamRecipientDisplayName;
            OnAnonGiftedSubscriptionParameters["Gifted.Id"] = _ => _.AnonGiftedSubscription.MsgParamRecipientId;
            OnAnonGiftedSubscriptionParameters["Gifted.SubscriptionMonths"] = _ => _.AnonGiftedSubscription.MsgParamCumulativeMonths;
            OnAnonGiftedSubscriptionParameters["Gifted.SubscriptionStreakMonths"] = _ => _.AnonGiftedSubscription.MsgParamStreakMonths;
            OnAnonGiftedSubscriptionParameters["Gifted.SubscriptionPlan"] = _ => _.AnonGiftedSubscription.MsgParamSubPlan;

            OnBeingHostedParameters = new PublicPropertyAccessorCache<OnBeingHostedArgs>();
            OnBeingHostedParameters["HostedByChannel"] = _ => _.BeingHostedNotification.HostedByChannel;
            OnBeingHostedParameters["IsAutoHosted"] = _ => _.BeingHostedNotification.IsAutoHosted;
            OnBeingHostedParameters["BotUsername"] = _ => _.BeingHostedNotification.BotUsername;
            OnBeingHostedParameters["ViewerCount"] = _ => _.BeingHostedNotification.Viewers;

            OnRaidNotificationParameters = new PublicPropertyAccessorCache<OnRaidNotificationArgs>();
            OnRaidNotificationParameters["Raider.Id"] = _ => _.RaidNotification.UserId;
            OnRaidNotificationParameters["Raider.Name"] = _ => _.RaidNotification.DisplayName;
            OnRaidNotificationParameters["Raider.Login"] = _ => _.RaidNotification.Login;
            OnRaidNotificationParameters["Raider.Type"] = _ => _.RaidNotification.UserType;
            OnRaidNotificationParameters["Raider.IsSubscriber"] = _ => _.RaidNotification.Subscriber;
            OnRaidNotificationParameters["Raider.IsModerator"] = _ => _.RaidNotification.Moderator;
            OnRaidNotificationParameters["Raider.IsTurbo"] = _ => _.RaidNotification.Turbo;
            OnRaidNotificationParameters["ViewerCount"] = _ => _.RaidNotification.MsgParamViewerCount;

            WelcomeChattersParameters = new PublicPropertyAccessorCache<ChatMessage>();
            WelcomeChattersParameters["User.Id"] = _ => _.UserId;
            WelcomeChattersParameters["User.Name"] = _ => _.DisplayName;
            WelcomeChattersParameters["User.Type"] = _ => _.UserType;
            WelcomeChattersParameters["User.IsSubscriber"] = _ => _.IsSubscriber;
            WelcomeChattersParameters["User.IsModerator"] = _ => _.IsModerator;
            WelcomeChattersParameters["User.IsTurbo"] = _ => _.IsTurbo;
            WelcomeChattersParameters["Message"] = _ => _.Message;
            WelcomeChattersParameters["EmoteReplacedMessage"] = _ => _.EmoteReplacedMessage;

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

        [PublicAPI]
        public PublicPropertyAccessorCache<OnReSubscriberArgs> OnReSubscriberParameters { get; }

        [PublicAPI]
        public PublicPropertyAccessorCache<OnGiftedSubscriptionArgs> OnGiftedSubscriptionParameters { get; }

        [PublicAPI]
        public PublicPropertyAccessorCache<OnAnonGiftedSubscriptionArgs> OnAnonGiftedSubscriptionParameters { get; }

        [PublicAPI]
        public PublicPropertyAccessorCache<ChatMessage> WelcomeChattersParameters { get; }

        private DateTime _followServiceInitializationDate;
        private readonly Dictionary<string, Channel> _channelIdToChannelCache = new Dictionary<string, Channel>();
        private readonly Dictionary<string, Channel> _channelNameToChannelCache = new Dictionary<string, Channel>();
        private TwitchAPI _twitchApi;

        /// <summary>
        /// this set contains all chatters that have said something, it is uses to welcome new chatters
        /// </summary>
        private readonly HashSet<string> _knownChatters = new HashSet<string>();

        public async void Start()
        {
            if (IsConnecting | IsConnected)
            {
                Log.Warn("TwitchBot is already started");
                return;
            }

            _configModel.PropertyChanged += (s, e) => RegisterEventHandlerSafe(s, e, ConfigModelPropertyChanged);
            _twitchClient = new TwitchClient(logger: new TwitchBotLogger<TwitchClient>());
            _twitchClient.Initialize(new ConnectionCredentials(_configModel.OAuthAccessToken.UserName, $"oauth:{_configModel.OAuthAccessToken.AccessToken}"), _configModel.Channel);
            _twitchClient.OnWhisperReceived += (s, e) =>
            {
                // TBD
            };
            _twitchClient.OnWhisperCommandReceived += (s, e) =>
            {
                // TBD
            };

            _twitchClient.OnLog += (s, e) =>
            {
                if (e.Data.IndexOf("msg-id=raid;", StringComparison.InvariantCultureIgnoreCase) < 0)
                {
                    return;
                }

                Console.WriteLine($"{e.DateTime} RAW(?): {e.BotUsername} - {e.Data}");
            };

            if (_twitchApi == null)
            {
                _twitchApi = new TwitchAPI();

                // ReSharper disable StringLiteralTypo
                _twitchApi.Settings.ClientId = OAuth.CLIENT_ID;

                // ReSharper restore StringLiteralTypo
                _twitchApi.Settings.AccessToken = _configModel.OAuthAccessToken.AccessToken;
            }

            var channel = await GetChannelByName(_configModel.Channel);

            if (_followerService == null)
            {
                _followerService = new FollowerService(_twitchApi);
                _followerService.OnNewFollowersDetected += (s, e) => RegisterEventHandlerSafe(s, e, FollowerService_OnNewFollowersDetected);
                _followerService.SetChannelsById(new List<string>()
                {
                    channel.Id
                });
                _followServiceInitializationDate = DateTime.UtcNow;
                _followerService.Start();
            }

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
            _twitchClient.OnConnectionError += (s, e) => { OnPropertyChanged(nameof(IsConnected)); };
            _twitchClient.OnDisconnected += (s, e) => { OnPropertyChanged(nameof(IsConnected)); };
            _twitchClient.OnConnected += async (s, e) =>
            {
                foreach (var identifier in _configModel.CommandIdentifiers)
                {
                    _twitchClient.AddChatCommandIdentifier(identifier);
                }

                OnPropertyChanged(nameof(IsConnected));
                IsConnecting = false;

                await SendMessage(_configModel.Channel, "bot started", true);
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

            _twitchClient.OnNewSubscriber += (s, e) => RegisterEventHandlerSafe(s, e, OnNewSubscriber);
            _twitchClient.OnReSubscriber += (s, e) => RegisterEventHandlerSafe(s, e, OnReSubscriber);
            _twitchClient.OnGiftedSubscription += (s, e) => RegisterEventHandlerSafe(s, e, OnGiftedSubscription);
            _twitchClient.OnAnonGiftedSubscription += (s, e) => RegisterEventHandlerSafe(s, e, OnAnonGiftedSubscription);

            _twitchClient.OnBeingHosted += (s, e) => RegisterEventHandlerSafe(s, e, _twitchClient_OnBeingHosted);
            _twitchClient.OnRaidNotification += (s, e) => RegisterEventHandlerSafe(s, e, _twitchClient_OnRaidNotification);
            _twitchClient.OnMessageReceived += (s, e) => RegisterEventHandlerSafe(s, e, _twitchClient_OnMessageReceived);
            _twitchClient.Connect();
            IsConnecting = true;
        }

        private async void _twitchClient_OnMessageReceived(object s, OnMessageReceivedArgs e)
        {
            if (_chatViewModel.IsTextToSpeechEnabled && !_configModel.CommandIdentifiers.Contains(e.ChatMessage.Message.FirstOrDefault()))
            {
#pragma warning disable 4014
                Task.Run(() => _chatViewModel.Speak(e.ChatMessage));
#pragma warning restore 4014
            }

            if (!_knownChatters.Add(e.ChatMessage.Username))
            {
                return;
            }

            var channel = await GetChannelByName(e.ChatMessage.Channel);
            await HandleMessageThing(channel, _configModel.IsWelcomeChattersEnabled, GetRandomLineFromString(_configModel.WelcomeChattersTemplate), e.ChatMessage, WelcomeChattersParameters, e.ChatMessage.Username);
        }

        private async void RegisterEventHandlerSafe<T>(object s, T e, Action<object, T> eventAction)
        {
            try
            {
                eventAction(s, e);
            }
            catch (Exception ex)
            {
                await SendMessage(_configModel.Channel, "Error: " + ex.Message, true);
                Log.Error(ex.ToString());
            }
        }

        [PublicAPI]
        private async void RegisterEventHandlerSafe<T>(object s, T e, Func<object, T, Task> eventAction)
        {
            try
            {
                await eventAction(s, e);
            }
            catch (Exception ex)
            {
                await SendMessage(_configModel.Channel, "Error: " + ex.Message, true);
                Log.Error(ex.ToString());
            }
        }

        private async void OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            var channel = await GetChannelById(e.Channel);
            await HandleMessageThing(channel, _configModel.IsSubscriberAnnouncementsEnabled, GetRandomLineFromString(_configModel.SubscriberAnnouncementTemplate), e, OnNewSubscriberParameters);
        }

        private async void OnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            var channel = await GetChannelById(e.Channel);
            await HandleMessageThing(channel, _configModel.IsSubscriberAnnouncementsEnabled, GetRandomLineFromString(_configModel.SubscriberAnnouncementTemplate), e, OnReSubscriberParameters);
        }

        private async void OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            var channel = await GetChannelById(e.Channel);
            await HandleMessageThing(channel, _configModel.IsSubscriberAnnouncementsEnabled, GetRandomLineFromString(_configModel.SubscriberAnnouncementTemplate), e, OnGiftedSubscriptionParameters);
        }

        private async void OnAnonGiftedSubscription(object sender, OnAnonGiftedSubscriptionArgs e)
        {
            var channel = await GetChannelById(e.Channel);
            await HandleMessageThing(channel, _configModel.IsSubscriberAnnouncementsEnabled, GetRandomLineFromString(_configModel.SubscriberAnnouncementTemplate), e, OnAnonGiftedSubscriptionParameters);
        }

        public async Task Stop()
        {
            if (!IsConnected)
            {
                return;
            }

            await Task.WhenAll(_twitchClient.JoinedChannels.Select(channel => Task.Run(() => SendMessage(channel.Channel, $"'{_configModel.OAuthAccessToken.UserName}' stopping", true))));

            _twitchClient.Disconnect();
            IsConnecting = false;
            _twitchClient = null;
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
                    GetRandomLineFromString(_configModel.FollowerAnnouncementTemplate),
                    EnumerableFromMessageThing(newFollower, FollowParameters)
                        .Union(EnumerableFromMessageThing(channel, ChannelParameters))
                        .Union(EnumerableFromMessageThing(twitchUser, UserParameters)));
            }
        }

        private string GetRandomLineFromString(string multiLineString)
        {
            var lines = multiLineString.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            return lines[RandomProvider.Random.Next(lines.Length)];
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

        private async void _twitchClient_OnRaidNotification(object sender, OnRaidNotificationArgs onRaidNotificationArgs)
        {
            try
            {
                var channel = await GetChannelById(onRaidNotificationArgs.Channel);
                await HandleMessageThing(channel, _configModel.IsRaidAnnouncementsEnabled, GetRandomLineFromString(_configModel.RaidAnnouncementTemplate), onRaidNotificationArgs, OnRaidNotificationParameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("raid channel: " + onRaidNotificationArgs.Channel);
                try
                {
                    var channel = await GetChannelByName(onRaidNotificationArgs.Channel);
                    await HandleMessageThing(channel, _configModel.IsRaidAnnouncementsEnabled, GetRandomLineFromString(_configModel.RaidAnnouncementTemplate), onRaidNotificationArgs, OnRaidNotificationParameters);
                }
                catch
                {
                    // ignore
                }
            }
        }

        private async void _twitchClient_OnBeingHosted(object sender, OnBeingHostedArgs e)
        {
            var channel = await GetChannelByName(e.BeingHostedNotification.Channel);
            await HandleMessageThing(channel, _configModel.IsHostAnnouncementsEnabled, GetRandomLineFromString(_configModel.HostAnnouncementTemplate), e, OnBeingHostedParameters);
        }

        private IEnumerable<(string Key, Func<string> ValueFactory)> EnumerableFromMessageThing<T>(T eventArgs, PublicPropertyAccessorCache<T> propertyAccessorCache)
        {
            foreach (var item in propertyAccessorCache)
            {
                yield return (item, () => propertyAccessorCache[item](eventArgs).ToString());
            }
        }

        private Task HandleMessageThing<T>(Channel channel, bool isEnabled, string template, T eventArgs, PublicPropertyAccessorCache<T> propertyAccessorCache, string serializerTarget = null)
        {
            return HandleMessageThing(channel, isEnabled, template, EnumerableFromMessageThing(eventArgs, propertyAccessorCache).Union(EnumerableFromMessageThing(channel, ChannelParameters)), serializerTarget);
        }

        private async Task HandleMessageThing(Channel channel, bool isEnabled, string template, IEnumerable<(string Key, Func<string> ValueFactory)> propertyAccessorCache, string serializerTarget = null)
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

            await SendMessage(channel.Name, sb.ToString(), true, serializerTarget);
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
            if (_twitchClient == null || !_twitchClient.IsConnected)
            {
                return;
            }

            foreach (var channel in _twitchClient.JoinedChannels)
            {
                _twitchClient.LeaveChannel(channel);
            }
        }

        public async Task SendMessage(string channel, string message, bool speak = false, string serializerTarget = null)
        {
            if (!_chatViewModel.IsSendMessagesEnabled)
            {
                return;
            }

            Task speakStartedTask = Task.CompletedTask;
            if (speak)
            {
                speakStartedTask = _chatViewModel.Speak(null, message, serializerTarget).SpeakStarted;
            }

            await RetryPolicy.ExecuteAsync(async () =>
            {
                if (!IsConnected)
                {
                    throw new TransientException($"Cant send message '{message}', TwitchBot is not connected");
                }

                if (!IsJoined)
                {
                    throw new TransientException($"Cant send message '{message}', TwitchBot has not joined channel '{_configModel.Channel}'");
                }

                await speakStartedTask;
                _twitchClient.SendMessage(channel, message);
            });
        }

        public void Dispose()
        {
            Stop().Wait();
        }
    }
}