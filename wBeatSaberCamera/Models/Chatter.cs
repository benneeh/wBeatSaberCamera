using Microsoft.Xna.Framework;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Speech.Synthesis;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Models
{
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
                    return;

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
                    return;

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
                    return;

                _pitch = value;
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
                    return;

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
                    return;

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
                    return;

                _trembleFactor = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public ObservableDictionary<CultureInfo, string> VoiceName
        {
            get => _voiceName;
            set
            {
                if (value == _voiceName)
                    return;

                UnsubscribeDirtyCollection(_voiceName);
                _voiceName = value;
                SubscribeDirtyCollection(_voiceName);
                OnPropertyChanged();
            }
        }

        private string _name;
        private ObservableDictionary<CultureInfo, string> _voiceName;
        private double _trembleFactor;
        private double _trembleSpeed;
        private double _trembleBegin;
        private double _pitch;
        private Vector3 _position;
        private readonly Lazy<SpeechSynthesizer> _lazySynthesizer;
        private string _selectedVoice;
        private static ReadOnlyCollection<InstalledVoice> _voices;
        private static readonly Random s_random = new Random();

        private static ReadOnlyCollection<InstalledVoice> Voices
        {
            get
            {
                if (_voices == null)
                {
                    using (var synthesizer = new SpeechSynthesizer())
                    {
                        _voices = synthesizer.GetInstalledVoices();
                    }
                }

                return _voices;
            }
        }

        public Chatter()
        {
            _voiceName = new ObservableDictionary<CultureInfo, string>();
            _lazySynthesizer = new Lazy<SpeechSynthesizer>(() => new SpeechSynthesizer());
        }

        public void WriteSpeechToStream(CultureInfo language, string text, MemoryStream ms)
        {
            if (!VoiceName.ContainsKey(language))
            {
                bool success = false;
                int tries = 10;
                while (!success && tries-- > 0)
                {
                    VoiceName[language] = GetRandomVoice(language).Name;
                    success = SelectVoice(VoiceName[language]);
                }

                if (tries == 0)
                {
                    Console.WriteLine($"Couldn't find a proper voice for {Name} and language {language}");
                    return;
                }
            }

            SelectVoice(VoiceName[language]);
            lock (_lazySynthesizer)
            {
                _lazySynthesizer.Value.SetOutputToWaveStream(ms);
                _lazySynthesizer.Value.Speak(text);
            }
        }

        private bool SelectVoice(string voiceName)
        {
            if (_selectedVoice == voiceName)
            {
                return true;
            }

            try
            {
                lock (_lazySynthesizer)
                {
                    _lazySynthesizer.Value.SelectVoice(voiceName);
                }

                _selectedVoice = voiceName;
                return true;
            }
            catch
            {
                return false;
            }
        }

        #region get voice

        [PublicAPI]
        private VoiceInfo GetRandomVoice([CanBeNull] CultureInfo language)
        {
            ReadOnlyCollection<InstalledVoice> voices = Voices;
            if (language != null)
            {
                voices = new ReadOnlyCollection<InstalledVoice>(voices.Where(x => x.VoiceInfo.Culture.Equals(language) || x.VoiceInfo.Culture.Parent.Equals(language)).ToList());
            }

            if (voices.Count == 0)
            {
                voices = Voices;
            }

            var selectedVoice = voices[s_random.Next(voices.Count)].VoiceInfo;

            return selectedVoice;
        }

        [PublicAPI]
        private VoiceInfo GetVoiceByName(string name)
        {
            return Voices.FirstOrDefault(x => x.VoiceInfo.Name == name)?.VoiceInfo ?? new SpeechSynthesizer().Voice;
        }

        [PublicAPI]
        private VoiceInfo GetVoiceById(string id)
        {
            return Voices.FirstOrDefault(x => x.VoiceInfo.Id == id)?.VoiceInfo ?? new SpeechSynthesizer().Voice;
        }

        #endregion get voice
    }
}