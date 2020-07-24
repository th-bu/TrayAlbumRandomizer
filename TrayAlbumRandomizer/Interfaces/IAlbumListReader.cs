namespace TrayAlbumRandomizer.Interfaces
{
    using TrayAlbumRandomizer.Pocos;

    public interface IAlbumListReader
    {
        SavableAlbum[] GetAlbums(string albumsPath);
    }
}
