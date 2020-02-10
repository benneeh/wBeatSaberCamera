using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Speech.Recognition;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using wBeatSaberCamera.Models;
using wBeatSaberCamera.Models.FrankerFaceZModels;
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
        private readonly Dictionary<string, Task<string[]>> _emoteCache = new Dictionary<string, Task<string[]>>();
        private static readonly HttpClient s_httpClient = new HttpClient();

        public TwitchBotConfigView()
        {
            InitializeComponent();
            MainViewModel.TwitchBot = new TwitchBot(MainViewModel.ChatViewModel, MainViewModel.TwitchBotConfigModel);
            MainViewModel.SpeechToEmojiModule = new SpeechToTextModule();
            MainViewModel.SpeechToTextModule = new SpeechToTextModule()
            {
                GrammarLoader = () => Task.FromResult((Grammar)new DictationGrammar())
            };

            MainViewModel.SpeechToTextModule.SpeechRecognized += _speechToTextModule_SpeechRecognized;
            MainViewModel.SpeechToEmojiModule.GrammarLoader = async () =>
            {
                var choices = new Choices();
                if (!_emoteCache.ContainsKey(MainViewModel.TwitchBotConfigModel.Channel))
                {
                    _emoteCache.Add(MainViewModel.TwitchBotConfigModel.Channel, Task.Run(GetChannelEmotes));
                }

                choices.Add(await _emoteCache[MainViewModel.TwitchBotConfigModel.Channel]);

                var keyWordsGrammarBuilder = new GrammarBuilder(choices);

                var keyWordsGrammar = new Grammar(keyWordsGrammarBuilder);
                return keyWordsGrammar;
            };
            MainViewModel.SpeechToEmojiModule.SpeechRecognized += _speechToEmojiModule_SpeechRecognized;

            MainViewModel.TwitchBotConfigModel.PropertyChanged += BotConfigModelPropertyChanged;
            MainViewModel.ChatViewModel.PropertyChanged += ChatConfigModel_PropertyChanged;

            if (MainViewModel.ChatViewModel.IsSpeechToTextEnabled)
            {
                MainViewModel.SpeechToTextModule.Start();
            }
            if (MainViewModel.ChatViewModel.IsSpeechEmojiEnabled)
            {
                MainViewModel.SpeechToEmojiModule.Start();
            }


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
                            foreach (var profile in MainViewModel.CameraConfigModel.Profiles.Where(x => x.IsChoosableByViewers && x.Aliases.Count > 0))
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

        async Task<string[]> GetChannelEmotes()
        {
            // https://api.betterttv.net/2/channels/benneeeh
            // https://api.frankerfacez.com/v1/room/benneeeh

            var t1 = Task.Run(async () =>
            {
                try
                {
                    var resultString = await s_httpClient.GetStringAsync($"https://api.betterttv.net/2/channels/{MainViewModel.TwitchBotConfigModel.Channel}");
                    var anon = new
                    {
                        emotes = new[]
                        {
                            new {code=""}
                        }
                    };
                    anon = JsonConvert.DeserializeAnonymousType(resultString, anon);
                    return anon.emotes.Select(x => x.code).ToArray();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return new string[0];
                }
            });
            var t2 = Task.Run(async () =>
            {
                try
                {
                    var resultString = await s_httpClient.GetStringAsync($"https://api.frankerfacez.com/v1/room/{MainViewModel.TwitchBotConfigModel.Channel}");
                    var result = JsonConvert.DeserializeObject<FfzRoot>(resultString);
                    return result.Sets[result.Room.Set].Emoticons.Where(x => !x.Hidden).Select(x => x.Name).ToArray();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return new string[0];
                }
            });

            return (await Task.WhenAll(t1, t2)).SelectMany(x => x).ToArray();
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

        private void ChatConfigModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.ChatViewModel.IsSpeechEmojiEnabled))
            {
                if (MainViewModel.ChatViewModel.IsSpeechEmojiEnabled)
                {
                    MainViewModel.SpeechToEmojiModule.Start();
                }
                else
                {
                    MainViewModel.SpeechToEmojiModule.Stop();
                }
            }

            if (e.PropertyName == nameof(MainViewModel.ChatViewModel.IsSpeechToTextEnabled))
            {
                if (MainViewModel.ChatViewModel.IsSpeechToTextEnabled)
                {
                    MainViewModel.SpeechToTextModule.Start();
                }
                else
                {
                    MainViewModel.SpeechToTextModule.Stop();
                }
            }
        }

        private void BotConfigModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.TwitchBotConfigModel.Channel))
            {
                if (MainViewModel.ChatViewModel.IsSpeechEmojiEnabled)
                {
                    MainViewModel.SpeechToEmojiModule.Stop();
                    MainViewModel.SpeechToEmojiModule.Start();
                }
            }
        }

        private async void _speechToTextModule_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (!MainViewModel.TwitchBot.IsConnected)
            {
                return;
            }

            await MainViewModel.TwitchBot.SendMessage(MainViewModel.TwitchBotConfigModel.Channel, $"{e.Result.Text}", true);
        }

        private async void _speechToEmojiModule_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
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