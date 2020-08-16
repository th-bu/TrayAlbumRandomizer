namespace TrayAlbumRandomizer.Infrastructure
{
    public static class Constants
    {
        public static readonly string CredentialsFileName = "credentials.json";
        public static readonly string TracksCacheFileName = "trackscache.json";
        public static readonly string PlaylistName = "Randomizer #{0} (auto generated)";
        public static readonly string AlbumListFileName = "albums.json";
        public static readonly string CallbackUri = "http://127.0.0.1:5000/callback";
        public static readonly int CallbackPort = 5000;
        public static readonly string SpotifyClientIdSettingsName = "SpotifyClientId";
        public static readonly string SpotifyClientSecretSettingsName = "SpotifyClientSecret";
        public static readonly string TrackBlacklistFileName = "trackblacklist.json";
    }
}
