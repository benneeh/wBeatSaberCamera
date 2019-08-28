using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using wBeatSaberCamera.Annotations;

namespace wBeatSaberCamera.Utils.OAuth
{
    /// <summary>
    /// Interaction logic for OAuth.xaml
    /// </summary>
    public partial class OAuth : Window
    {
        // client configuration
        private const string ClientId = "ijyc8kmvhaoa1wtfz9ys90a37u3wr2";

        private const string AuthorizationEndpoint = "https://id.twitch.tv/oauth2/authorize";
        private const string UserInfoEndpoint = "https://id.twitch.tv/oauth2/validate";

        public OAuth()
        {
            InitializeComponent();
        }

        public string AccessToken
        {
            [PublicAPI]
            get;
            set;
        }

        public string TokenType
        {
            [PublicAPI]
            get;
            set;
        }

        public string UserName
        {
            [PublicAPI]
            get;
            set;
        }

        public string[] Scopes
        {
            [PublicAPI]
            get;
            set;
        }

        public int UserId
        {
            [PublicAPI]
            get;
            set;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitiateOAuth();
        }

        private async void InitiateOAuth()
        {
            // Generates state and PKCE values.
            string state = RandomDataBase64Url(32);

            // Creates a redirect URI using an available port on the loopback address.
            string redirectUri = $"http://localhost:50393/";
            Output("redirect URI: " + redirectUri);

            // Creates an HttpListener to listen for requests on that redirect URI.
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add(redirectUri);
            Output("Listening..");
            httpListener.Start();
            var scopes = new[]
            {
                "openid",
                "channel_subscriptions",
                "chat:read",
                "chat:edit",
                "whispers:read",
                "whispers:edit",
                //"channel:read:subscriptions",
                //"user:read:broadcast",
                //"channel_check_subscription",
                //"channel_feed_read",
                //"channel_read",
                //"channel_subscriptions",
                //"user_blocks_edit"
            };

            // Creates the OAuth 2.0 authorization request.
            var @params = new Dictionary<string, string>()
            {
                {"client_id", ClientId},
                {"redirect_uri", Uri.EscapeDataString(redirectUri)},
                {"response_type", "token"},
                {"scope", string.Join("+", scopes)},
                {"state", state},
                {"force_verify", "true"}
            };
            string authorizationRequest = $"{AuthorizationEndpoint}?{string.Join("&", @params.Select(x => $"{x.Key}={x.Value}"))}";

            // Opens request in the browser.
            Process.Start(authorizationRequest);

            // Waits for the OAuth authorization response.

            // Brings this app back to the foreground.
            Activate();
            async Task<HttpListenerRequest> HandleIncomingRequest(HttpListener listener)
            {
                var httpListenerContext = await listener.GetContextAsync();

                // Sends an HTTP response to the browser.
                var htmlResource = Assembly.GetExecutingAssembly().GetManifestResourceStream("wBeatSaberCamera.Utils.OAuth.RedirectTarget.html");

                httpListenerContext.Response.ContentType = "text/html";
                if (htmlResource == null)
                {
                    throw new InvalidOperationException("RedirectTarget.html resource could not be found, wtf?");
                }

                httpListenerContext.Response.ContentLength64 = htmlResource.Length;
                var responseOutputStream = httpListenerContext.Response.OutputStream;
                var responseTask = htmlResource.CopyToAsync(responseOutputStream);

                if (!httpListenerContext.Request.QueryString.HasKeys())
                {
                    await responseTask.ContinueWith((_) =>
                    {
                        responseOutputStream.Close();
                        Output("Implicit grant flow received, redirecting to get code...");
                    });
                    return await HandleIncomingRequest(listener);
                }
                await responseTask.ContinueWith((_) =>
                {
                    responseOutputStream.Close();
                    listener.Close();
                    Output("Implicit grant flow finished");
                });
                return httpListenerContext.Request;
            }

            var clientRequest = await HandleIncomingRequest(httpListener);
            Activate();

            // Checks for errors.
            if (clientRequest.QueryString.Get("error") != null)
            {
                Output($"OAuth authorization error: {clientRequest.QueryString.Get("error")}.");
                DialogResult = false;
                return;
            }
            if (clientRequest.QueryString.Get("access_token") == null
                || clientRequest.QueryString.Get("token_type") == null
                || clientRequest.QueryString.Get("state") == null)
            {
                Output("Malformed authorization response. " + clientRequest.QueryString);
                DialogResult = false;
                return;
            }

            // extracts the code
            var accessToken = clientRequest.QueryString.Get("access_token");
            var tokenType = clientRequest.QueryString.Get("token_type");
            var incomingState = clientRequest.QueryString.Get("state");

            // Compares the received state to the expected value, to ensure that this app made the
            // request which resulted in authorization.
            if (incomingState != state)
            {
                Output($"Received request with invalid state ({incomingState})");
                DialogResult = false;
                return;
            }
            Output("Access token: " + accessToken);

            Activate();
            //// Starts the code exchange at the Token Endpoint.
            //PerformCodeExchange(code, codeVerifier, redirectUri);
            await UserInfoCall(accessToken, tokenType == "bearer" ? "OAuth" : tokenType);

            DialogResult = true;
        }

        private async Task UserInfoCall(string accessToken, string tokenType)
        {
            Output("Making API Call to userInfo...");

            // sends the request
            HttpWebRequest userInfoRequest = (HttpWebRequest)WebRequest.Create(UserInfoEndpoint);
            userInfoRequest.Method = "GET";
            userInfoRequest.Headers.Add($"Authorization: {tokenType} {accessToken}");
            userInfoRequest.ContentType = "application/x-www-form-urlencoded";
            userInfoRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

            // gets the response
            WebResponse userInfoResponse = await userInfoRequest.GetResponseAsync();
            using (StreamReader userInfoResponseReader = new StreamReader(userInfoResponse.GetResponseStream() ?? throw new InvalidOperationException()))
            {
                // reads response body
                string userInfoResponseText = await userInfoResponseReader.ReadToEndAsync();
                var userInfo = JsonConvert.DeserializeAnonymousType(userInfoResponseText, new
                {
                    client_id = "",
                    login = "",
                    scopes = new string[] { },
                    user_id = 0
                });
                AccessToken = accessToken;
                TokenType = tokenType;
                UserName = userInfo.login;
                UserId = userInfo.user_id;
                Scopes = userInfo.scopes;
                Output(userInfoResponseText);
            }
        }

        /// <summary>
        /// Appends the given string to the on-screen log, and the debug console.
        /// </summary>
        /// <param name="output">string to be appended</param>
        private void Output(string output)
        {
            Dispatcher?.Invoke(() => TextBoxOutput.AppendText(output + Environment.NewLine));
            Console.WriteLine(output);
        }

        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        private static string RandomDataBase64Url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return Base64UrlEncodeNoPadding(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static string Base64UrlEncodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }
    }
}