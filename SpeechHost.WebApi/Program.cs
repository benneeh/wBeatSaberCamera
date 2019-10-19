using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Hosting;
using Owin;

namespace SpeechHost
{
    class Program
    {
        public static readonly TaskCompletionSource<object> AppStopSource = new TaskCompletionSource<object>();

        static void Main(string[] args)
        {
            try
            {
                bool launch = true;
                var url = "http://localhost:16531";
                if (args.Length > 0)
                {
                    url = new Uri($"http://localhost:{args[0]}").ToString();
                    launch = false;
                }

                using (WebApp.Start<Startup>(url: url))
                {
                    if (launch)
                    {
                        Process.Start(url + "/api/Speech/SpeakText?text=speach+host+started");
                    }
                    ReportActivity();

                    AppStopSource.Task.Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }

        private static (Task Task, CancellationTokenSource CancellationTokenSource)? _activityTask;

        public static void ReportActivity()
        {
            _activityTask?.CancellationTokenSource.Cancel();

            var cancellationTokenSource = new CancellationTokenSource();

            _activityTask = (Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                AppStopSource.SetResult("AutoStop");
            }), cancellationTokenSource);
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            config.MapHttpAttributeRoutes();

            app.UseWebApi(config);

            app.MapSignalR();
        }
    }
}
