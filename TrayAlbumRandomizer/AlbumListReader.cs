namespace TrayAlbumRandomizer
{
    using Newtonsoft.Json;
    using System.IO;
    using TrayAlbumRandomizer.AlbumListUpdate;
    using TrayAlbumRandomizer.Infrastructure;

    public class AlbumListReader
    {
        public SavableAlbum[] GetAlbums()
        {
            if (!File.Exists(Constants.AlbumListFileName))
            {
                return new SavableAlbum[0];
            }

            var json = File.ReadAllText(Constants.AlbumListFileName);
            return JsonConvert.DeserializeObject<SavableAlbum[]>(json);
        }
    }
}
