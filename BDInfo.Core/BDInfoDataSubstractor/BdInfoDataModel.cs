
using System.Text.Json.Serialization;

namespace BDInfoDataSubstractor
{
    public sealed class BdInfoDataModel
    {
        public string DiscTitle { get; private set; } = string.Empty;
        public string DiscLabel { get; private set; } = string.Empty;
        public string DiscSize { get; private set; } = string.Empty;
        public long LongDiscSize => string.IsNullOrWhiteSpace(DiscSize) ? 0 : long.Parse(DiscSize.ToLower().Replace("bytes", "").Trim().Replace(",", ""));
        public PlaylistModel Playlist { get; private set; } = new();

        [JsonIgnore]
        public string InnerData { get; }

        public BdInfoDataModel(string inner)
        {
            InnerData = inner;
            PopulateFields(inner);
        }

        private void PopulateFields(string inner)
        {
            foreach (var line in inner.Split(Environment.NewLine))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var split = line.Split(':');

                if (split.Length > 1)
                {
                    var key = split[0].ToLower();
                    var value = string.Join(':', line.Split(':').Skip(1)).Trim();

                    if (key.StartsWith("disc title"))
                    {
                        DiscTitle = value;
                    }
                    else if (key.StartsWith("disc label"))
                    {
                        DiscLabel = value;
                    }
                    else if (key.StartsWith("disc size"))
                    {
                        DiscSize = value;
                    }
                    else if (key.StartsWith("name") || (key.StartsWith("playlist") && !key.Contains("report")))
                    {
                        Playlist ??= new PlaylistModel();
                        Playlist.Name = value;
                    }
                    else if (key.StartsWith("length"))
                    {
                        Playlist.Length = value;
                    }
                    else if (key.StartsWith("size"))
                    {
                        Playlist.Size = value;
                    }
                    else if (key.StartsWith("total bitrate"))
                    {
                        Playlist.TotalBitrate = value;
                        break;
                    }
                }
            }
        }
    }

    public sealed class PlaylistModel
    {
        public string Name { get; set; } = string.Empty;
        public string Length { get; set; } = string.Empty;
        public float LongLength => GetLength(Length);

        public string Size { get; set; } = string.Empty;
        public long LongSize => string.IsNullOrWhiteSpace(Size) ? 0 : long.Parse(Size.ToLower().Replace("bytes", "").Trim().Replace(",", ""));

        public string TotalBitrate { get; set; } = string.Empty;
        public long LongTotalBitrate => string.IsNullOrWhiteSpace(TotalBitrate) ? 0 : (long)(float.Parse(TotalBitrate.ToLower().Replace("mbps", "").Trim(), System.Globalization.NumberStyles.AllowDecimalPoint) * 1000);

        private static float GetLength(string length)
        {
            if (string.IsNullOrWhiteSpace(length) || !length.Contains(':'))
            {
                return 0.0f;
            }

            var valueData = length.Split('(')[0].Trim();
            var clockSplit = valueData.Split(':');

            return
                float.Parse(clockSplit[0]) * (24 * 60 * 60) +
                float.Parse(clockSplit[1]) * 60 +
                float.Parse(clockSplit[2], System.Globalization.NumberStyles.AllowDecimalPoint);
        }
    }
}
