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
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Newtonsoft.Json;
using wBeatSaberCamera.Models;
using wBeatSaberCamera.Twitch;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Service
{
    public class SpeechHostClient : ObservableBase
    {
        private readonly int _port;
        private bool _isBusy;
        private HttpClient _httpClient = new HttpClient();

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

        public SpeechHostClient(int port)
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
                var message = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{_port}/api/Speech/SpeakSsml");
                message.Content = new StringContent(JsonConvert.SerializeObject(ssml), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    await stream.CopyToAsync(targetStream);
                }
                else
                {
                    Log.Error("Got unexpected response from SpeechHost:\n" + response.ToString());
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
            Process.Start($"{nameof(SpeechHost)}.exe", _port.ToString());

            return await RetryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetStringAsync($"http://localhost:{_port}/api/Speech/Hello");
                    if (response != "\"World\"")
                    {
                        Log.Error($"Expected the world, but only got '{response}'");
                        return false;
                    }

                    _busyStartingProcess.SetResult("finished");
                    return true;
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                    throw new TransientException(e.Message);
                }

                return false;
            });
        }
    }

    public class SpeechHostClientCache
    {
        private int cacheIndex;

        public ObservableCollection<SpeechHostClient> SpeechHostClients
        {
            get;
        } = new ObservableCollection<SpeechHostClient>();

        public SpeechHostClientCache()
        {
            BindingOperations.EnableCollectionSynchronization(SpeechHostClients, new object());
        }

        private async Task<SpeechHostClient> GetFreeClient()
        {
            SpeechHostClient client;

            try
            {
                client = await RetryPolicy.Execute(() =>
                {
                    int testCount = 0;
                    while (testCount++ < SpeechHostClients.Count)
                    {
                        if (cacheIndex > SpeechHostClients.Count - 1)
                        {
                            cacheIndex = 0;
                        }

                        client = SpeechHostClients[cacheIndex++ % SpeechHostClients.Count];

                        if (client.IsBusy)
                        {
                            continue;
                        }

                        return client;
                    }

                    throw new TransientException("All clients busy");
                }, 3);
                return client;
            }
            catch (Exception)
            {
                client = new SpeechHostClient(FreeRandomTcpPort());
                if (await client.Initialize())
                {
                    SpeechHostClients.Add(client);
                    return client;
                }
            }

            throw new Exception("Could not get/create a new SpeechHostClient");
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
                    Log.Warn(ex.ToString());
                    throw new TransientException(ex.Message);
                }
            });
        }
    }

    public class SpeechService
    {
        private readonly ChatConfigModel _chatConfigModel;
        private NaiveBayesLanguageIdentifier _lazyLanguagesIdentifier;
        private readonly AudioListener _audioListener;
        private readonly VrPositioningService _vrPositioningService;
        private readonly SpeechHostClientCache _speechHostClientCache = new SpeechHostClientCache();

        public SpeechService(ChatConfigModel chatConfigModel)
        {
            _chatConfigModel = chatConfigModel;
            _audioListener = new AudioListener();
            _vrPositioningService = new VrPositioningService();
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

                    if (memoryStream.Length == 0)
                    {
                        return;
                    }

                    memoryStream.Position = 0;
                    var wavDuration = TimeSpan.FromMilliseconds(50);
                    try
                    {
                        wavDuration = new NAudio.Wave.WaveFileReader(memoryStream).TotalTime - TimeSpan.FromMilliseconds(800);
                        if (wavDuration < TimeSpan.FromMilliseconds(50))
                        {
                            wavDuration = TimeSpan.FromMilliseconds(50);
                        }
                    }
                    catch
                    {
                        // meh
                    }

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

                        pitch *= _chatConfigModel.MaxPitchFactor;

                        //Console.WriteLine(pitch);
                        soundEffect.Pitch = (float)pitch;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while text text '{text}': " + ex);
            }
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

        public static CultureInfo GetCultureInfoFromIso3Name(string name)
        {
            return CultureInfo
                   .GetCultures(CultureTypes.NeutralCultures)
                   .FirstOrDefault(c => c.ThreeLetterISOLanguageName == name);
        }
    }
}