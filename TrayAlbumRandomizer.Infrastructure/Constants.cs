namespace TrayAlbumRandomizer.Infrastructure
{
    public static class Constants
    {
        public const string CredentialsFileName = "credentials.json";
        public const string TracksCacheFileName = "trackscache.json";
        public const string PlaylistName = "Randomizer #{0} (auto generated)";
        public const string AlbumListFileName = "albums.json";
        public const string CallbackUri = "http://127.0.0.1:5000/callback";
        public const int CallbackPort = 5000;
        public const string SpotifyClientIdSettingsName = "SpotifyClientId";
        public const string SpotifyClientSecretSettingsName = "SpotifyClientSecret";
        public const string TrackBlacklistFileName = "trackblacklist.json";
    }
}
