﻿namespace TrayAlbumRandomizer.AlbumListUpdate
{
    using Newtonsoft.Json;
    using SpotifyAPI.Web;
    using SpotifyAPI.Web.Auth;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using static SpotifyAPI.Web.Scopes;

    public class AlbumListUpdater
    {
        private const string CredentialsFileName = "credentials.json";
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _albumListFileName;
        private readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://127.0.0.1:5000/callback"), 5000);

        public AlbumListUpdater(string albumListFileName)
        {
            _clientId = ConfigurationManager.AppSettings["SpotfiyClientId"];
            if (string.IsNullOrWhiteSpace(_clientId))
            {
                _clientId = Environment.GetEnvironmentVariable("SpotfiyClientId");
            }

            _clientSecret = ConfigurationManager.AppSettings["SpotfiyClientSecret"];
            if (string.IsNullOrWhiteSpace(_clientSecret))
            {
                _clientSecret = Environment.GetEnvironmentVariable("SpotfiyClientSecret");
            }

            _albumListFileName = albumListFileName;
        }

        public async Task UpdateAlbumList()
        {
            if (File.Exists(CredentialsFileName))
            {
                await Start();
            }
            else
            {
                await StartAuthentication();
            }
        }

        private async Task Start()
        {
            var credentialsJson = File.ReadAllText(CredentialsFileName);
            var tokenResponse = JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(credentialsJson);

            var authenticator = new AuthorizationCodeAuthenticator(_clientId, _clientSecret, tokenResponse);
            authenticator.TokenRefreshed += (sender, token) => File.WriteAllText(CredentialsFileName, JsonConvert.SerializeObject(token));

            var clientConfig = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);
            var spotifyClient = new SpotifyClient(clientConfig);
            var paginator = new SimplePaginatorWithDelay(500);

            var albumsFromSpotify = await spotifyClient.PaginateAll(await spotifyClient.Library.GetAlbums().ConfigureAwait(false), paginator);

            var savableAlbums = albumsFromSpotify.Select(a =>
                new SavableAlbum { Artist = a.Album.Artists.FirstOrDefault()?.Name, Album = a.Album.Name, Id = a.Album.Id }).ToList();

            File.WriteAllText(_albumListFileName, JsonConvert.SerializeObject(savableAlbums));
        }

        private async Task StartAuthentication()
        {
            await _server.Start();
            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;

            var loginRequest = new LoginRequest(_server.BaseUri, _clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { UserLibraryRead }
            };

            Uri authorizationUri = loginRequest.ToUri();
            BrowserUtil.Open(authorizationUri);
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();
            AuthorizationCodeTokenResponse tokenResponse = await new OAuthClient().RequestToken(
              new AuthorizationCodeTokenRequest(_clientId, _clientSecret, response.Code, _server.BaseUri)
            );

            File.WriteAllText(CredentialsFileName, JsonConvert.SerializeObject(tokenResponse));
            _server.Dispose();

            await Start();
        }
    }
}
