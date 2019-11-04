using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Data;
using TwitchLib.Client.Models;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.Service;
using wBeatSaberCamera.Utils;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace wBeatSaberCamera.Models
{
    [DataContract]
    public class ChatConfigModel
    {
        [DataMember]
        public List<Chatter> ChatterList
        {
            get;
            set;
        }

        [DataMember(Name = "Chatters", EmitDefaultValue = false)]
        [UsedImplicitly]
        [Obsolete]
        private Dictionary<string, Chatter> ChatterDict
        {
            get => null;
            set
            {
                if (value != null && value.Count > 0 && (ChatterList?.Count ?? 0) == 0)
                {
                    ChatterList = value.Values.ToList();
                }
            }
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
        public bool IsSpeechEmojiEnabled
        {
            get;
            set;
        }
    }

    public class ChatViewModel : DirtyBase
    {
        #region private fields

        private ObservableCollection<Chatter> _chatters;
        private bool _isTextToSpeechEnabled;
        private double _maxPitchFactor = .3;
        private static readonly Random s_random = new Random();

        private readonly TaskSerializer _taskSerializer = new TaskSerializer();
        private bool _isSendMessagesEnabled;
        private bool _isReadingStreamerMessagesEnabled;
        private bool _isSpeechEmojiEnabled;
        private SpeechService SpeechService => _lazySpeechService.Value;
        private readonly Lazy<SpeechService> _lazySpeechService;
        private Dictionary<string, Chatter> _chatterDictionary;

        #endregion private fields

        #region properties

        public ObservableCollection<Chatter> Chatters
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
                _chatterDictionary = value?.ToDictionary(x => x.Name);
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

        public bool IsSpeechEmojiEnabled
        {
            get => _isSpeechEmojiEnabled;
            set
            {
                if (value == _isSpeechEmojiEnabled)
                    return;

                _isSpeechEmojiEnabled = value;
                OnPropertyChanged();
            }
        }

        #endregion properties

        public ChatViewModel()
        {
            Chatters = new ObservableCollection<Chatter>();

            _lazySpeechService = new Lazy<SpeechService>(() => new SpeechService(this));
        }

        public (Task SpeakStarted, Task SpeakFinished) Speak(string user, string text, string serializerTarget = null)
        {
            var taskStartedCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Func<Task> task = async () =>
            {
                var chatter = GetChatterFromUsername(user);
                taskStartedCompletionSource.SetResult(null);
                await SpeechService.Speak(chatter, text, false);
            };

            if (user != null || serializerTarget != null)
            {
                return (taskStartedCompletionSource.Task, _taskSerializer.Enqueue(serializerTarget ?? user, () => task()));
            }
            else
            {
                return (taskStartedCompletionSource.Task, task());
            }
        }

        private Chatter GetChatterFromUsername(string user)
        {
            if (user == null || !_chatterDictionary.TryGetValue(user, out var chatter))
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
                        _chatterDictionary.Add(user, chatter);
                        Chatters.Add(chatter);
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
            foreach (var chatter in Chatters)
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
                    ChatterList = new List<Chatter>(Chatters),
                    IsSpeechEmojiEnabled = IsSpeechEmojiEnabled,
                    MaxPitchFactor = MaxPitchFactor,
                    IsReadingStreamerMessagesEnabled = IsReadingStreamerMessagesEnabled,
                    IsSendMessagesEnabled = IsSendMessagesEnabled,
                    IsTextToSpeechEnabled = IsTextToSpeechEnabled,
                };
            }
        }

        public void RemoveChatter(string chatterName)
        {
            Chatter chatter = null;
            _chatterDictionary?.TryGetValue(chatterName, out chatter);
            if (chatter != null)
            {
                Chatters.Remove(chatter);
                _chatterDictionary.Remove(chatterName);
            }
        }
    }
}