namespace TrayAlbumRandomizer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using TrayAlbumRandomizer.AlbumListUpdate;

    internal class OpenInSpotifyLogic
    {
        private readonly Random random = new Random();
        private List<SavableAlbum> shuffledAlbums;

        public SavableAlbum[] Albums { get; set; }
        public NextMode NextMode { get; set; }

        public void Open(bool openInBrowser, string browserPath = null)
        {
            if (!openInBrowser)
            {
                Process.Start("spotify:album:" + this.GetNextAlbumId());
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(browserPath))
                {
                    Process.Start(browserPath, "https://open.spotify.com/album/" + this.GetNextAlbumId());
                }
                else
                {
                    Process.Start("https://open.spotify.com/album/" + this.GetNextAlbumId());
                }
            }
        }

        public void ShuffleAlbums()
        {
            this.shuffledAlbums = this.Albums.OrderBy(a => this.random.Next(int.MaxValue)).ToList();
        }

        private string GetNextAlbumId()
        {
            if (this.NextMode == NextMode.Random)
            {
                return this.Albums[this.random.Next(this.Albums.Length)].Id;
            }

            if (this.Albums.Length <= 0)
            {
                return string.Empty;
            }

            if (this.shuffledAlbums.Count == 0)
            {
                this.ShuffleAlbums();
            }

            string albumId = this.shuffledAlbums[0].Id;
            this.shuffledAlbums.RemoveAt(0);

            return albumId;
        }
    }
}
