using System.IO;
using System.Speech.Synthesis;
using System.Linq;
using System.Text;

namespace Speech
{
    public class Speech
    {
        private static readonly SpeechSynthesizer s_speechSynthesizer = new SpeechSynthesizer();

        public static byte[] SpeakSsml(string ssml, string defaultVoiceName)
        {
            lock (s_speechSynthesizer)
            {
                using (var memoryStream = new MemoryStream())
                {
                    if (!string.IsNullOrEmpty(defaultVoiceName))
                    {
                        s_speechSynthesizer.SelectVoice(defaultVoiceName);
                    }

                    s_speechSynthesizer.SetOutputToWaveStream(memoryStream);

                    s_speechSynthesizer.SpeakSsml(ssml);

                    return memoryStream.ToArray();
                }
            }
        }

        public static byte[] SpeakText(string text, string voiceName = null)
        {
            lock (s_speechSynthesizer)
            {
                using (var memoryStream = new MemoryStream())
                {
                    s_speechSynthesizer.SetOutputToWaveStream(memoryStream);
                    if (!string.IsNullOrEmpty(voiceName))
                    {
                        s_speechSynthesizer.SelectVoice(voiceName);
                    }

                    s_speechSynthesizer.Speak(text);

                    return memoryStream.ToArray();
                }
            }
        }
    }
}