using System;
using System.Threading.Tasks;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Twitch
{
    internal class RetryPolicy
    {
        public async Task ExecuteAsync(Func<Task> action)
        {
            int tries = 0;
            while (true)
            {
                try
                {
                    await action();
                    return;
                }
                catch (TransientException ex)
                {
                    Log.Warn(ex.ToString());
                    await Task.Delay(tries++ * 100);
                }
            }
        }
    }
}