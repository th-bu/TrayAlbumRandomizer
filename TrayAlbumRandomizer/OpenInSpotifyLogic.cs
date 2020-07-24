namespace TrayAlbumRandomizer
{
    using System;
    using System.Diagnostics;
    using TrayAlbumRandomizer.Interfaces;
    using TrayAlbumRandomizer.Pocos;

    public class OpenInSpotifyLogic : IOpenInPlayerLogic
    {
        private readonly SavableAlbum[] _albums;
        private Random _random = new Random();

        public OpenInSpotifyLogic(SavableAlbum[] albums)
        {
            _albums = albums;
        }

        public void Open(bool openInBrowser, string browserPath = null)
        {
            if (!openInBrowser)
            {
                Process.Start("spotify:album:" + GetRandomAlbumId());
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(browserPath))
                {
                    Process.Start(browserPath, "https://open.spotify.com/album/" + GetRandomAlbumId());
                }
                else
                {
                    Process.Start("https://open.spotify.com/album/" + GetRandomAlbumId());
                }
            }
        }

        private string GetRandomAlbumId()
        {
            return _albums[_random.Next(_albums.Length)].Id;
        }
    }
}
