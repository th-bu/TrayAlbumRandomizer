namespace TrayAlbumRandomizer
{
    using System.IO;
    using System.Xml.Serialization;
    using TrayAlbumRandomizer.Interfaces;
    using TrayAlbumRandomizer.Pocos;

    public class AlbumListReader : IAlbumListReader
    {
        public SavableAlbum[] GetAlbums(string albumsPath)
        {
            var serializer = new XmlSerializer(typeof(SavableAlbum[]));

            using (FileStream fileStream = new FileStream(albumsPath, FileMode.Open))
            {
                return (SavableAlbum[])serializer.Deserialize(fileStream);
            }
        }
    }
}
