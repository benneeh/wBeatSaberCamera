using System;
using System.Diagnostics;
using SpeechHost.WebApi.Requests;

namespace SpeechHost.WebApi.Hub
{
    public class SpeechHub : Microsoft.AspNet.SignalR.Hub
    {
        public string Hello()
        {
            return "World";
        }

        public byte[] Speak(SpeechRequest request)
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
                    responseBytes = Speech.Speech.SpeakSsml(request.Ssml, request.VoiceName);
                }

                if (!string.IsNullOrEmpty(request.Text))
                {
                    responseBytes = Speech.Speech.SpeakText(request.Text, request.VoiceName);
                }

                return responseBytes;
            }
#pragma warning disable 168
            catch (Exception ex)
#pragma warning restore 168
            {
                Console.WriteLine("Error while reading:\n" + (request.Ssml ?? request.Text) + "\n\n\n" + ex);

                // ignored
            }
            finally
            {
                Console.Title = "On hold";
                Console.WriteLine($"{DateTime.UtcNow.ToShortTimeString()}: Handling Speak took '{sw.Elapsed}'");
            }

            return null;
        }

        public void Stop()
        {
            Program.AppStopSource.SetResult("yea");
        }
    }
}