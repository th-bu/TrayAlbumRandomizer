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
    using TrayAlbumRandomizer.TrackBlacklisting;

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
                await Console.Error.WriteLineAsync(exception.Message);
            }
        }

        public void Dispose()
        {
            this.spotifyAuthorization.Dispose();
        }

        private async Task StartPlaylistGeneration()
        {
            const int tracksPerPlaylist = 9900;

            var paginator = new SimplePaginatorWithDelay(100);

            IAuthenticator authenticator = this.spotifyAuthorization.GetAuthenticator();
            var spotifyClient = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator));
            PrivateUser profile = await spotifyClient.UserProfile.Current();

            List<SavableAlbum> albums = GetAlbums().OrderBy(a => a.Artist).ThenBy(a => a.Album).ToList();

            List<AlbumWithTracks> cachedAlbums = GetCachedAlbums();

            var trackUris = new List<string>();
            int counter = 0;

            IEnumerable<AlbumWithTracks> cachedAlbumsInLibrary = cachedAlbums.Where(ca => albums.Select(a => a.Id).Contains(ca.Id));
            trackUris.AddRange(cachedAlbumsInLibrary.SelectMany(ca => ca.TrackUris));

            List<SavableAlbum> albumsNotInCache = albums.Where(a => cachedAlbums.All(ca => ca.Id != a.Id)).ToList();

            foreach (SavableAlbum album in albumsNotInCache)
            {
                Console.WriteLine($"Processing {++counter}/{albumsNotInCache.Count}: {album.Artist} - {album.Album}");
                List<string> albumTrackUris = (await spotifyClient.PaginateAll(await spotifyClient.Albums.GetTracks(album.Id).ConfigureAwait(false), paginator)).Select(at => at.Uri).ToList();
                trackUris.AddRange(albumTrackUris);

                cachedAlbums.Add(new AlbumWithTracks{ Id = album.Id, TrackUris = albumTrackUris });

                Thread.Sleep(100);
            }

            SaveCachedAlbums(cachedAlbums);

            int playlistCounter = 1;

            trackUris = trackUris.Except(TrackBlacklistHelper.GetBlacklist()).ToList();

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
            IList<SimplePlaylist> playlists = await spotifyClient.PaginateAll(await spotifyClient.Playlists.GetUsers(profile.Id).ConfigureAwait(false), paginator);
            SimplePlaylist playlist = playlists.FirstOrDefault(pl => pl.Name == playlistName);

            FullPlaylist createdPlaylist = null;

            if (playlist == null)
            {
                createdPlaylist = await spotifyClient.Playlists.Create(profile.Id, new PlaylistCreateRequest(playlistName));
            }

            string playlistId = playlist?.Id ?? createdPlaylist.Id;
            return playlistId;
        }

        private static IEnumerable<SavableAlbum> GetAlbums()
        {
            if (!File.Exists(Constants.AlbumListFileName))
            {
                return Array.Empty<SavableAlbum>();
            }

            string json = File.ReadAllText(Constants.AlbumListFileName);
            return JsonConvert.DeserializeObject<SavableAlbum[]>(json);
        }

        private static List<AlbumWithTracks> GetCachedAlbums()
        {
            if (!File.Exists(Constants.TracksCacheFileName))
            {
                return new List<AlbumWithTracks>();
            }

            string json = File.ReadAllText(Constants.TracksCacheFileName);
            return JsonConvert.DeserializeObject<List<AlbumWithTracks>>(json);
        }

        private static void SaveCachedAlbums(List<AlbumWithTracks> albums)
        {
            File.WriteAllText(Constants.TracksCacheFileName, JsonConvert.SerializeObject(albums));
        }
    }
}
