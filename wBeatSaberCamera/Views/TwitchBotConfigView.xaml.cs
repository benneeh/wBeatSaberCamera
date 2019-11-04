using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using wBeatSaberCamera.Models;
using wBeatSaberCamera.Twitch;
using wBeatSaberCamera.Utils;
using wBeatSaberCamera.Utils.OAuth;

namespace wBeatSaberCamera.Views
{
    /// <summary>
    /// Interaction logic for BotSettings.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class TwitchBotConfigView : UserControl
    {
        private MainViewModel MainViewModel => Application.Current.Resources["MainViewModel"] as MainViewModel;
        private CameraConfigModel CameraConfigModel => MainViewModel.CameraConfigModel;

        public TwitchBotConfigView()
        {
            InitializeComponent();
            MainViewModel.SpeechToTextModule = new SpeechToTextModule(MainViewModel.ChatViewModel, MainViewModel.TwitchBotConfigModel);
            MainViewModel.SpeechToTextModule.SpeechRecognized += _speechToTextModule_SpeechRecognized;
            MainViewModel.TwitchBot = new TwitchBot(MainViewModel.ChatViewModel, MainViewModel.TwitchBotConfigModel);
            //MainViewModel.TwitchBotConfigModel.Commands.Add(
            //    new TwitchChatCommand(
            //        "fpv",
            //        "Toggles camera view between first person view and third person view",
            //        async (twitchBot, channel, commandParams) =>
            //        {
            //            CameraConfigModel.GameCameraProfile.CameraPlusConfig.IsThirdPersonView = !CameraConfigModel.GameCameraProfile.CameraPlusConfig.IsThirdPersonView;

            //            await twitchBot.SendMessage(channel, CameraConfigModel.GameCameraProfile.CameraPlusConfig.IsThirdPersonView ? "Switching to third person view" : "Switching to first person view");

            //            try
            //            {
            //                CameraConfigModel.GameCameraProfile.CameraPlusConfig.SaveToBeatSaber(AppConfigModel);
            //            }
            //            catch (Exception ex)
            //            {
            //                await twitchBot.SendMessage(channel, $"Error while trying to update camera config: '{ex.Message}'");
            //            }
            //        }));

            //MainViewModel.TwitchBotConfigModel.Commands.Add(
            //    new TwitchChatCommand(
            //        "tw",
            //        "Toggles walls transparency",
            //        async (twitchBot, channel, commandParams) =>
            //        {
            //            CameraConfigModel.GameCameraProfile.CameraPlusConfig.MakeWallsTransparent = !CameraConfigModel.GameCameraProfile.CameraPlusConfig.MakeWallsTransparent;

            //            await twitchBot.SendMessage(channel, CameraConfigModel.GameCameraProfile.CameraPlusConfig.MakeWallsTransparent ? "Making walls transparent" : "Reverting to original walls");

            //            try
            //            {
            //                CameraConfigModel.GameCameraProfile.CameraPlusConfig.SaveToBeatSaber(AppConfigModel);
            //            }
            //            catch (Exception ex)
            //            {
            //                await twitchBot.SendMessage(channel, $"Error while trying to update camera config: '{ex.Message}'");
            //            }
            //        }));

            MainViewModel.TwitchBotConfigModel.Commands.Add(
                new TwitchChatCommand(
                    new[] { "help", "commands" },
                    "Shows this help",
                    async (twitchBot, chatCommand) =>
                    {
                        await twitchBot.SendMessage(chatCommand.ChatMessage.Channel, $"This bot supports the following commands with prefixes '{string.Join("/", MainViewModel.TwitchBotConfigModel.CommandIdentifiers)}':");
                        foreach (var command in MainViewModel.TwitchBotConfigModel.Commands.Where(x => x.IsVisibleInHelp))
                        {
                            await twitchBot.SendMessage(chatCommand.ChatMessage.Channel, $"'{string.Join("/", command.Commands)}': {command.Description}");
                        }
                    },
                    false
                ));

            MainViewModel.TwitchBotConfigModel.Commands.Add(
                new TwitchChatCommand(
                    new[] { "cp", "cam", "camera", "cameraProfiles" },
                    "Selects a camera profile (with parameter), or shows a list of camera profiles (no parameter)",
                    async (twitchBot, chatCommand) =>
                    {
                        if (chatCommand.ArgumentsAsString.IsNullOrEmpty())
                        {
                            await twitchBot.SendMessage(chatCommand.ChatMessage.Channel, "This bot currently has following camera profiles:");
                            foreach (var profile in MainViewModel.CameraConfigModel.Profiles.Where(x => x.IsChoosableByViewers))
                            {
                                await twitchBot.SendMessage(chatCommand.ChatMessage.Channel, $"'{string.Join("' / '", profile.Aliases.Select(x => x.Alias))}': {profile.Name}");
                            }

                            return;
                        }

                        foreach (var profile in MainViewModel.CameraConfigModel.Profiles.Where(x => x.IsChoosableByViewers))
                        {
                            foreach (var profileAlias in profile.Aliases)
                            {
                                if (profileAlias.Alias.Equals(chatCommand.ArgumentsAsString, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (CameraConfigModel.GameCameraProfile == profile)
                                    {
                                        // TODO: find out why it sometimes doesn't work, do i save it but the game ignores it?
                                        profile.CameraPlusConfig.SaveToBeatSaber(MainViewModel.AppConfigModel);
                                        return;
                                    }

                                    profile.CameraPlusConfig.SaveToBeatSaber(MainViewModel.AppConfigModel);
                                    CameraConfigModel.GameCameraProfile = profile;
                                    await twitchBot.SendMessage(chatCommand.ChatMessage.Channel, $"Switching camera to '{profileAlias.Alias}' - {profile.Name}");
                                    return;
                                }
                            }
                        }
                    },
                    false
                ));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.TwitchBot.Start();
            //var synthesizer = new SpeechSynthesizer();
            //foreach (var voice in synthesizer.GetInstalledVoices())
            //{
            //    synthesizer.Volume = 100;  // 0...100
            //    synthesizer.Rate = 0;     // -10...10
            //    Console.WriteLine($"Voice: {voice.VoiceInfo.Name}, Age: {voice.VoiceInfo.Age}, Gender: {voice.VoiceInfo.Gender}, Desc: {voice.VoiceInfo.Description}");
            //    synthesizer.SelectVoice(voice.VoiceInfo.Name);
            //    //synthesizer.
            //    //var pb = new PromptBuilder();

            //    //pb.StartVoice(VoiceGender.Female, VoiceAge.Senior);
            //    //pb.AppendText("Hello");
            //    //pb.EndVoice();
            //    //pb.StartVoice(VoiceGender.Female, VoiceAge.Child);
            //    //pb.AppendText("Hello");
            //    //pb.EndVoice();
            //    //synthesizer.
            //    var p = synthesizer.SpeakAsync("Hi, this is a test text");
            //    synthesizer = new SpeechSynthesizer();
            //}
        }
        private async void _speechToTextModule_SpeechRecognized(object sender, System.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            if (!MainViewModel.TwitchBot.IsConnected)
            {
                return;
            }
            //Console.WriteLine($"Recognized '{e.Result.Text}' with a confidence of {e.Result.Confidence:P}");

            await MainViewModel.TwitchBot.SendMessage(MainViewModel.TwitchBotConfigModel.Channel, $"{e.Result.Text} ({e.Result.Confidence:P})");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var oauth = new OAuth();
            if (oauth.ShowDialog() ?? false)
            {
                MainViewModel.TwitchBotConfigModel.OAuthAccessToken = oauth.OAuthAccessToken;
                if (MainViewModel.TwitchBotConfigModel.Channel.IsNullOrEmpty())
                {
                    MainViewModel.TwitchBotConfigModel.Channel = oauth.OAuthAccessToken.UserName;
                }
            }
            (Parent as Window)?.Activate();
        }

        private async void Button2_Click(object sender, RoutedEventArgs e)
        {
            await MainViewModel.TwitchBot.Stop();
        }
    }
}