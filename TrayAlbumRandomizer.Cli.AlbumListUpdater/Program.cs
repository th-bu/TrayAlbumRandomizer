namespace TrayAlbumRandomizer.Cli.AlbumListUpdater
{
    using System;
    using TrayAlbumRandomizer.AlbumListUpdate;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Update started.");

            using (var albumListUpdater = new AlbumListUpdater(args[0]))
            {
                albumListUpdater.UpdateAlbumList().Wait();
            }

            Console.WriteLine("Update finished.");
        }
    }
}
