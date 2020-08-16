namespace TrayAlbumRandomizer.AlbumListUpdate
{
    using Newtonsoft.Json;
    using SpotifyAPI.Web;
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using TrayAlbumRandomizer.Authorization;
    using TrayAlbumRandomizer.Infrastructure;

    public class AlbumListUpdater : IDisposable
    {
        private readonly SpotifyAuthorization spotifyAuthorization;

        public AlbumListUpdater()
        {
            string clientId = ConfigurationManager.AppSettings[Constants.SpotifyClientIdSettingsName];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                clientId = Environment.GetEnvironmentVariable(Constants.SpotifyClientIdSettingsName);
            }

            string clientSecret = ConfigurationManager.AppSettings[Constants.SpotifyClientSecretSettingsName];
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                clientSecret = Environment.GetEnvironmentVariable(Constants.SpotifyClientSecretSettingsName);
            }

            this.spotifyAuthorization = new SpotifyAuthorization(clientId, clientSecret);
        }

        public async Task UpdateAlbumList()
        {
            try
            {
                if (File.Exists(Constants.CredentialsFileName))
                {
                    await this.StartUpdate();
                }
                else
                {
                    await this.spotifyAuthorization.StartAuthorization();
                    while (!this.spotifyAuthorization.IsAuthorizationFinished)
                    {
                        // Todo: Exit condition in case the authorization is not successful
                        Thread.Sleep(1000);
                    }

                    await this.StartUpdate();
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
        }

        public void Dispose()
        {
            this.spotifyAuthorization.Dispose();
        }

        private async Task StartUpdate()
        {
            var authenticator = this.spotifyAuthorization.GetAuthenticator();
            var spotifyClient = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator));
            var paginator = new SimplePaginatorWithDelay(100);

            var albumsFromSpotify = await spotifyClient.PaginateAll(await spotifyClient.Library.GetAlbums().ConfigureAwait(false), paginator);

            var savableAlbums = albumsFromSpotify.Select(a =>
                new SavableAlbum { Artist = a.Album.Artists.FirstOrDefault()?.Name, Album = a.Album.Name, Id = a.Album.Id }).ToArray();

            File.WriteAllText(Constants.AlbumListFileName, JsonConvert.SerializeObject(savableAlbums));
        }
    }
}
