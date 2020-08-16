namespace TrayAlbumRandomizer.PlaylistGeneration
{
    using Newtonsoft.Json;
    using SpotifyAPI.Web;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using TrayAlbumRandomizer.AlbumListUpdate;
    using TrayAlbumRandomizer.Authorization;
    using TrayAlbumRandomizer.Infrastructure;

    public class PlaylistGenerator : IDisposable
    {
        private readonly SpotifyAuthorization spotifyAuthorization;

        public PlaylistGenerator()
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

        public async Task GeneratePlaylist()
        {
            try
            {
                if (File.Exists(Constants.CredentialsFileName))
                {
                    await this.StartPlaylistGeneration();
                }
                else
                {
                    await this.spotifyAuthorization.StartAuthorization();
                    while (!this.spotifyAuthorization.IsAuthorizationFinished)
                    {
                        // Todo: Exit condition in case the authorization is not successful
                        Thread.Sleep(1000);
                    }

                    await this.StartPlaylistGeneration();
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

        private async Task StartPlaylistGeneration()
        {
            const int tracksPerPlaylist = 9500;

            var paginator = new SimplePaginatorWithDelay(100);

            var authenticator = this.spotifyAuthorization.GetAuthenticator();
            var spotifyClient = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator));
            var profile = await spotifyClient.UserProfile.Current();

            var albums = this.GetAlbums().OrderBy(a => a.Artist).ThenBy(a => a.Album).ToList();

            var cachedAlbums = this.GetCachedAlbums();

            List<string> trackUris = new List<string>();
            int counter = 0;

            var cachedAlbumsInLibrary = cachedAlbums.Where(ca => albums.Select(a => a.Id).Contains(ca.Id));
            trackUris.AddRange(cachedAlbumsInLibrary.SelectMany(ca => ca.TrackUris));

            var albumsNotInCache = albums.Where(a => !cachedAlbums.Any(ca => ca.Id == a.Id)).ToList();

            foreach (var album in albumsNotInCache)
            {
                Console.WriteLine($"Processing {++counter}/{albumsNotInCache.Count}: {album.Artist} - {album.Album}");
                var albumTrackUris = (await spotifyClient.PaginateAll(await spotifyClient.Albums.GetTracks(album.Id).ConfigureAwait(false), paginator)).Select(at => at.Uri).ToList();
                trackUris.AddRange(albumTrackUris);

                cachedAlbums.Add(new AlbumWithTracks{ Id = album.Id, TrackUris = albumTrackUris });

                Thread.Sleep(100);
            }

            this.SaveCachedAlbums(cachedAlbums);

            int playlistCounter = 1;
            while(trackUris.Any())
            {
                string playlistId = await GetPlaylistId(string.Format(Constants.PlaylistName, playlistCounter++), paginator, spotifyClient, profile).ConfigureAwait(false);
                await AddTracksToPlaylist(spotifyClient, playlistId, trackUris.Take(tracksPerPlaylist).ToList());
                trackUris.RemoveRange(0, Math.Min(tracksPerPlaylist, trackUris.Count));
            }
        }

        private static async Task AddTracksToPlaylist(SpotifyClient spotifyClient, string playlistId, List<string> trackUris)
        {
            const int tracksPerRequest = 100;

            await spotifyClient.Playlists.ReplaceItems(playlistId, new PlaylistReplaceItemsRequest(new List<string>()));
            int trackCount = trackUris.Count;
            while (trackUris.Any())
            {
                Console.WriteLine($"Tracks processing for playlist: {trackCount - trackUris.Count}");
                await spotifyClient.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(trackUris.Take(tracksPerRequest).ToArray()));
                trackUris.RemoveRange(0, Math.Min(tracksPerRequest, trackUris.Count));
                Thread.Sleep(100);
            }
        }

        private static async Task<string> GetPlaylistId(string playlistName, SimplePaginatorWithDelay paginator, SpotifyClient spotifyClient, PrivateUser profile)
        {
            var playlists = await spotifyClient.PaginateAll(await spotifyClient.Playlists.GetUsers(profile.Id).ConfigureAwait(false), paginator);
            var playlist = playlists.FirstOrDefault(pl => pl.Name == playlistName);

            FullPlaylist createdPlaylist = null;

            if (playlist == null)
            {
                createdPlaylist = await spotifyClient.Playlists.Create(profile.Id, new PlaylistCreateRequest(playlistName));
            }

            string playlistId = playlist?.Id ?? createdPlaylist?.Id;
            return playlistId;
        }

        private SavableAlbum[] GetAlbums()
        {
            if (!File.Exists(Constants.AlbumListFileName))
            {
                return new SavableAlbum[0];
            }

            var json = File.ReadAllText(Constants.AlbumListFileName);
            return JsonConvert.DeserializeObject<SavableAlbum[]>(json);
        }

        private List<AlbumWithTracks> GetCachedAlbums()
        {
            if (!File.Exists(Constants.TracksCacheFileName))
            {
                return new List<AlbumWithTracks>();
            }

            var json = File.ReadAllText(Constants.TracksCacheFileName);
            return JsonConvert.DeserializeObject<List<AlbumWithTracks>>(json);
        }

        private void SaveCachedAlbums(List<AlbumWithTracks> albums)
        {
            File.WriteAllText(Constants.TracksCacheFileName, JsonConvert.SerializeObject(albums));
        }
    }
}
