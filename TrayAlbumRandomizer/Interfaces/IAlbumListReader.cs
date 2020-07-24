namespace TrayAlbumRandomizer.Interfaces
{
    using TrayAlbumRandomizer.AlbumListUpdate;

    public interface IAlbumListReader
    {
        SavableAlbum[] GetAlbums(string albumsPath);
    }
}
