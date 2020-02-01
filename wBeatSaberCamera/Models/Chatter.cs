using Microsoft.Xna.Framework;
using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using wBeatSaberCamera.Service;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Models
{
    public static class RandomProvider
    {
        private static readonly ThreadLocal<Random> s_random = new ThreadLocal<Random>(() => new Random());

        public static Random Random => s_random.Value;
    }

    public class ChatterVoice
    {
        public readonly bool IsValid;

        public string VoiceName => Voice.VoiceInfo.Name;

        public readonly InstalledVoice Voice;

        public ChatterVoice(InstalledVoice voice)
        {
            Voice = voice;
            IsValid = SpeechService.IsVoiceValid(this);
        }
    }

    [DataContract]
    public class Chatter : DirtyBase
    {
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
        public ObservableDictionary<CultureInfo, string> LocalizedVoices
        {
            get => _localizedVoices;
            set
            {
                if (value == _localizedVoices)
                {
                    return;
                }

                UnsubscribeDirtyCollection(_localizedVoices);
                _localizedVoices = value;
                SubscribeDirtyCollection(_localizedVoices);
                OnPropertyChanged();
            }
        }

        private string _name;
        private double _trembleFactor;
        private double _trembleSpeed;
        private double _trembleBegin;
        private double _pitch;
        private Vector3 _position;
        private int _speechRate;
        private int _speechPitch;
        private ObservableDictionary<CultureInfo, string> _localizedVoices;
        private DateTime _lastSpeakTime;

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
            _localizedVoices = new ObservableDictionary<CultureInfo, string>();
            _speechRate = RandomProvider.Random.Next(-40, 40);
            _speechPitch = RandomProvider.Random.Next(-50, 50);
        }

        public string GetVoiceForLanguage(CultureInfo cultureInfo)
        {
            if (!LocalizedVoices.ContainsKey(cultureInfo))
            {
                bool success = false;
                int tries = 10;
                while (!success && tries-- > 0)
                {
                    Application.Current.Dispatcher?.Invoke(() => LocalizedVoices[cultureInfo] = SpeechService.GetRandomVoice(cultureInfo).Name);

                    success = SpeechService.IsVoiceValid(LocalizedVoices[cultureInfo]);
                }

                if (tries == 0)
                {
                    Console.WriteLine($"Couldn't find a proper voice for {Name} and language {cultureInfo}");
                }
            }

            return LocalizedVoices[cultureInfo];
        }
    }
}