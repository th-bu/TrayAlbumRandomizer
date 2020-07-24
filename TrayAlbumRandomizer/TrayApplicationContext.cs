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
        private readonly SavableAlbum[] _albums;
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
            try
            {
                AlbumListUpdater updater = new AlbumListUpdater(_albumListFileName);
                updater.UpdateAlbumList().Wait();

                MessageBox.Show("Done fetching the albums in the user library.", "Fetching done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch(Exception exception)
            {
                MessageBox.Show(exception.Message, "Error during update", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
