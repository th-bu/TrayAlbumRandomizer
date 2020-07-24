namespace TrayAlbumRandomizer
{
    using Newtonsoft.Json;
    using System.IO;
    using TrayAlbumRandomizer.Interfaces;
    using TrayAlbumRandomizer.Pocos;

    public class AlbumListReader : IAlbumListReader
    {
        public SavableAlbum[] GetAlbums(string albumsPath)
        {
            var json = File.ReadAllText(albumsPath);
            return JsonConvert.DeserializeObject<SavableAlbum[]>(json);
        }
    }
}
