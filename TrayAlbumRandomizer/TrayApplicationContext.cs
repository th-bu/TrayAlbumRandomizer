namespace TrayAlbumRandomizer
{
    using System;
    using System.Configuration;
    using System.Windows.Forms;
    using TrayAlbumRandomizer.AlbumListUpdate;
    using TrayAlbumRandomizer.Properties;
    using TrayAlbumRandomizer.Windows;

    /// <see>
    /// https://stackoverflow.com/a/10250051/869120
    /// </see>
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private SavableAlbum[] _albums;
        private readonly OpenInSpotifyLogic _openInSpotifyLogic;
        private readonly bool _openInBrowser = false;
        private readonly string _browserPath = string.Empty;
        private readonly string _albumListFileName = "albums.json";
        private readonly ContextMenu _contextMenu;

        public TrayApplicationContext()
        {
            // Initialize Tray Icon
            _contextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("Open next album", OpenRandomAlbumClicked),
                    new MenuItem("-"),
                    new MenuItem("Random", OptionRandomClicked),
                    new MenuItem("Shuffle", OptionShuffleClicked),
                    new MenuItem("-"),
                    new MenuItem("Update album list", UpdateAlbumListClicked),
                    new MenuItem("-"),
                    new MenuItem("Exit", Exit) });

            _trayIcon = new NotifyIcon()
            {
                Icon = Resources.random_album_icon,
                ContextMenu = _contextMenu,
                Visible = true
            };

            _contextMenu.MenuItems[3].Checked = true;

            _trayIcon.DoubleClick += TrayIconDoubleClick;

            _openInBrowser = bool.Parse(ConfigurationManager.AppSettings["OpenInBrowser"]);
            _browserPath = ConfigurationManager.AppSettings["BrowserPath"];
            _albumListFileName = ConfigurationManager.AppSettings["AlbumListFileName"] ?? _albumListFileName;

            var albumsReader = new AlbumListReader();
            _albums = albumsReader.GetAlbums(_albumListFileName);

            _openInSpotifyLogic = new OpenInSpotifyLogic();
            _openInSpotifyLogic.Albums = _albums;
            _openInSpotifyLogic.NextMode = NextMode.Shuffle;
            _openInSpotifyLogic.ShuffleAlbums();
        }

        private void OpenRandomAlbum()
        {
            _openInSpotifyLogic.Open(_openInBrowser, _browserPath);
        }

        private void UpdateAlbumList()
        {
            OpenCliForm openCliForm = new OpenCliForm("Update album list");

            openCliForm.Show();
            openCliForm.OpenProcess("TrayAlbumRandomizer.Cli.exe", _albumListFileName);

            openCliForm.FormClosed += OpenCliFormFormClosed;
        }

        private void OpenCliFormFormClosed(object sender, FormClosedEventArgs e)
        {
            var albumsReader = new AlbumListReader();
            _albums = albumsReader.GetAlbums(_albumListFileName);

            _openInSpotifyLogic.ShuffleAlbums();

            var openCliForm = sender as OpenCliForm;
            openCliForm.FormClosed -= OpenCliFormFormClosed;
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

        private void OptionRandomClicked(object sender, EventArgs e)
        {
            _openInSpotifyLogic.NextMode = NextMode.Random;
            _contextMenu.MenuItems[2].Checked = true;
            _contextMenu.MenuItems[3].Checked = false;
        }

        private void OptionShuffleClicked(object sender, EventArgs e)
        {
            _openInSpotifyLogic.NextMode = NextMode.Shuffle;
            _contextMenu.MenuItems[2].Checked = false;
            _contextMenu.MenuItems[3].Checked = true;

            _openInSpotifyLogic.ShuffleAlbums();
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;

            Application.Exit();
        }
    }
}
