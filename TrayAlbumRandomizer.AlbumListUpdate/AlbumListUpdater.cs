namespace TrayAlbumRandomizer.AlbumListUpdate
{
    using Newtonsoft.Json;
    using SpotifyAPI.Web;
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using TrayAlbumRandomizer.Authorization;

    public class AlbumListUpdater : IDisposable
    {
        private const string CredentialsFileName = "credentials.json";
        private readonly string _albumListFileName;
        private readonly SpotifyAuthorization _spotifyAuthorization;

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
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
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
        }

        public void Dispose()
        {
            _spotifyAuthorization.Dispose();
        }
    }
}
