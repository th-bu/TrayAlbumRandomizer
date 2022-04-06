namespace TrayAlbumRandomizer
{
    using System;
    using System.Configuration;
    using System.Windows.Forms;
    using TrayAlbumRandomizer.AlbumListUpdate;
    using TrayAlbumRandomizer.Properties;
    using TrayAlbumRandomizer.TrackBlacklisting;
    using TrayAlbumRandomizer.Windows;

    /// <see>
    /// https://stackoverflow.com/a/10250051/869120
    /// </see>
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon trayIcon;
        private SavableAlbum[] albums;
        private readonly OpenInSpotifyLogic openInSpotifyLogic;
        private readonly bool openInBrowser = false;
        private readonly string browserPath = string.Empty;
        private readonly ContextMenu contextMenu;

        public TrayApplicationContext()
        {
            // Initialize Tray Icon
            this.contextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Open next album", this.OpenRandomAlbumClicked),
                new MenuItem("-"),
                new MenuItem("Random", this.OptionRandomClicked),
                new MenuItem("Shuffle", this.OptionShuffleClicked),
                new MenuItem("-"),
                new MenuItem("Update album list", this.UpdateAlbumListClicked),
                new MenuItem("-"),
                new MenuItem("Generate playlist", this.GeneratePlaylistClicked),
                new MenuItem("Add track(s) from clipboard to blacklist", this.AddTracksToBlacklistClicked),
                new MenuItem("-"),
                new MenuItem("Exit", this.Exit) });

            this.trayIcon = new NotifyIcon()
            {
                Icon = Resources.random_album_icon,
                ContextMenu = contextMenu,
                Visible = true
            };

            this.contextMenu.MenuItems[3].Checked = true;

            this.trayIcon.DoubleClick += this.TrayIconDoubleClick;

            this.openInBrowser = bool.Parse(ConfigurationManager.AppSettings["OpenInBrowser"]);
            this.browserPath = ConfigurationManager.AppSettings["BrowserPath"];

            var albumsReader = new AlbumListReader();
            this.albums = albumsReader.GetAlbums();

            this.openInSpotifyLogic = new OpenInSpotifyLogic
            {
                Albums = this.albums,
                NextMode = NextMode.Shuffle
            };

            this.openInSpotifyLogic.ShuffleAlbums();
        }

        private void OpenRandomAlbum()
        {
            this.openInSpotifyLogic.Open(this.openInBrowser, this.browserPath);
        }

        private void UpdateAlbumList()
        {
            OpenCliForm openCliForm = new OpenCliForm("Update album list");

            openCliForm.Show();
            openCliForm.OpenProcess("TrayAlbumRandomizer.Cli.exe", "-u");

            openCliForm.FormClosed += this.OpenCliFormClosed;
        }

        private void OpenCliFormClosed(object sender, FormClosedEventArgs e)
        {
            var albumsReader = new AlbumListReader();
            this.albums = albumsReader.GetAlbums();

            this.openInSpotifyLogic.ShuffleAlbums();

            var openCliForm = sender as OpenCliForm;
            openCliForm.FormClosed -= this.OpenCliFormClosed;
        }

        private void GeneratePlaylist()
        {
            OpenCliForm openCliForm = new OpenCliForm("Playlist generation");

            openCliForm.Show();
            openCliForm.OpenProcess("TrayAlbumRandomizer.Cli.exe", "-g");
        }

        private void TrayIconDoubleClick(object sender, EventArgs e)
        {
            this.OpenRandomAlbum();
        }

        private void OpenRandomAlbumClicked(object sender, EventArgs e)
        {
            this.OpenRandomAlbum();
        }

        private void UpdateAlbumListClicked(object sender, EventArgs e)
        {
            this.UpdateAlbumList();
        }

        private void GeneratePlaylistClicked(object sender, EventArgs e)
        {
            this.GeneratePlaylist();
        }

        private void AddTracksToBlacklistClicked(object sender, EventArgs e)
        {
            var blacklistHelper = new TrackBlacklistHelper();
            var text = Clipboard.GetText();

            if (text.Contains("\n"))
            {
                blacklistHelper.AddRangeToBlacklist(text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                blacklistHelper.AddToBlacklist(text);
            }
        }


        private void OptionRandomClicked(object sender, EventArgs e)
        {
            this.openInSpotifyLogic.NextMode = NextMode.Random;
            this.contextMenu.MenuItems[2].Checked = true;
            this.contextMenu.MenuItems[3].Checked = false;
        }

        private void OptionShuffleClicked(object sender, EventArgs e)
        {
            this.openInSpotifyLogic.NextMode = NextMode.Shuffle;
            this.contextMenu.MenuItems[2].Checked = false;
            this.contextMenu.MenuItems[3].Checked = true;

            this.openInSpotifyLogic.ShuffleAlbums();
        }

        private void Exit(object sender, EventArgs e)
        {
            this.trayIcon.Visible = false;

            Application.Exit();
        }
    }
}
