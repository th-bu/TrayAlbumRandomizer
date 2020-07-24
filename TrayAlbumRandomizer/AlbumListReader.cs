namespace TrayAlbumRandomizer
{
    using Newtonsoft.Json;
    using System.IO;
    using TrayAlbumRandomizer.AlbumListUpdate;

    public class AlbumListReader
    {
        public SavableAlbum[] GetAlbums(string albumsPath)
        {
            if (!File.Exists(albumsPath))
            {
                return new SavableAlbum[0];
            }

            var json = File.ReadAllText(albumsPath);
            return JsonConvert.DeserializeObject<SavableAlbum[]>(json);
        }
    }
}
