using System;
using System.Globalization;
using System.Linq;
using System.Windows;
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
            MainViewModel.TwitchBotConfigModel.Commands.Add(new TwitchChatCommand("rv", "Creates a new voice for the requester (optional param de/en to reset only specified language)", async (bot, chatCommand) =>
            {
                if (chatCommand.ArgumentsAsString.IsNullOrEmpty())
                {
                    bool wasChatterRemoved = false;
                    lock (MainViewModel.ChatViewModel.Chatters)
                    {
                        wasChatterRemoved = MainViewModel.ChatViewModel.RemoveChatter(chatCommand.ChatMessage.Username);
                    }
                    if (wasChatterRemoved)
                    {
                        await bot.SendMessage(chatCommand.ChatMessage.Channel, ":+1:");
                    }
                    return;
                }

                try
                {
                    var chatter = MainViewModel.ChatViewModel.GetChatterFromUsername(chatCommand.ChatMessage.Username);
                    
                    var cultureInfo = CultureInfo.GetCultureInfo(chatCommand.ArgumentsAsString);
                    if (chatter.LocalizedChatterVoices.ContainsKey(cultureInfo))
                    {
                        chatter.LocalizedChatterVoices.Remove(cultureInfo);
                        await bot.SendMessage(chatCommand.ChatMessage.Channel, ":+1:");
                    }
                }
                catch (Exception)
                {
                    await bot.SendMessage(chatCommand.ChatMessage.Channel, $"Could not find language '{chatCommand.ArgumentsAsString}'");
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

        private void SpeakSpecificUserCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MainViewModel.ChatViewModel.Speak(e.Parameter as string, TbText.Text);
        }

        private async void SpeakSpecificVoiceCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            await MainViewModel.ChatViewModel.SpeechService.Speak(e.Parameter as string, TbText.Text, true);
        }
    }
}