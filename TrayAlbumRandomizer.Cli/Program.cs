namespace TrayAlbumRandomizer.Cli
{
    using System;
    using TrayAlbumRandomizer.AlbumListUpdate;
    using TrayAlbumRandomizer.PlaylistGeneration;

    class Program
    {
        static void Main(string[] args)
        {
            switch (args[0])
            {
                case "-u":
                    UpdateAlbums(args[1]);
                    break;
                case "-g":
                    GeneratePlaylist(args[1]);
                    break;
                default:
                    break;
            }            
        }

        private static void GeneratePlaylist(string albumListFileName)
        {
            Console.WriteLine("Playlist generation started.");

            try
            {
                using (var playlistGenerator = new PlaylistGenerator(albumListFileName))
                {
                    playlistGenerator.GeneratePlaylist().Wait();
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }

            Console.WriteLine("Playlist generation finished.");
        }

        private static void UpdateAlbums(string albumListFileName)
        {
            Console.WriteLine("Update started.");

            try
            {
                using (var albumListUpdater = new AlbumListUpdater(albumListFileName))
                {
                    albumListUpdater.UpdateAlbumList().Wait();
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }

            Console.WriteLine("Update finished.");
        }
    }
}
