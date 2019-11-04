using Microsoft.Xna.Framework;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Models
{
    public static class RandomProvider
    {
        private static readonly ThreadLocal<Random> s_random = new ThreadLocal<Random>(() => new Random());

        public static Random Random => s_random.Value;
    }

    [DataContract]
    public class Chatter : DirtyBase
    {
        private static readonly SpeechSynthesizer s_speechSynthesizer = new SpeechSynthesizer();

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
        public ObservableDictionary<CultureInfo, string> VoiceName
        {
            get => _voiceName;
            set
            {
                if (value == _voiceName)
                {
                    return;
                }

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
        private int _speechRate;
        private int _speechPitch;
        private DateTime _lastSpeakTime;
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
            _voiceName = new ObservableDictionary<CultureInfo, string>();
            _speechRate = RandomProvider.Random.Next(-40, 20);
            //if (_speechRate < 0)
            //{
            _speechPitch = RandomProvider.Random.Next(-50, 50);
            //}
        }

        private static readonly Regex s_urlRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)", RegexOptions.Compiled);
        private static readonly Regex s_ohReplacementRegex = new Regex("\\b(([a-zA-Z]{2,2})|([a-zA-Z)]+[aAeEiIoOuUöyYÖäÄüÜ]{1,}[a-zA-Z]+))\\b", RegexOptions.Compiled);

        public string GetSsmlFromText(CultureInfo cultureInfo, string text)
        {
            var voiceForLanguage = GetVoiceForLanguage(cultureInfo);

            text = s_urlRegex.Replace(text, "URL");

            var ssml = $@"
<speak version=""1.0"" xmlns=""https://www.w3.org/2001/10/synthesis"" xml:lang=""en-US"">
    <voice name=""{voiceForLanguage}"">
        <prosody pitch=""{SpeechPitch:+#;-#;0}%"" rate=""{SpeechRate}%"">
            {s_ohReplacementRegex.Replace(text, (match) => $"<prosody pitch=\"{RandomProvider.Random.Next(-50, 50):+#;-#;0}%\" rate=\"{RandomProvider.Random.Next(50)}%\">{match.Value}</prosody>")}
        </prosody>
    </voice>
</speak>";

            //var ohTemplate = "<prosody pitch=\"+50%\" rate=\"1%\">{0}</prosody>"; // <prosody rate="10%" contour="(0%,+20Hz) (50%,+420Hz) (100%, +10Hz)">oh</prosody>

            return ssml;
        }

        private string GetVoiceForLanguage(CultureInfo cultureInfo)
        {
            if (!VoiceName.ContainsKey(cultureInfo))
            {
                bool success = false;
                int tries = 10;
                while (!success && tries-- > 0)
                {
                    App.Current.Dispatcher.Invoke(() => VoiceName[cultureInfo] = GetRandomVoice(cultureInfo).Name);

                    try
                    {
                        lock (s_speechSynthesizer)
                        {
                            s_speechSynthesizer.SelectVoice(VoiceName[cultureInfo]);
                        }

                        success = true;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                if (tries == 0)
                {
                    Console.WriteLine($"Couldn't find a proper voice for {Name} and language {cultureInfo}");
                }
                lock (s_speechSynthesizer)
                {
                    return s_speechSynthesizer.Voice.Name;
                }
            }

            return VoiceName[cultureInfo];
        }

        public void WriteSpeechToStream(CultureInfo language, string text, MemoryStream ms)
        {
            lock (s_speechSynthesizer)
            {
                s_speechSynthesizer.SetOutputToWaveStream(ms);

                var ssml = GetSsmlFromText(language, text);

                s_speechSynthesizer.SpeakSsml(ssml);
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