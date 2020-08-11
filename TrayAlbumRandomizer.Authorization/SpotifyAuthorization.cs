namespace TrayAlbumRandomizer.Authorization
{
    using Newtonsoft.Json;
    using SpotifyAPI.Web;
    using SpotifyAPI.Web.Auth;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using static SpotifyAPI.Web.Scopes;

    public class SpotifyAuthorization : IDisposable
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _credentialsFileName;
        private readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://127.0.0.1:5000/callback"), 5000);
        private Action _runAfterSuccessfulAuthorization;

        public SpotifyAuthorization(string clientId, string clientSecret, string credentialsFileName)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _credentialsFileName = credentialsFileName;
        }

        public void Dispose()
        {
            _server.Dispose();
        }

        public IAuthenticator GetAuthenticator()
        {
            var credentialsJson = File.ReadAllText(_credentialsFileName);
            var tokenResponse = JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(credentialsJson);

            var authenticator = new AuthorizationCodeAuthenticator(_clientId, _clientSecret, tokenResponse);
            authenticator.TokenRefreshed += (sender, token) => File.WriteAllText(_credentialsFileName, JsonConvert.SerializeObject(token));

            return authenticator;
        }

        public async Task StartAuthorization(Action doAfterSuccessfulAuthorization)
        {
            _runAfterSuccessfulAuthorization = doAfterSuccessfulAuthorization;

            await _server.Start();
            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;

            var loginRequest = new LoginRequest(_server.BaseUri, _clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { UserLibraryRead }
            };

            BrowserUtil.Open(loginRequest.ToUri());
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();
            _server.AuthorizationCodeReceived -= OnAuthorizationCodeReceived;

            AuthorizationCodeTokenResponse tokenResponse = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(_clientId, _clientSecret, response.Code, _server.BaseUri)
            );

            File.WriteAllText(_credentialsFileName, JsonConvert.SerializeObject(tokenResponse));

            _runAfterSuccessfulAuthorization();
        }
    }
}
