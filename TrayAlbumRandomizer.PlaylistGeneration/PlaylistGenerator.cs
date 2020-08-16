﻿namespace TrayAlbumRandomizer.PlaylistGeneration
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

    public class PlaylistGenerator
    {
        private const string CredentialsFileName = "credentials.json";
        private const string TracksCacheFileName = "trackscache.json";
        const string PlaylistName = "Randomizer #{0} (auto generated)";
        const int TracksPerRequest = 100;
        const int TracksPerPlaylist = 9500;

        private readonly string _albumListFileName;
        private readonly SpotifyAuthorization _spotifyAuthorization;

        public PlaylistGenerator(string albumListFileName)
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

        public async Task GeneratePlaylist()
        {
            try
            {
                if (File.Exists(CredentialsFileName))
                {
                    await StartPlaylistGeneration();
                }
                else
                {
                    await _spotifyAuthorization.StartAuthorization();
                    while (!_spotifyAuthorization.IsAuthorizationFinished)
                    {
                        // Todo: Exit condition in case the authorization is not successful
                        Thread.Sleep(1000);
                    }

                    await StartPlaylistGeneration();
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
        }

        private async Task StartPlaylistGeneration()
        {
            var paginator = new SimplePaginatorWithDelay(100);

            var authenticator = _spotifyAuthorization.GetAuthenticator();
            var spotifyClient = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator));
            var profile = await spotifyClient.UserProfile.Current();

            var albums = GetAlbums(_albumListFileName).OrderBy(a => a.Artist).ThenBy(a => a.Album).ToList();

            var cachedAlbums = GetCachedAlbums();

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

            SaveCachedAlbums(cachedAlbums);

            int playlistCounter = 1;
            while(trackUris.Any())
            {
                string playlistId = await GetPlaylistId(string.Format(PlaylistName, playlistCounter++), paginator, spotifyClient, profile).ConfigureAwait(false);
                await AddTracksToPlaylist(spotifyClient, playlistId, trackUris.Take(TracksPerPlaylist).ToList());
                trackUris.RemoveRange(0, Math.Min(TracksPerPlaylist, trackUris.Count));
            }
        }

        private static async Task AddTracksToPlaylist(SpotifyClient spotifyClient, string playlistId, List<string> trackUris)
        {
            await spotifyClient.Playlists.ReplaceItems(playlistId, new PlaylistReplaceItemsRequest(new List<string>()));
            int trackCount = trackUris.Count;
            while (trackUris.Any())
            {
                Console.WriteLine($"Tracks processing for playlist: {trackCount - trackUris.Count}");
                await spotifyClient.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(trackUris.Take(TracksPerRequest).ToArray()));
                trackUris.RemoveRange(0, Math.Min(TracksPerRequest, trackUris.Count));
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

        private SavableAlbum[] GetAlbums(string albumsPath)
        {
            if (!File.Exists(albumsPath))
            {
                return new SavableAlbum[0];
            }

            var json = File.ReadAllText(albumsPath);
            return JsonConvert.DeserializeObject<SavableAlbum[]>(json);
        }

        private List<AlbumWithTracks> GetCachedAlbums()
        {
            if (!File.Exists(TracksCacheFileName))
            {
                return new List<AlbumWithTracks>();
            }

            var json = File.ReadAllText(TracksCacheFileName);
            return JsonConvert.DeserializeObject<List<AlbumWithTracks>>(json);
        }

        private void SaveCachedAlbums(List<AlbumWithTracks> albums)
        {
            File.WriteAllText(TracksCacheFileName, JsonConvert.SerializeObject(albums));
        }
    }
}