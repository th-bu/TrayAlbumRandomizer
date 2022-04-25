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
                    UpdateAlbums();
                    break;
                case "-g":
                    GeneratePlaylist();
                    break;
            }            
        }

        private static void GeneratePlaylist()
        {
            Console.WriteLine("Playlist generation started.");

            try
            {
                using (var playlistGenerator = new PlaylistGenerator())
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

        private static void UpdateAlbums()
        {
            Console.WriteLine("Update started.");

            try
            {
                using (var albumListUpdater = new AlbumListUpdater())
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
