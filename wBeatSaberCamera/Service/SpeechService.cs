using Microsoft.AspNet.SignalR.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using NAudio.Wave;
using NTextCat;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Linq;
using SpeechHost.WebApi.Requests;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.Models;
using wBeatSaberCamera.Twitch;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Service
{
    public class SpeechHostSignalRClient : ObservableBase, ISpeechHostClient
    {
        private readonly int _port;
        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (value == _isBusy)
                    return;

                _isBusy = value;
                OnPropertyChanged();
            }
        }

        private readonly TaskCompletionSource<object> _busyStartingProcess = new TaskCompletionSource<object>();
        private IHubProxy _hubProxy;
        private HubConnection _hubConnection;

        public SpeechHostSignalRClient(int port)
        {
            _port = port;
        }

        public async Task FillStreamWithSpeech(string voiceName, string ssml, Stream targetStream)
        {
            IsBusy = true;
            var sw = Stopwatch.StartNew();
            await _busyStartingProcess.Task;

            try
            {
                var response = await _hubProxy.Invoke<byte[]>(
                    "Speak",
                    new SpeechRequest()
                    {
                        Ssml = ssml,
                        VoiceName = voiceName
                    });
                if (response != null)
                {
                    targetStream.Write(response, 0, response.Length);
                }
            }
            finally
            {
                Console.WriteLine($"{DateTime.UtcNow.ToShortTimeString()}: Handling Speak took '{sw.Elapsed}'");
                IsBusy = false;
            }
        }

        public async Task<bool> Initialize()
        {
            var launchParams = new ProcessStartInfo()
            {
                FileName = "SpeechHost.WebApi.exe",
                Arguments = $"{_port}",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(launchParams);

            _hubConnection = new HubConnection($"http://localhost:{_port}/signalr");
            _hubProxy = _hubConnection.CreateHubProxy("SpeechHub");
            await _hubConnection.Start();

            return await RetryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    var response = await _hubProxy.Invoke<string>("Hello");
                    if (response != "World")
                    {
                        Log.Error($"Expected the world, but only got '{response}'");
                        return false;
                    }

                    _busyStartingProcess.SetResult("finished");
                    return true;
                }
                catch (Exception e)
                {
                    throw new TransientException(e);
                }
            });
        }

        public void Dispose()
        {
            _hubConnection.Stop();
        }
    }

    public class SpeechHostClientCache
    {
        private int _cacheIndex;
        private Task<ISpeechHostClient> _clientCreator;

        [PublicAPI]
        public ObservableCollection<ISpeechHostClient> SpeechHostClients
        {
            get;
        } = new ObservableCollection<ISpeechHostClient>();

        public SpeechHostClientCache()
        {
            BindingOperations.EnableCollectionSynchronization(SpeechHostClients, new object());
        }

        private async Task<ISpeechHostClient> GetFreeClient(int tries = 3)
        {
            ISpeechHostClient client;

            try
            {
                client = await RetryPolicy.Execute(() =>
                {
                    int testCount = 0;
                    while (testCount++ < SpeechHostClients.Count)
                    {
                        if (_cacheIndex > SpeechHostClients.Count - 1)
                        {
                            _cacheIndex = 0;
                        }

                        client = SpeechHostClients[_cacheIndex++ % SpeechHostClients.Count];

                        if (client.IsBusy)
                        {
                            continue;
                        }

                        return client;
                    }

                    throw new TransientException("All clients busy");
                }, tries);
                return client;
            }
            catch (Exception)
            {
                if (_clientCreator == null || _clientCreator.IsCompleted)
                {
                    _clientCreator = Task.Run(async () =>
                    {
                        var newClient = new SpeechHostSignalRClient(GetFreeRandomTcpPort());
                        if (await newClient.Initialize())
                        {
                            SpeechHostClients.Add(newClient);
                            return (ISpeechHostClient)newClient;
                        }

                        newClient.Dispose();

                        throw new InvalidOperationException("Couldn't create new client");
                    });
                }

                return await _clientCreator;
            }
        }

        private int GetFreeRandomTcpPort()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        public async Task FillStreamWithSpeech(string voiceName, string ssml, Stream targetStream)
        {
            await RetryPolicy.ExecuteAsync(async () =>
            {
                var client = await GetFreeClient();
                try
                {
                    await client.FillStreamWithSpeech(voiceName, ssml, targetStream);
                }
                catch (Exception ex)
                {
                    SpeechHostClients.Remove(client);
                    client.Dispose();
                    throw new TransientException(ex);
                }
            });
        }
    }

    public class SpeechService
    {
        private readonly ChatViewModel _chatViewModel;
        private NaiveBayesLanguageIdentifier _lazyLanguagesIdentifier;
        private readonly AudioListener _audioListener;
        private readonly VrPositioningService _vrPositioningService;
        private readonly SpeechHostClientCache _speechHostClientCache = new SpeechHostClientCache();
        private static ReadOnlyCollection<ChatterVoice> _voices;
        private static readonly Random s_random = new Random();

        private static ReadOnlyCollection<ChatterVoice> Voices
        {
            get
            {
                if (_voices == null)
                {
                    using (var synthesizer = new SpeechSynthesizer())
                    {
                        _voices = new ReadOnlyCollection<ChatterVoice>(synthesizer.GetInstalledVoices().Select(x => new ChatterVoice(x)).ToList());
                    }
                }

                return _voices;
            }
        }

        public SpeechService(ChatViewModel chatViewModel)
        {
            _chatViewModel = chatViewModel;
            _audioListener = new AudioListener();
            _vrPositioningService = new VrPositioningService();
        }

        public async Task Speak(Chatter chatter, byte[] audioData)
        {
            using (var memoryStream = new MemoryStream(audioData))
            {
                await PlaySound(memoryStream, chatter);
            }
        }

        public async Task Speak(string voiceName, string text, bool useLocalSpeak)
        {
            var language = GetLanguageFromText(text);
            var chatter = new Chatter()
            {
                LocalizedChatterVoices = new ObservableDictionary<CultureInfo, ChatterVoice>()
                {
                    {language, GetChatterVoiceByName(voiceName)}
                }
            };

            await Speak(chatter, text, useLocalSpeak);
        }

        public async Task Speak(Chatter chatter, string text, bool useLocalSpeak)
        {
            chatter.LastSpeakTime = DateTime.UtcNow;

            var language = GetLanguageFromText(text);
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    if (useLocalSpeak)
                    {
                        WriteSpeechToStream(chatter, language, text, memoryStream);
                    }
                    else
                    {
                        var voiceForLanguage = chatter.GetVoiceForLanguage(language);
                        await _speechHostClientCache.FillStreamWithSpeech(voiceForLanguage.VoiceName, GetSsmlFromText(chatter, text, voiceForLanguage.VoiceName), memoryStream);
                    }

                    await PlaySound(memoryStream, chatter);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while text text '{text}': " + ex);
            }
        }

        public async Task SpeakSsml(string voiceName, string ssml, bool useLocalSpeak)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    if (useLocalSpeak)
                    {
                        Speech.Speech.SpeakSsml(ssml, null, memoryStream);
                    }
                    else
                    {
                        await _speechHostClientCache.FillStreamWithSpeech(voiceName, ssml, memoryStream);
                    }

                    await PlaySound(memoryStream);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while text text '{ssml}': " + ex);
            }
        }

        private async Task PlaySound(MemoryStream memoryStream, Chatter chatter = null)
        {
            if (memoryStream.Length == 0)
            {
                return;
            }

            memoryStream.Position = 0;
            var wavDuration = TimeSpan.FromMilliseconds(50);
            try
            {
                var normalizeResult = NormalizeAudio(memoryStream);
                wavDuration = normalizeResult.WavDuration;
                memoryStream = normalizeResult.NormalizedStream;

                if (wavDuration < TimeSpan.FromMilliseconds(50))
                {
                    wavDuration = TimeSpan.FromMilliseconds(50);
                }
            }
            catch
            {
                // meh
            }

            using (memoryStream)
            {
                memoryStream.Position = 0;
                var soundEffect = SoundEffect.FromStream(memoryStream).CreateInstance();
                var audioEmitter = new AudioEmitter();
                if (_vrPositioningService.IsVrEnabled && chatter != null)
                {
                    var hmdPositioning = _vrPositioningService.GetHmdPositioning();
                    audioEmitter.Position = Vector3.Transform(chatter.Position, -hmdPositioning.Rotation);
                }

                soundEffect.Apply3D(_audioListener, audioEmitter);
                soundEffect.Play();

                if (chatter == null)
                {
                    await Task.Delay(wavDuration);
                    return;
                }

                double sineTime = chatter.TrembleBegin;
                var stopWatch = Stopwatch.StartNew();
                while (stopWatch.Elapsed < wavDuration)
                {
                    sineTime += chatter.TrembleSpeed;
                    await Task.Delay(10);

                    if (_vrPositioningService.IsVrEnabled)
                    {
                        var hmdPositioning = _vrPositioningService.GetHmdPositioning();

                        var newAudioEmitterPosition = Vector3.Transform(chatter.Position, hmdPositioning.Rotation);
                        audioEmitter.Velocity = (newAudioEmitterPosition - audioEmitter.Position) * 100;
                        audioEmitter.Position = newAudioEmitterPosition;

                        _audioListener.Velocity = hmdPositioning.Velocity - audioEmitter.Position + Vector3.Transform(audioEmitter.Position, new Quaternion(hmdPositioning.Omega, 1));

                        //_audioListener.Position = hmdPositioning.Position;
                        //Console.WriteLine(audioEmitter.Position + "/" + _audioListener.Position);
                        soundEffect.Apply3D(_audioListener, audioEmitter);

                        //am.Rotation = position.GetRotation();
                    }

                    var pitch = chatter.Pitch + Math.Sin(sineTime) * chatter.TrembleFactor;
                    if (pitch < -1)
                    {
                        pitch = -1;
                    }

                    if (pitch > 1)
                    {
                        pitch = 1;
                    }

                    pitch *= _chatViewModel.MaxPitchFactor;

                    //Console.WriteLine(pitch);
                    soundEffect.Pitch = (float)pitch;
                }
            }
        }

        private (MemoryStream NormalizedStream, TimeSpan WavDuration) NormalizeAudio(MemoryStream memoryStream)
        {
            float max = 0;
            TimeSpan wavDuration;
            //using var reader = new RawSourceWaveStream(memoryStream, new WaveFormat(16000, 16, 1));
            using (var reader = new RawSourceWaveStream(memoryStream, new WaveFormat(Speech.Speech.SPEECH_SAMPLE_RATE, Speech.Speech.SPEECH_BITS_PER_SAMPLE, Speech.Speech.SPEECH_CHANNELS)))
            {
                var sampleProvider = reader.ToSampleProvider();
                //Console.WriteLine("BitsPerSample:" + sampleProvider.WaveFormat.BitsPerSample);
                //Console.WriteLine("SampleRate:" + sampleProvider.WaveFormat.SampleRate);
                //Console.WriteLine("Encoding:" + sampleProvider.WaveFormat.Encoding);
                wavDuration = reader.TotalTime;
                // find the max peak
                float[] buffer = new float[sampleProvider.WaveFormat.SampleRate];
                int read;
                do
                {
                    read = sampleProvider.Read(buffer, 0, buffer.Length);
                    for (int n = 0; n < read; n++)
                    {
                        var abs = Math.Abs(buffer[n]);
                        if (abs > max) max = abs;
                    }
                } while (read > 0);
                Console.WriteLine($"Max sample value: {max}");

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (max == 0 || max > 1.0f)
                {
                    return (memoryStream, wavDuration);
                }

                // rewind and amplify
                reader.Position = 0;
                var volumeModifier = new VolumeWaveProvider16(reader)
                {
                    Volume = 1.0f / max
                };

                // write out to a new WAV file
                var resultStream = new MemoryStream();
                WaveFileWriter.WriteWavFileToStream(resultStream, volumeModifier);
                memoryStream = resultStream;
            }

            return (memoryStream, wavDuration);
        }

        private CultureInfo GetLanguageFromText(string text)
        {
            if (_lazyLanguagesIdentifier == null)
            {
                var factory = new NaiveBayesLanguageIdentifierFactory();

                _lazyLanguagesIdentifier = factory.Load(Assembly.GetExecutingAssembly().GetManifestResourceStream("wBeatSaberCamera.Ressource.LanguageProfile.xml"));
            }
            var res = _lazyLanguagesIdentifier.Identify(text);
            return GetCultureInfoFromIso3Name(res.FirstOrDefault()?.Item1.Iso639_3) ?? CultureInfo.InvariantCulture;
        }

        [PublicAPI]
        public static CultureInfo GetCultureInfoFromIso3Name(string name)
        {
            return CultureInfo
                   .GetCultures(CultureTypes.NeutralCultures)
                   .FirstOrDefault(c => c.ThreeLetterISOLanguageName == name);
        }

        public void WriteSpeechToStream(Chatter chatter, CultureInfo language, string text, MemoryStream ms)
        {
            var voiceForLanguage = chatter.GetVoiceForLanguage(language);
            var ssml = GetSsmlFromText(chatter, text, voiceForLanguage.VoiceName);

            Speech.Speech.SpeakSsml(ssml, null, ms);
        }

        private static readonly Regex s_urlRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)", RegexOptions.Compiled);
        private static readonly Regex s_ohReplacementRegex = new Regex("^(([a-zA-Z]{2,2})|([a-zA-ZöÖäÄüÜ)]{1,1}[aAeEiIoOuUöyYÖäÄüÜhH]{1,}[a-zA-Z]{1,1}))$", RegexOptions.Compiled);

        private static string GetSsmlFromText(Chatter chatter, string text, string voiceName)
        {
            Console.WriteLine($"{voiceName}: {chatter.SpeechPitch}{chatter.SpeechRate}");
            text = s_urlRegex.Replace(text, "URL");
            var words = text.Split(new[] { ' ' }, StringSplitOptions.None);
            var woahBuilder = new StringBuilder();
            foreach (var word in words)
            {
                if (RandomProvider.Random.Next(10) > 8 || s_ohReplacementRegex.Match(word).Success)
                {
                    woahBuilder.Append($"<prosody pitch=\"{RandomProvider.Random.Next(-100, 100):+#;-#;+0}%\" rate=\"{RandomProvider.Random.Next(-100, 50):+#;-#;+0}%\">{new XText(word)}</prosody>");
                }
                else
                {
                    woahBuilder.Append(new XText(word));
                }
            }

            var woahText = woahBuilder.ToString(); //;s_ohReplacementRegex.Replace(text, (match) => $"<prosody pitch=\"{RandomProvider.Random.Next(-50, 50):+#;-#;0}%\" rate=\"{RandomProvider.Random.Next(50)}%\">{new XText(match.Value)}</prosody>");
            var ssml = $@"
<speak version=""1.0"" xmlns=""https://www.w3.org/2001/10/synthesis"" xml:lang=""en-US"">
    <voice name=""{voiceName}"">
        <prosody pitch=""{chatter.SpeechPitch:+#;-#;+0}%"" rate=""{chatter.SpeechRate:+#;-#;+0}%"">
            {woahText}
        </prosody>
    </voice>
</speak>";

            //
            //</prosody>
            //var ohTemplate = "<prosody pitch=\"+50%\" rate=\"1%\">{0}</prosody>"; // <prosody rate="10%" contour="(0%,+20Hz) (50%,+420Hz) (100%, +10Hz)">oh</prosody>

            return ssml;
        }

        #region get voice

        public static bool IsVoiceValid(ChatterVoice voice)
        {
            if (voice.Voice == null || !voice.Voice.Enabled)
            {
                return false;
            }

            return IsVoiceValid(voice.VoiceName);
        }

        public static bool IsVoiceValid(string voiceName)
        {
            try
            {
                lock (Speech.Speech.SpeechSynthesizer)
                {
                    Speech.Speech.SpeechSynthesizer.SelectVoice(voiceName);
                }

                return true;
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        [PublicAPI]
        public static VoiceInfo GetRandomVoice([CanBeNull] CultureInfo language)
        {
            ReadOnlyCollection<ChatterVoice> voices = Voices;
            if (language != null)
            {
                voices = new ReadOnlyCollection<ChatterVoice>(voices.Where(x => x.IsValid && x.Voice.VoiceInfo.Culture.Equals(language) || x.Voice.VoiceInfo.Culture.Parent.Equals(language)).ToList());
            }

            if (voices.Count == 0)
            {
                voices = Voices;
            }

            var selectedVoice = voices[s_random.Next(voices.Count)].Voice.VoiceInfo;

            return selectedVoice;
        }

        [PublicAPI]
        [CanBeNull]
        public static InstalledVoice GetVoiceByName(string name)
        {
            return GetChatterVoiceByName(name)?.Voice;
        }

        [PublicAPI]
        [CanBeNull]
        public static ChatterVoice GetChatterVoiceByName(string name)
        {
            return Voices.FirstOrDefault(x => x.VoiceName == name);
        }

        [PublicAPI]
        [CanBeNull]
        private InstalledVoice GetVoiceById(string id)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            return Voices.FirstOrDefault(x => x.Voice.VoiceInfo.Id == id)?.Voice;
        }

        #endregion get voice
    }
}