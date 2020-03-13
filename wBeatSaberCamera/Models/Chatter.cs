using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Windows;
using Ionic.Zlib;
using Newtonsoft.Json;
using ProtoBuf;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.Service;
using wBeatSaberCamera.Utils;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using GZipStream = System.IO.Compression.GZipStream;

namespace wBeatSaberCamera.Models
{
    public static class RandomProvider
    {
        private static readonly ThreadLocal<Random> s_random = new ThreadLocal<Random>(() => new Random());

        public static Random Random => s_random.Value;
    }

    [DataContract]
    [ProtoContract]
    public class ChatterVoice
    {
        private string _voiceName;
        private InstalledVoice _voice;

        public bool IsValid
        {
            get;
            private set;
        }

        [DataMember]
        [ProtoMember(1)]
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
            get => _voice;
            private set
            {
                _voice = value;

                IsValid = SpeechService.IsVoiceValid(this);
            }
        }

        public ChatterVoice()
        {
        }

        public ChatterVoice(string voiceName)
        {
            Voice = SpeechService.GetVoiceByName(voiceName);
        }

        public ChatterVoice(InstalledVoice voice)
        {
            Voice = voice;
        }
    }

    [DataContract]
    [ProtoContract]
    public class Chatter : DirtyBase
    {
        private string _name;
        private float _trembleFactor;
        private float _trembleSpeed;
        private float _trembleBegin;
        private float _pitch;
        private Vector3 _position;
        private short _speechRate;
        private sbyte _speechPitch;
        private ObservableDictionary<CultureInfo, ChatterVoice> _localizedChatterVoices;
        private DateTime _lastSpeakTime;
        private bool _isWeirdo;
        private byte _voiceRange;

        [DataMember]
        [ProtoMember(1)]
        public byte VoiceRange
        {
            get => _voiceRange;
            set
            {
                if (value == _voiceRange)
                    return;

                _voiceRange = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        [ProtoMember(2)]
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
        [ProtoMember(3)]
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

        [ProtoContract]
        private class ProtoVector3
        {
            [ProtoMember(1)]
            public float X;
            /// <summary>Gets or sets the y-component of the vector.</summary>
            [ProtoMember(2)]
            public float Y;
            /// <summary>Gets or sets the z-component of the vector.</summary>
            [ProtoMember(3)]
            public float Z;
        }

        [ProtoMember(4)]
        [UsedImplicitly]
        private ProtoVector3 ProtoPosition
        {
            get => new ProtoVector3()
            {
                X = Position.X,
                Y = Position.Y,
                Z = Position.Z,
            };
            set => Position = new Vector3()
            {
                X = value.X,
                Y = value.Y,
                Z = value.Z,
            };
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
        [ProtoMember(5)]
        public float Pitch
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
        [ProtoMember(6)]
        public sbyte SpeechPitch
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
        [ProtoMember(7)]
        public short SpeechRate
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
        [ProtoMember(8)]
        public float TrembleBegin
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
        [ProtoMember(9)]
        public float TrembleSpeed
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
        [ProtoMember(10)]
        public float TrembleFactor
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

        [ProtoMember(11)]
        [UsedImplicitly]
        private Dictionary<string, ChatterVoice> ProtoLocalizedChatterVoices
        {
            get => LocalizedChatterVoices.ToDictionary(x => x.Key.Name, x => x.Value);
            set => LocalizedChatterVoices = new ObservableDictionary<CultureInfo, ChatterVoice>(value.ToDictionary(x => CultureInfo.GetCultureInfo(x.Key), x => x.Value));
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
            _speechRate = (sbyte)RandomProvider.Random.Next(-80, 100);
            _speechPitch = (sbyte)RandomProvider.Random.Next(-50, 50);
            _isWeirdo = RandomProvider.Random.Next(100) == 0;
            _voiceRange = (byte)RandomProvider.Random.Next(1, 200);
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

        public string GetCode()
        {
            using (var ms = new MemoryStream())
            using (var zipStream = new ZlibStream(ms, CompressionMode.Compress, Ionic.Zlib.CompressionLevel.BestCompression, true))
            {
                Serializer.Serialize(zipStream, this);
                zipStream.Close();
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static Chatter FromCode(string code)
        {
            using (var ms = new MemoryStream())
            {
                var bytes = Convert.FromBase64String(code);
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                using (var zipStream = new ZlibStream(ms, CompressionMode.Decompress, Ionic.Zlib.CompressionLevel.BestCompression, false))
                {
                    return Serializer.Deserialize<Chatter>(zipStream);
                }
            }
        }
    }
}