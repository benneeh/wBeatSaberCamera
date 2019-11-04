using System;
using System.Speech.Recognition;
using System.Threading.Tasks;

namespace wBeatSaberCamera.Utils
{
    public class SpeechToTextModule : ObservableBase, IDisposable
    {
        private SpeechRecognitionEngine _speechRecognitionEngine;

        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;

        private bool _isBusy;

        public Func<Task<Grammar>> GrammarLoader = null;

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

        public async void Start()
        {
            try
            {
                _speechRecognitionEngine?.Dispose();

                // Create an in-process speech recognizer for the en-US locale.
                var speechRecognitionEngine = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));

                if (GrammarLoader != null)
                {
                    speechRecognitionEngine.LoadGrammar(await GrammarLoader());
                }

                // Add a handler for the speech recognized event.
                speechRecognitionEngine.SpeechRecognized += (s, e) => SpeechRecognized?.Invoke(s, e);

                // Configure input to the speech recognizer.
                speechRecognitionEngine.SetInputToDefaultAudioDevice();

                // Start asynchronous, continuous speech recognition.
                speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);

                _speechRecognitionEngine = speechRecognitionEngine;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Stop()
        {
            _speechRecognitionEngine?.Dispose();
        }

        public void Dispose()
        {
            _speechRecognitionEngine?.Dispose();
        }
    }
}