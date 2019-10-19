using SpeechHost.Requests;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Speech.Synthesis;
using System.Web.Http;

namespace SpeechHost.Controller
{
    [RoutePrefix("api/Speech")]
    public class SpeechController : ApiController
    {
        private static readonly SpeechSynthesizer s_speechSynthesizer = new SpeechSynthesizer();

        [Route("Hello")]
        [HttpGet]
        public string Hello()
        {
            return "World";
        }

        [Route("SpeakSsml")]
        [HttpPost]
        public HttpResponseMessage SpeakSsml([FromBody]string ssml)
        {
            return Speak(new SpeechRequest()
            {
                Ssml = ssml
            });
        }

        [Route("SpeakText")]
        [HttpPost]
        public HttpResponseMessage SpeakText([FromBody]string text, string voiceName = null)
        {
            return Speak(new SpeechRequest()
            {
                Text = text,
                VoiceName = voiceName
            });
        }

        [Route("Speak")]
        [HttpGet]
        public HttpResponseMessage Speak([FromBody] SpeechRequest request)
        {
            Program.ReportActivity();
            var sw = Stopwatch.StartNew();
            Console.Title = "Busy";
            try
            {
                lock (s_speechSynthesizer)
                {
                    try
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            s_speechSynthesizer.SetOutputToWaveStream(memoryStream);

                            if (!string.IsNullOrEmpty(request.Ssml))
                            {
                                //new PromptBuilder().StartStyle(new PromptStyle()
                                s_speechSynthesizer.SpeakSsml(request.Ssml);
                            }
                            else if (!string.IsNullOrEmpty(request.Text))
                            {
                                if (!string.IsNullOrEmpty(request.VoiceName))
                                {
                                    s_speechSynthesizer.SelectVoice(request.VoiceName);
                                }

                                s_speechSynthesizer.Speak(request.Text);
                            }

                            var response = new HttpResponseMessage();
                            response.Content = new ByteArrayContent(memoryStream.ToArray());
                            return response;
                        }
                    }
#pragma warning disable 168
                    catch (Exception ex)
#pragma warning restore 168
                    {
                        // ignored
                    }

                    return null;
                }
            }
            finally
            {
                Console.Title = "On hold";
                Console.WriteLine($"{DateTime.UtcNow.ToShortTimeString()}: Handling Speak took '{sw.Elapsed}'");
            }
        }

        [HttpGet]
        [Route("Stop")]
        public void Stop()
        {
            Program.AppStopSource.SetResult("yea");
        }
    }
}