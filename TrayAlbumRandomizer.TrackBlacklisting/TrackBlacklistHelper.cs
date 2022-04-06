namespace TrayAlbumRandomizer.TrackBlacklisting
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.IO;
    using TrayAlbumRandomizer.Infrastructure;

    public class TrackBlacklistHelper
    {
        public List<string> GetBlacklist()
        {
            if (!File.Exists(Constants.TrackBlacklistFileName))
            {
                return new List<string>();
            }

            var json = File.ReadAllText(Constants.TrackBlacklistFileName);
            return JsonConvert.DeserializeObject<List<string>>(json);
        }

        public void SaveBlacklist(List<string> blacklist)
        {
            File.WriteAllText(Constants.TrackBlacklistFileName, JsonConvert.SerializeObject(blacklist));
        }

        public void AddToBlacklist(string trackUri)
        {
            var blacklist = this.GetBlacklist();

            if (!blacklist.Contains(trackUri))
            {
                blacklist.Add(trackUri);
                this.SaveBlacklist(blacklist);
            }
        }

        public void AddRangeToBlacklist(IEnumerable<string> trackUris)
        {
            var blacklist = this.GetBlacklist();

            foreach (var trackUri in trackUris)
            {
                if (!blacklist.Contains(trackUri))
                {
                    blacklist.Add(trackUri);
                }
            }

            this.SaveBlacklist(blacklist);
        }
    }
}
