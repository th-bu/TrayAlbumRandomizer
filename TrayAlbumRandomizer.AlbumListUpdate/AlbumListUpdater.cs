namespace TrayAlbumRandomizer.AlbumListUpdate
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
    using TrayAlbumRandomizer.Authorization;
    using static SpotifyAPI.Web.Scopes;

    public class AlbumListUpdater : IDisposable
    {
        private const string CredentialsFileName = "credentials.json";
        private readonly string _albumListFileName;
        private readonly SpotifyAuthorization _spotifyAuthorization;

        public event EventHandler<AlbumListUpdateFinishedEventArgs> AlbumListUpdateFinished;
        public event EventHandler<UpdateErrorEventArgs> UpdateError;

        public AlbumListUpdater(string albumListFileName)
        {
            string clientId = ConfigurationManager.AppSettings["SpotifyClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                clientId = Environment.GetEnvironmentVariable("SpotifyClientId");
            }

            string clientSecret = ConfigurationManager.AppSettings["SpotifyClientSecret"];
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                clientSecret = Environment.GetEnvironmentVariable("SpotifyClientSecret");
            }

            _spotifyAuthorization = new SpotifyAuthorization(clientId, clientSecret, CredentialsFileName);

            _albumListFileName = albumListFileName;
        }

        public async Task UpdateAlbumList()
        {
            try
            {
                if (File.Exists(CredentialsFileName))
                {
                    await StartUpdate();
                }
                else
                {
                    await _spotifyAuthorization.StartAuthorization(async () => await StartUpdate());
                }
            }
            catch(Exception exception)
            {
                RaiseUpdateError(exception.Message);
            }
        }

        private async Task StartUpdate()
        {
            var authenticator = _spotifyAuthorization.GetAuthenticator();
            var spotifyClient = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator));
            var paginator = new SimplePaginatorWithDelay(500);

            var albumsFromSpotify = await spotifyClient.PaginateAll(await spotifyClient.Library.GetAlbums().ConfigureAwait(false), paginator);

            var savableAlbums = albumsFromSpotify.Select(a =>
                new SavableAlbum { Artist = a.Album.Artists.FirstOrDefault()?.Name, Album = a.Album.Name, Id = a.Album.Id }).ToArray();

            File.WriteAllText(_albumListFileName, JsonConvert.SerializeObject(savableAlbums));

            RaiseAlbumListFinished(savableAlbums);
        }

        private void RaiseAlbumListFinished(SavableAlbum[] albums)
        {
            var eventArgs = new AlbumListUpdateFinishedEventArgs();
            eventArgs.Albums = albums;

            var eventHandler = AlbumListUpdateFinished;
            eventHandler?.Invoke(this, eventArgs);
        }

        private void RaiseUpdateError(string errorMessage)
        {
            var eventArgs = new UpdateErrorEventArgs();
            eventArgs.ErrorMessage = errorMessage;

            var eventHandler = UpdateError;
            eventHandler?.Invoke(this, eventArgs);
        }

        public void Dispose()
        {
            _spotifyAuthorization.Dispose();
        }
    }
}
