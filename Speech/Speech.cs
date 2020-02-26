using System.IO;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;

namespace Speech
{
    public class Speech
    {
        public static readonly SpeechSynthesizer SpeechSynthesizer = new SpeechSynthesizer();

        public const int SPEECH_SAMPLE_RATE = 22050;
        public const int SPEECH_BITS_PER_SAMPLE = 16;
        public const int SPEECH_CHANNELS = 1;

        public static byte[] SpeakSsml(string ssml, string defaultVoiceName, SpeechSynthesizer speechSynthesizer = null)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            using (var memoryStream = new MemoryStream())
            {
                SpeakSsml(ssml, defaultVoiceName, memoryStream, speechSynthesizer);

                return memoryStream.ToArray();
            }
        }

        public static void SpeakSsml(string ssml, string defaultVoiceName, MemoryStream memoryStream, SpeechSynthesizer speechSynthesizer = null)
        {
            speechSynthesizer = speechSynthesizer ?? SpeechSynthesizer;
            lock (speechSynthesizer)
            {
                if (!string.IsNullOrEmpty(defaultVoiceName))
                {
                    speechSynthesizer.SelectVoice(defaultVoiceName);
                }

                SpeechSynthesizer.SetOutputToAudioStream(memoryStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, SPEECH_SAMPLE_RATE, SPEECH_BITS_PER_SAMPLE, SPEECH_CHANNELS, SPEECH_SAMPLE_RATE * SPEECH_CHANNELS * SPEECH_BITS_PER_SAMPLE / 8, 2, null));
                //speechSynthesizer.SetOutputToWaveStream(memoryStream);

                speechSynthesizer.SpeakSsml(ssml);
            }
        }

        public static byte[] SpeakText(string text, string voiceName = null, SpeechSynthesizer speechSynthesizer = null)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            speechSynthesizer = speechSynthesizer ?? SpeechSynthesizer;
            using (var memoryStream = new MemoryStream())
            {
                SpeakText(text, voiceName, memoryStream, speechSynthesizer);

                return memoryStream.ToArray();
            }
        }

        public static void SpeakText(string text, string defaultVoiceName, MemoryStream memoryStream, SpeechSynthesizer speechSynthesizer = null)
        {
            speechSynthesizer = speechSynthesizer ?? SpeechSynthesizer;
            lock (speechSynthesizer)
            {
                if (!string.IsNullOrEmpty(defaultVoiceName))
                {
                    speechSynthesizer.SelectVoice(defaultVoiceName);
                }

                SpeechSynthesizer.SetOutputToAudioStream(memoryStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, SPEECH_SAMPLE_RATE, SPEECH_BITS_PER_SAMPLE, SPEECH_CHANNELS, SPEECH_SAMPLE_RATE * SPEECH_CHANNELS * SPEECH_BITS_PER_SAMPLE / 8, 2, null));

                //speechSynthesizer.SetOutputToWaveStream(memoryStream);

                speechSynthesizer.Speak(text);
            }
        }
    }
}