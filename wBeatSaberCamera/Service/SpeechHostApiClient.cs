using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpeechHost.WebApi.Requests;
using wBeatSaberCamera.Annotations;
using wBeatSaberCamera.Twitch;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Service
{
    [PublicAPI]
    public class SpeechHostApiClient : ObservableBase, ISpeechHostClient
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

        public SpeechHostApiClient(int port)
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
                var message = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{_port}/api/Speech/SpeakSsml");
                message.Content = new StringContent(JsonConvert.SerializeObject(new SpeechRequest()
                {
                    VoiceName = voiceName,
                    Ssml = ssml
                }), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    await stream.CopyToAsync(targetStream);
                }
                else
                {
                    Log.Error("Got unexpected response from SpeechHost:\n" + response);
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
            Process.Start("SpeechHost.WebApi.exe", _port.ToString());

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
                    throw new TransientException(e);
                }
            });
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}