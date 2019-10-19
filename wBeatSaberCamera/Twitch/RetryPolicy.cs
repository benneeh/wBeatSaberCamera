using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Twitch
{
    internal class RetryPolicy
    {
        public static async Task Execute(Action action)
        {
            int tries = 0;
            while (true)
            {
                try
                {
                    action();
                    return;
                }
                catch (TransientException ex)
                {
                    Log.Warn(ex.ToString());
                    await Task.Delay(tries++ * 100);
                }
            }
        }

        public static async Task ExecuteAsync(Func<Task> action)
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

        public static async Task<TRet> Execute<TRet>(Func<TRet> action, int maxTries = 100)
        {
            int tries = 0;
            ExceptionDispatchInfo exceptionDispatchInfo = null;
            while (tries < maxTries)
            {
                try
                {
                    return action();
                }
                catch (TransientException ex)
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                    Log.Warn(ex.ToString());
                    await Task.Delay(tries++ * 100);
                }
            }

            // ReSharper disable once PossibleNullReferenceException
            exceptionDispatchInfo.Throw();
            throw new InvalidOperationException("noooooooooooooooo");
        }

        public static async Task<TRet> ExecuteAsync<TRet>(Func<Task<TRet>> action, int maxTries = 100)
        {
            int tries = 0;
            ExceptionDispatchInfo exceptionDispatchInfo = null;
            while (tries < maxTries)
            {
                try
                {
                    return await action();
                }
                catch (TransientException ex)
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                    Log.Warn(ex.ToString());
                    await Task.Delay(tries++ * 100);
                }
            }

            // ReSharper disable once PossibleNullReferenceException
            exceptionDispatchInfo.Throw();
            throw new InvalidOperationException("noooooooooooooooo");
        }
    }
}