using Microsoft.Xna.Framework;
using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;
using wBeatSaberCamera.Service;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Models
{
    public static class RandomProvider
    {
        private static readonly ThreadLocal<Random> s_random = new ThreadLocal<Random>(() => new Random());

        public static Random Random => s_random.Value;
    }

    [DataContract]
    public class ChatterVoice
    {
        private string _voiceName;

        public readonly bool IsValid;

        [DataMember]
        public string VoiceName
        {
            get => _voiceName ?? (_voiceName = Voice?.VoiceInfo.Name);
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Voice = SpeechService.GetVoiceByName(value);
                }
                else
                {
                    Voice = null;
                }
                _voiceName = value;
            }
        }

        public InstalledVoice Voice
        {
            get;
            private set;
        }

        [JsonConstructor]
        public ChatterVoice(string voiceName)
        {
            Voice = SpeechService.GetVoiceByName(voiceName);
            IsValid = SpeechService.IsVoiceValid(this);
        }

        public ChatterVoice(InstalledVoice voice)
        {
            Voice = voice;
            IsValid = SpeechService.IsVoiceValid(this);
        }
    }

    [DataContract]
    public class Chatter : DirtyBase
    {
        private string _name;
        private double _trembleFactor;
        private double _trembleSpeed;
        private double _trembleBegin;
        private double _pitch;
        private Vector3 _position;
        private int _speechRate;
        private int _speechPitch;
        private ObservableDictionary<CultureInfo, ChatterVoice> _localizedChatterVoices;
        private DateTime _lastSpeakTime;
        private bool _isWeirdo;

        [DataMember]
        public bool IsWeirdo
        {
            get => _isWeirdo;
            set
            {
                if (value == _isWeirdo)
                    return;

                _isWeirdo = value;
                OnPropertyChanged();
            }
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
        public Vector3 Position
        {
            get => _position;
            set
            {
                if (value.Equals(_position))
                {
                    return;
                }

                _position = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public double Pitch
        {
            get => _pitch;
            set
            {
                if (value.Equals(_pitch))
                {
                    return;
                }

                _pitch = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public int SpeechPitch
        {
            get => _speechPitch;
            set
            {
                if (value.Equals(_speechPitch))
                    return;

                _speechPitch = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public int SpeechRate
        {
            get => _speechRate;
            set
            {
                if (value.Equals(_speechRate))
                    return;

                _speechRate = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public double TrembleBegin
        {
            get => _trembleBegin;
            set
            {
                if (value.Equals(_trembleBegin))
                {
                    return;
                }

                _trembleBegin = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public double TrembleSpeed
        {
            get => _trembleSpeed;
            set
            {
                if (value.Equals(_trembleSpeed))
                {
                    return;
                }

                _trembleSpeed = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public double TrembleFactor
        {
            get => _trembleFactor;
            set
            {
                if (value.Equals(_trembleFactor))
                {
                    return;
                }

                _trembleFactor = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public ObservableDictionary<CultureInfo, ChatterVoice> LocalizedChatterVoices
        {
            get => _localizedChatterVoices;
            set
            {
                if (value == _localizedChatterVoices)
                {
                    return;
                }

                UnsubscribeDirtyCollection(_localizedChatterVoices);
                _localizedChatterVoices = value;
                SubscribeDirtyCollection(_localizedChatterVoices);
                OnPropertyChanged();
            }
        }

        [DataMember]
        public DateTime LastSpeakTime
        {
            get => _lastSpeakTime;
            set
            {
                if (value.Equals(_lastSpeakTime))
                    return;

                _lastSpeakTime = value;
                OnPropertyChanged();
            }
        }

        public Chatter()
        {
            _localizedChatterVoices = new ObservableDictionary<CultureInfo, ChatterVoice>();
            _speechRate = RandomProvider.Random.Next(-80, 100);
            _speechPitch = RandomProvider.Random.Next(-50, 50);
            _isWeirdo = RandomProvider.Random.Next(100) == 0;
        }

        public ChatterVoice GetVoiceForLanguage(CultureInfo cultureInfo)
        {
            if (!LocalizedChatterVoices.ContainsKey(cultureInfo) || !LocalizedChatterVoices[cultureInfo].IsValid)
            {
                bool success = false;
                int tries = 10;
                while (!success && tries-- > 0)
                {
                    Application.Current.Dispatcher?.Invoke(() => LocalizedChatterVoices[cultureInfo] = new ChatterVoice(SpeechService.GetRandomVoice(cultureInfo).Name));

                    success = SpeechService.IsVoiceValid(LocalizedChatterVoices[cultureInfo]);
                }

                if (tries == 0)
                {
                    Console.WriteLine($"Couldn't find a proper voice for {Name} and language {cultureInfo}");
                }
            }

            return LocalizedChatterVoices[cultureInfo];
        }
    }
}