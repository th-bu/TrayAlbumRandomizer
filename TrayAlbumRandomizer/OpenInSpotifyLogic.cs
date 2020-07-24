namespace TrayAlbumRandomizer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using TrayAlbumRandomizer.AlbumListUpdate;

    internal class OpenInSpotifyLogic
    {
        private Random _random = new Random();
        private List<SavableAlbum> _shuffledAlbums;

        public SavableAlbum[] Albums { get; set; }
        public NextMode NextMode { get; set; }

        public void Open(bool openInBrowser, string browserPath = null)
        {
            if (!openInBrowser)
            {
                Process.Start("spotify:album:" + GetNextAlbumId());
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(browserPath))
                {
                    Process.Start(browserPath, "https://open.spotify.com/album/" + GetNextAlbumId());
                }
                else
                {
                    Process.Start("https://open.spotify.com/album/" + GetNextAlbumId());
                }
            }
        }

        public void ShuffleAlbums()
        {
            _shuffledAlbums = Albums.OrderBy(a => _random.Next(int.MaxValue)).ToList();
        }

        private string GetNextAlbumId()
        {
            if (NextMode == NextMode.Random)
            {
                return Albums[_random.Next(Albums.Length)].Id;
            }

            if (Albums.Length > 0)
            {
                if (_shuffledAlbums.Count == 0)
                {
                    ShuffleAlbums();
                }

                string albumId = _shuffledAlbums[0].Id;
                _shuffledAlbums.RemoveAt(0);

                return albumId;
            }

            return string.Empty;
        }
    }
}
