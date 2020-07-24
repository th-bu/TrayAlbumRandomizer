namespace TrayAlbumRandomizer
{
    using System;
    using System.Configuration;
    using System.Windows.Forms;
    using TrayAlbumRandomizer.Interfaces;
    using TrayAlbumRandomizer.Pocos;
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


        public TrayApplicationContext()
        {
            // Initialize Tray Icon
            _trayIcon = new NotifyIcon()
            {
                Icon = Resources.random_album_icon,
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("Open random album", OpenRandomAlbumClicked),
                    new MenuItem("-"),
                    new MenuItem("Exit", Exit) }),
                Visible = true
            };

            _trayIcon.DoubleClick += TrayIconDoubleClick;

            _openInBrowser = Boolean.Parse(ConfigurationManager.AppSettings["OpenInBrowser"]);
            _browserPath = ConfigurationManager.AppSettings["BrowserPath"];

            var albumsReader = new AlbumListReader();
            _albums = albumsReader.GetAlbums("albums.json");

            _openInPlayerLogic = new OpenInSpotifyLogic(_albums);
        }

        private void OpenRandomAlbum()
        {
            _openInPlayerLogic.Open(_openInBrowser, _browserPath);
        }

        private void TrayIconDoubleClick(object sender, EventArgs e)
        {
            OpenRandomAlbum();
        }

        private void OpenRandomAlbumClicked(object sender, EventArgs e)
        {
            OpenRandomAlbum();
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;

            Application.Exit();
        }
    }
}
