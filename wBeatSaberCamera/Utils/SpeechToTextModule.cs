using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Speech.Recognition;
using System.Threading.Tasks;
using Newtonsoft.Json;
using wBeatSaberCamera.Models;
using wBeatSaberCamera.Models.FrankerFaceZModels;

namespace wBeatSaberCamera.Utils
{
    public class SpeechToTextModule : ObservableBase, IDisposable
    {
        private static readonly HttpClient s_httpClient = new HttpClient();
        private readonly ChatConfigModel _chatConfigModel;
        private readonly TwitchBotConfigModel _botConfigModel;
        private SpeechRecognitionEngine _speechRecognitionEngine;
        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;
        private readonly Dictionary<string, Task<string[]>> _emoteCache = new Dictionary<string, Task<string[]>>();
        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (value == _isBusy)
                    return;

                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public SpeechToTextModule(ChatConfigModel chatConfigModel, TwitchBotConfigModel botConfigModel)
        {
            _chatConfigModel = chatConfigModel;
            _botConfigModel = botConfigModel;
            botConfigModel.PropertyChanged += BotConfigModelPropertyChanged;
            chatConfigModel.PropertyChanged += ChatConfigModel_PropertyChanged;
            if (chatConfigModel.IsSpeechToTextEnabled)
            {
                Start();
            }
        }

        private void ChatConfigModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_chatConfigModel.IsSpeechToTextEnabled))
            {
                if (_chatConfigModel.IsSpeechToTextEnabled)
                {
                    Start();
                }
                else
                {
                    Stop();
                }
            }
        }

        private void BotConfigModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_botConfigModel.Channel))
            {
                if (_chatConfigModel.IsSpeechToTextEnabled)
                {
                    Stop();
                    Start();
                }
            }
        }

        private async void Start()
        {
            IsBusy = true;
            try
            {
                _speechRecognitionEngine?.Dispose();

                // Create an in-process speech recognizer for the en-US locale.
                var speechRecognitionEngine = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
                var choices = new Choices();
                if (!_emoteCache.ContainsKey(_botConfigModel.Channel))
                {
                    _emoteCache.Add(_botConfigModel.Channel, Task.Run(GetChannelEmotes));
                }

                choices.Add(await _emoteCache[_botConfigModel.Channel]);

                var keyWordsGrammarBuilder = new GrammarBuilder(choices);

                var keyWordsGrammar = new Grammar(keyWordsGrammarBuilder);
                speechRecognitionEngine.LoadGrammar(keyWordsGrammar);

                // Add a handler for the speech recognized event.
                speechRecognitionEngine.SpeechRecognized += (s, e) => SpeechRecognized?.Invoke(s, e);

                // Configure input to the speech recognizer.
                speechRecognitionEngine.SetInputToDefaultAudioDevice();

                // Start asynchronous, continuous speech recognition.
                speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);

                if (!_chatConfigModel.IsSpeechToTextEnabled)
                {
                    speechRecognitionEngine.Dispose();
                    return;
                }

                _speechRecognitionEngine = speechRecognitionEngine;
            }
            finally
            {
                IsBusy = false;
            }
        }

        void Stop()
        {
            _speechRecognitionEngine?.Dispose();
        }

        async Task<string[]> GetChannelEmotes()
        {
            // https://api.betterttv.net/2/channels/benneeeh
            // https://api.frankerfacez.com/v1/room/benneeeh

            var t1 = Task.Run(async () =>
            {
                try
                {
                    var resultString = await s_httpClient.GetStringAsync($"https://api.betterttv.net/2/channels/{_botConfigModel.Channel}");
                    var anon = new
                    {
                        emotes = new[]
                        {
                            new {code=""}
                        }
                    };
                    anon = JsonConvert.DeserializeAnonymousType(resultString, anon);
                    return anon.emotes.Select(x => x.code).ToArray();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return new string[0];
                }
            });
            var t2 = Task.Run(async () =>
            {
                try
                {
                    var resultString = await s_httpClient.GetStringAsync($"https://api.frankerfacez.com/v1/room/{_botConfigModel.Channel}");
                    var result = JsonConvert.DeserializeObject<FfzRoot>(resultString);
                    return result.Sets[result.Room.Set].Emoticons.Where(x => !x.Hidden).Select(x => x.Name).ToArray();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return new string[0];
                }
            });

            return (await Task.WhenAll(t1, t2)).SelectMany(x => x).ToArray();
        }

        public void Dispose()
        {
            _speechRecognitionEngine?.Dispose();
        }
    }
}