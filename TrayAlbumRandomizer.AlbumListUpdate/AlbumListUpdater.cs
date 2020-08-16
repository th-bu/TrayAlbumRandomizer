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

    public class AlbumListUpdater : IDisposable
    {
        private const string CredentialsFileName = "credentials.json";
        private readonly string albumListFileName;
        private readonly SpotifyAuthorization spotifyAuthorization;

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

            this.spotifyAuthorization = new SpotifyAuthorization(clientId, clientSecret, CredentialsFileName);

            this.albumListFileName = albumListFileName;
        }

        public async Task UpdateAlbumList()
        {
            try
            {
                if (File.Exists(CredentialsFileName))
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

        private async Task StartUpdate()
        {
            var authenticator = this.spotifyAuthorization.GetAuthenticator();
            var spotifyClient = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator));
            var paginator = new SimplePaginatorWithDelay(100);

            var albumsFromSpotify = await spotifyClient.PaginateAll(await spotifyClient.Library.GetAlbums().ConfigureAwait(false), paginator);

            var savableAlbums = albumsFromSpotify.Select(a =>
                new SavableAlbum { Artist = a.Album.Artists.FirstOrDefault()?.Name, Album = a.Album.Name, Id = a.Album.Id }).ToArray();

            File.WriteAllText(this.albumListFileName, JsonConvert.SerializeObject(savableAlbums));
        }

        public void Dispose()
        {
            this.spotifyAuthorization.Dispose();
        }
    }
}
