using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace wBeatSaberCamera.Service
{
    public interface ISpeechHostClient : INotifyPropertyChanged, IDisposable
    {
        bool IsBusy { get; }

        Task FillStreamWithSpeech(string voiceName, string ssml, Stream targetStream);

        Task<bool> Initialize();
    }
}