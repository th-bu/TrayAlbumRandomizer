namespace TrayAlbumRandomizer.TrackBlacklisting
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.IO;
    using TrayAlbumRandomizer.Infrastructure;

    public static class TrackBlacklistHelper
    {
        public static List<string> GetBlacklist()
        {
            if (!File.Exists(Constants.TrackBlacklistFileName))
            {
                return new List<string>();
            }

            string json = File.ReadAllText(Constants.TrackBlacklistFileName);
            return JsonConvert.DeserializeObject<List<string>>(json);
        }

        private static void SaveBlacklist(List<string> blacklist)
        {
            File.WriteAllText(Constants.TrackBlacklistFileName, JsonConvert.SerializeObject(blacklist));
        }

        public static void AddToBlacklist(string trackUri)
        {
            List<string> blacklist = GetBlacklist();

            if (!blacklist.Contains(trackUri))
            {
                blacklist.Add(trackUri);
                SaveBlacklist(blacklist);
            }
        }

        public static void AddRangeToBlacklist(IEnumerable<string> trackUris)
        {
            List<string> blacklist = GetBlacklist();

            foreach (string trackUri in trackUris)
            {
                if (!blacklist.Contains(trackUri))
                {
                    blacklist.Add(trackUri);
                }
            }

            SaveBlacklist(blacklist);
        }
    }
}
