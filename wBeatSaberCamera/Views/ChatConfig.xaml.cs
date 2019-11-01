using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using wBeatSaberCamera.Models;
using wBeatSaberCamera.Twitch;

namespace wBeatSaberCamera.Views
{
    /// <summary>
    /// Interaction logic for ChatConfig.xaml
    /// </summary>
    public partial class ChatConfig : UserControl
    {
        private MainViewModel MainViewModel => Application.Current.Resources["MainViewModel"] as MainViewModel;

        public ChatConfig()
        {
            InitializeComponent();
            MainViewModel.TwitchBotConfigModel.Commands.Add(new TwitchChatCommand("rv", "Creates a new voice for the requester", (bot, msg) =>
            {
                lock (MainViewModel.ChatViewModel.Chatters)
                {
                    MainViewModel.ChatViewModel.RemoveChatter(msg.ChatMessage.Username);
                }
            }));
        }

        private void RemoveChatterCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is Chatter;
        }

        private void RemoveChatterCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var chatter = (Chatter)e.Parameter;
            lock (MainViewModel.ChatViewModel.Chatters)
            {
                MainViewModel.ChatViewModel.RemoveChatter(chatter.Name);
            }
        }

        private void SpeakCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MainViewModel.ChatViewModel.Speak(e.Parameter as string, TbText.Text);
        }
    }
}