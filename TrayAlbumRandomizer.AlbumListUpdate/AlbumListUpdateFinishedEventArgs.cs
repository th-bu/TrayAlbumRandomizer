namespace TrayAlbumRandomizer.AlbumListUpdate
{
    using System;

    public class AlbumListUpdateFinishedEventArgs : EventArgs
    {
        public SavableAlbum[] Albums { get; set; }
    }
}
