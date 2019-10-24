using Microsoft.AspNet.SignalR.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
using System.Threading.Tasks;
using System.Windows.Data;
using NAudio.Wave;
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

        public async Task FillStreamWithSpeech(string ssml, Stream targetStream)
        {
            IsBusy = true;
            var sw = Stopwatch.StartNew();
            await _busyStartingProcess.Task;

            try
            {
                var response = await _hubProxy.Invoke<byte[]>("SpeakSsml", ssml);
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
                        var newClient = new SpeechHostSignalRClient(FreeRandomTcpPort());
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

        private int FreeRandomTcpPort()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        public async Task FillStreamWithSpeech(string ssml, Stream targetStream)
        {
            await RetryPolicy.ExecuteAsync(async () =>
            {
                var client = await GetFreeClient();
                try
                {
                    await client.FillStreamWithSpeech(ssml, targetStream);
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
                await Speak(chatter, memoryStream);
            }
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
                        chatter.WriteSpeechToStream(language, text, memoryStream);
                    }
                    else
                    {
                        await _speechHostClientCache.FillStreamWithSpeech(chatter.GetSsmlFromText(language, text), memoryStream);
                    }

                    await Speak(chatter, memoryStream);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while text text '{text}': " + ex);
            }
        }

        private async Task Speak(Chatter chatter, MemoryStream memoryStream)
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
                if (_vrPositioningService.IsVrEnabled)
                {
                    var hmdPositioning = _vrPositioningService.GetHmdPositioning();
                    audioEmitter.Position = Vector3.Transform(chatter.Position, -hmdPositioning.Rotation);
                }

                soundEffect.Apply3D(_audioListener, audioEmitter);
                soundEffect.Play();

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
            using (var reader = new WaveFileReader(memoryStream))
            {
                var sampleProvider = reader.ToSampleProvider();

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
    }
}