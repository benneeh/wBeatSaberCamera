using System;

namespace wBeatSaberCamera.Twitch
{
    internal class TransientException : Exception
    {
        public TransientException(string message)
            : base(message)
        {
        }
    }
}