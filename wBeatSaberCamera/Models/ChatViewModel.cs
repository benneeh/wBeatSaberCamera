using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using TwitchLib.Client.Models;
using wBeatSaberCamera.Service;
using wBeatSaberCamera.Utils;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace wBeatSaberCamera.Models
{
    [DataContract]
    public class ChatConfigModel
    {
        [DataMember]
        public ObservableDictionary<string, Chatter> Chatters
        {
            get;
            set;
        }

        [DataMember]
        public bool IsTextToSpeechEnabled
        {
            get;
            set;
        }

        [DataMember]
        public double MaxPitchFactor
        {
            get;
            set;
        }

        [DataMember]
        public bool IsSendMessagesEnabled
        {
            get;
            set;
        }

        [DataMember]
        public bool IsReadingStreamerMessagesEnabled
        {
            get;
            set;
        }

        [DataMember]
        public bool IsSpeechToTextEnabled
        {
            get;
            set;
        }
    }

    public class ChatViewModel : DirtyBase
    {
        #region private fields

        private ObservableDictionary<string, Chatter> _chatters;
        private bool _isTextToSpeechEnabled;
        private double _maxPitchFactor = .3;
        private static readonly Random s_random = new Random();

        private readonly TaskSerializer _taskSerializer = new TaskSerializer();
        private bool _isSendMessagesEnabled;
        private bool _isReadingStreamerMessagesEnabled;
        private bool _isSpeechToTextEnabled;
        private SpeechService SpeechService => _lazySpeechService.Value;
        private readonly Lazy<SpeechService> _lazySpeechService;

        #endregion private fields

        #region properties

        public ObservableDictionary<string, Chatter> Chatters
        {
            get => _chatters;
            set
            {
                if (Equals(value, _chatters))
                {
                    return;
                }

                UnsubscribeDirtyCollection(_chatters);
                BindingOperations.EnableCollectionSynchronization(value, new object());
                _chatters = value;
                SubscribeDirtyCollection(_chatters);

                OnPropertyChanged();
            }
        }

        public bool IsTextToSpeechEnabled
        {
            get => _isTextToSpeechEnabled;
            set
            {
                if (value == _isTextToSpeechEnabled)
                {
                    return;
                }

                _isTextToSpeechEnabled = value;
                OnPropertyChanged();
            }
        }

        public double MaxPitchFactor
        {
            get => _maxPitchFactor;
            set
            {
                if (value.Equals(_maxPitchFactor))
                {
                    return;
                }

                _maxPitchFactor = value;
                OnPropertyChanged();
            }
        }

        public bool IsSendMessagesEnabled
        {
            get => _isSendMessagesEnabled;
            set
            {
                if (value == _isSendMessagesEnabled)
                    return;

                _isSendMessagesEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsReadingStreamerMessagesEnabled
        {
            get => _isReadingStreamerMessagesEnabled;
            set
            {
                if (value == _isReadingStreamerMessagesEnabled)
                    return;

                _isReadingStreamerMessagesEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsSpeechToTextEnabled
        {
            get => _isSpeechToTextEnabled;
            set
            {
                if (value == _isSpeechToTextEnabled)
                    return;

                _isSpeechToTextEnabled = value;
                OnPropertyChanged();
            }
        }

        #endregion properties

        public ChatViewModel()
        {
            Chatters = new ObservableDictionary<string, Chatter>();

            _lazySpeechService = new Lazy<SpeechService>(() => new SpeechService(this));
        }

        public void Speak(string user, string text)
        {
            Func<Task> task = async () =>
            {
                var chatter = GetChatterFromUsername(user);

                await SpeechService.Speak(chatter, text, false);
            };

            if (user != null)
            {
                _taskSerializer.Enqueue(user, () => task());
            }
            else
            {
                task();
            }
        }

        private Chatter GetChatterFromUsername(string user)
        {
            if (user == null || !Chatters.TryGetValue(user, out var chatter))
            {
                chatter = new Chatter()
                {
                    Name = user
                };

                chatter.Position = /*Vector3.Right * 2;*/ (Vector3.Lerp(Vector3.Left, Vector3.Right, (float)s_random.NextDouble())
                                                           + Vector3.Lerp(Vector3.Up, Vector3.Down, (float)s_random.NextDouble())
                                                           + Vector3.Lerp(Vector3.Forward, Vector3.Backward, (float)s_random.NextDouble()));
                chatter.Pitch = (s_random.NextDouble() * 2 - 1.0) * MaxPitchFactor;
                chatter.TrembleBegin = s_random.NextDouble() * Math.PI * 2;
                chatter.TrembleSpeed = s_random.NextDouble();
                var factorMultMax = .3;
                if (chatter.TrembleSpeed < .02)
                {
                    factorMultMax = 2;
                }

                chatter.TrembleFactor = s_random.NextDouble() * factorMultMax;
                if (user != null)
                {
                    lock (Chatters)
                    {
                        Chatters[user] = chatter;
                    }
                }

                //Console.WriteLine($"TS: {chatter.TrembleSpeed}, TF: {chatter.TrembleFactor}");
            }

            return chatter;
        }

        public async Task Speak(string user, byte[] speechResultAudioData)
        {
            var chatter = GetChatterFromUsername(user);

            await SpeechService.Speak(chatter, speechResultAudioData);
        }

        public void Speak(ChatMessage chatMessage)
        {
            if (!IsReadingStreamerMessagesEnabled && chatMessage.IsBroadcaster)
            {
                return;
            }
            Speak(chatMessage.Username, chatMessage.Message);
        }

        public override void Clean()
        {
            foreach (var chatter in Chatters.Values)
            {
                chatter.Clean();
            }
            base.Clean();
        }

        public ChatConfigModel AsConfigModel()
        {
            lock (Chatters)
            {
                return new ChatConfigModel()
                {
                    Chatters = new ObservableDictionary<string, Chatter>(Chatters),
                    IsSpeechToTextEnabled = IsSpeechToTextEnabled,
                    MaxPitchFactor = MaxPitchFactor,
                    IsReadingStreamerMessagesEnabled = IsReadingStreamerMessagesEnabled,
                    IsSendMessagesEnabled = IsSendMessagesEnabled,
                    IsTextToSpeechEnabled = IsTextToSpeechEnabled,
                };
            }
        }
    }
}