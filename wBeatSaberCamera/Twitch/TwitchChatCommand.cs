using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TwitchLib.Client.Models;
using wBeatSaberCamera.Annotations;

namespace wBeatSaberCamera.Twitch
{
    public sealed class TwitchChatCommand : INotifyPropertyChanged
    {
        private readonly Func<TwitchBot, ChatCommand, Task> _action;
        private string _description;
        private ObservableCollection<string> _commands;
        private bool _isVisibleInHelp;

        public ObservableCollection<string> Commands
        {
            get => _commands;
            set
            {
                if (value == _commands)
                {
                    return;
                }

                _commands = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (value == _description)
                {
                    return;
                }

                _description = value;
                OnPropertyChanged();
            }
        }

        public bool IsVisibleInHelp
        {
            get => _isVisibleInHelp;
            set
            {
                if (value == _isVisibleInHelp)
                {
                    return;
                }

                _isVisibleInHelp = value;
                OnPropertyChanged();
            }
        }

        [PublicAPI]
        public TwitchChatCommand(string commands, string description, Action<TwitchBot, ChatCommand> action, bool isVisibleInHelp = true)
            : this(commands, description,
                (t, c) =>
                {
                    action(t, c);
                    return Task.CompletedTask;
                }, isVisibleInHelp)
        {
        }

        [PublicAPI]
        public TwitchChatCommand(string command, string description, Func<TwitchBot, ChatCommand, Task> action, bool isVisibleInHelp = true)
            : this(new List<string>() { command }, description, action, isVisibleInHelp)

        {
        }

        [PublicAPI]
        public TwitchChatCommand(IEnumerable<string> commands, string description, Func<TwitchBot, ChatCommand, Task> action, bool isVisibleInHelp = true)
        {
            _action = action;
            Commands = new ObservableCollection<string>(commands);
            Description = description;
            IsVisibleInHelp = isVisibleInHelp;
        }

        public async Task Execute(TwitchBot twitchBot, ChatCommand chatCommand)
        {
            await _action(twitchBot, chatCommand);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}