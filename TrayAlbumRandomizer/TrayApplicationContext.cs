namespace TrayAlbumRandomizer
{
    using System;
    using System.Configuration;
    using System.Windows.Forms;
    using TrayAlbumRandomizer.AlbumListUpdate;
    using TrayAlbumRandomizer.Interfaces;
    using TrayAlbumRandomizer.Properties;

    /// <see>
    /// https://stackoverflow.com/a/10250051/869120
    /// </see>
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private SavableAlbum[] _albums;
        private readonly IOpenInPlayerLogic _openInPlayerLogic;
        private readonly bool _openInBrowser = false;
        private readonly string _browserPath = string.Empty;
        private readonly string _albumListFileName = "albums.json";

        public TrayApplicationContext()
        {
            // Initialize Tray Icon
            _trayIcon = new NotifyIcon()
            {
                Icon = Resources.random_album_icon,
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("Open random album", OpenRandomAlbumClicked),
                    new MenuItem("Update album list", UpdateAlbumListClicked),
                    new MenuItem("-"),
                    new MenuItem("Exit", Exit) }),
                Visible = true
            };

            _trayIcon.DoubleClick += TrayIconDoubleClick;

            _openInBrowser = Boolean.Parse(ConfigurationManager.AppSettings["OpenInBrowser"]);
            _browserPath = ConfigurationManager.AppSettings["BrowserPath"];
            _albumListFileName = ConfigurationManager.AppSettings["AlbumListFileName"] ?? _albumListFileName;

            var albumsReader = new AlbumListReader();
            _albums = albumsReader.GetAlbums("albums.json");

            _openInPlayerLogic = new OpenInSpotifyLogic(_albums);
        }

        private void OpenRandomAlbum()
        {
            _openInPlayerLogic.Open(_openInBrowser, _browserPath);
        }

        private void UpdateAlbumList()
        {
            var albumListUpdater = new AlbumListUpdater(_albumListFileName);
            albumListUpdater.AlbumListUpdateFinished += AlbumListUpdateFinished;
            albumListUpdater.UpdateError += UpdateError;

            albumListUpdater.UpdateAlbumList().Wait();
        }

        private void AlbumListUpdateFinished(object sender, AlbumListUpdateFinishedEventArgs e)
        {
            _albums = e.Albums;

            var albumListUpdater = sender as AlbumListUpdater;

            albumListUpdater.AlbumListUpdateFinished -= AlbumListUpdateFinished;
            albumListUpdater.UpdateError -= UpdateError;

            MessageBox.Show("Done updating the albums in the user library.", "Update done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateError(object sender, UpdateErrorEventArgs e)
        {
            var albumListUpdater = sender as AlbumListUpdater;

            albumListUpdater.AlbumListUpdateFinished -= AlbumListUpdateFinished;
            albumListUpdater.UpdateError -= UpdateError;

            MessageBox.Show(e.ErrorMessage, "Error during update", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void TrayIconDoubleClick(object sender, EventArgs e)
        {
            OpenRandomAlbum();
        }

        private void OpenRandomAlbumClicked(object sender, EventArgs e)
        {
            OpenRandomAlbum();
        }

        private void UpdateAlbumListClicked(object sender, EventArgs e)
        {
            UpdateAlbumList();
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;

            Application.Exit();
        }
    }
}
