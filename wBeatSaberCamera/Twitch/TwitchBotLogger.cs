using Microsoft.Extensions.Logging;
using System;

namespace wBeatSaberCamera.Twitch
{
    internal class TwitchBotLogger<T> : ILogger<T>, IDisposable
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    Utils.Log.Debug(formatter(state, exception));
                    break;

                case LogLevel.Debug:
                    Utils.Log.Debug(formatter(state, exception));
                    break;

                case LogLevel.Information:
                    Utils.Log.Debug(formatter(state, exception));
                    break;

                case LogLevel.Warning:
                    Utils.Log.Warn(formatter(state, exception));
                    break;

                case LogLevel.Error:
                    Utils.Log.Error(formatter(state, exception));
                    break;

                case LogLevel.Critical:
                    Utils.Log.Error(formatter(state, exception));
                    break;

                case LogLevel.None:
                    Utils.Log.Debug(formatter(state, exception));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {
        }
    }
}