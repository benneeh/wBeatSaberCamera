using System.Globalization;

namespace SpeechHost.WebApi.Requests
{
    public class SpeechRequest
    {
        public string VoiceName
        {
            get;
            set;
        }

        public string Text
        {
            get;
            set;
        }

        public string Ssml
        {
            get;
            set;
        }
    }
}