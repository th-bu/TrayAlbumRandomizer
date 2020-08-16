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
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string credentialsFileName;
        private EmbedIOAuthServer server = null;

        public SpotifyAuthorization(string clientId, string clientSecret, string credentialsFileName)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.credentialsFileName = credentialsFileName;
        }

        public bool IsAuthorizationFinished { get; set; } = false;

        public void Dispose()
        {
            this.server?.Dispose();
        }

        public IAuthenticator GetAuthenticator()
        {
            var credentialsJson = File.ReadAllText(this.credentialsFileName);
            var tokenResponse = JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(credentialsJson);

            var authenticator = new AuthorizationCodeAuthenticator(this.clientId, this.clientSecret, tokenResponse);
            authenticator.TokenRefreshed += (sender, token) => File.WriteAllText(this.credentialsFileName, JsonConvert.SerializeObject(token));

            return authenticator;
        }

        public async Task StartAuthorization()
        {
            this.server = new EmbedIOAuthServer(new Uri("http://127.0.0.1:5000/callback"), 5000);

            await this.server.Start();
            this.server.AuthorizationCodeReceived += this.OnAuthorizationCodeReceived;

            var loginRequest = new LoginRequest(this.server.BaseUri, this.clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { UserLibraryRead, PlaylistModifyPublic }
            };

            BrowserUtil.Open(loginRequest.ToUri());
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await this.server.Stop();
            this.server.AuthorizationCodeReceived -= this.OnAuthorizationCodeReceived;

            AuthorizationCodeTokenResponse tokenResponse = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(this.clientId, this.clientSecret, response.Code, this.server.BaseUri)
            );

            File.WriteAllText(this.credentialsFileName, JsonConvert.SerializeObject(tokenResponse));

            this.IsAuthorizationFinished = true;
        }
    }
}
