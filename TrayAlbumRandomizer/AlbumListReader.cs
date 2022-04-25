namespace TrayAlbumRandomizer
{
    using Newtonsoft.Json;
    using System.IO;
    using System;
    using TrayAlbumRandomizer.AlbumListUpdate;
    using TrayAlbumRandomizer.Infrastructure;

    public static class AlbumListReader
    {
        public static SavableAlbum[] GetAlbums()
        {
            if (!File.Exists(Constants.AlbumListFileName))
            {
                return Array.Empty<SavableAlbum>();
            }

            string json = File.ReadAllText(Constants.AlbumListFileName);
            return JsonConvert.DeserializeObject<SavableAlbum[]>(json);
        }
    }
}
