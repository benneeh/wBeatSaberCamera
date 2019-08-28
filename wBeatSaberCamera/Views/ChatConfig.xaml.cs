﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            MainViewModel.TwitchBotConfigModel.Commands.Add(new TwitchChatCommand("rv", "Creates a new voice for the requester", (bot, msg) => {
                MainViewModel.ChatConfigModel.Chatters.Remove(msg.ChatMessage.Username);
            }));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.ChatConfigModel.Spek(null, TbText.Text);
        }

        private void RemoveChatterCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is Chatter;
        }

        private void RemoveChatterCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var chatter = (Chatter)e.Parameter;
            MainViewModel.ChatConfigModel.Chatters.Remove(chatter.Name);
        }
    }
}