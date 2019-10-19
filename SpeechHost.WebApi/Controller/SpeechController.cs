using SpeechHost.Requests;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Web.Http;

namespace SpeechHost.Controller
{
    [RoutePrefix("api/Speech")]
    public class SpeechController : ApiController
    {
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
                byte[] responseBytes = null;
                if (!string.IsNullOrEmpty(request.Ssml))
                {
                    //new PromptBuilder().StartStyle(new PromptStyle()
                    responseBytes = Speech.Speech.SpeakSsml(request.Ssml);
                }

                if (!string.IsNullOrEmpty(request.Text))
                {
                    responseBytes = Speech.Speech.SpeakText(request.Text, request.VoiceName);
                }

                var response = new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(responseBytes)
                };
                return response;
            }
#pragma warning disable 168
            catch (Exception ex)
#pragma warning restore 168
            {
                Console.WriteLine(ex);

                // ignored
            }
            finally
            {
                Console.Title = "On hold";
                Console.WriteLine($"{DateTime.UtcNow.ToShortTimeString()}: Handling Speak took '{sw.Elapsed}'");
            }

            return null;
        }

        [HttpGet]
        [Route("Stop")]
        public void Stop()
        {
            Program.AppStopSource.SetResult("yea");
        }
    }
}