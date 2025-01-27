using System.Text;
using System.Text.RegularExpressions;

namespace BDInfoDataSubstractor
{
    internal static class DataContentManager
    {
        private static readonly Regex _codeRegex = new Regex(@"\[code\](.+?)\[\/code\]", RegexOptions.Singleline);
        private static readonly Regex _numericRegex = new Regex(@"(\d+,)+(\d+)");
        private static readonly string[] _allowedQuickSummary = ["Disc Title", "Disc Label", "Disc Size", "Protection", "Playlist", "Size", "Length", "Total Bitrate", "Video", "Audio", "Subtitle"];

        public static async Task ParseValidDataAsync(string inputFile, string? outputFileBdInfoContent = null, string? outputFileQuickSummary = null)
        {
            string name = Path.GetFileNameWithoutExtension(inputFile);
            string content = await File.ReadAllTextAsync(inputFile);

            var discInfosSection = ExtractDiscInfo(content);
            discInfosSection = discInfosSection.Where(c => IsValidDiscInfoSection(c)).OrderByDescending(c => GetPlaylistSize(c));

            var summariesSection = ExtractSummaries(content);
            summariesSection = [.. summariesSection.Where(c => IsValidDiscInfoSection(c)).OrderByDescending(c => GetPlaylistSize(c))];

            var outFile = outputFileBdInfoContent;
            if (string.IsNullOrWhiteSpace(outFile))
            {
                outFile = Path.Combine(Path.GetDirectoryName(inputFile)!, $"{name}.bdinfo.txt");
            }
            await File.WriteAllTextAsync(outFile, discInfosSection.First());

            outFile = outputFileQuickSummary;
            if (string.IsNullOrWhiteSpace(outFile))
            {
                outFile = Path.Combine(Path.GetDirectoryName(inputFile)!, $"{name}.quicksummary.txt");
            }
            await File.WriteAllTextAsync(outFile, summariesSection.First());
        }

        private static IEnumerable<string> ExtractDiscInfo(string content)
        {
            var matches = _codeRegex.Matches(content);

            foreach (Match match in matches)
            {
                if (match.Groups[1].Value.Contains("disc info:", StringComparison.OrdinalIgnoreCase))
                {
                    yield return match.Groups[1].Value.Trim();
                }
            }
        }

        private static bool IsValidDiscInfoSection(string section)
        {
            var lines = section.Split(Environment.NewLine);
            long discSize = 0;
            long playlistSize = 0;

            foreach (var line in lines)
            {
                var sline = line.Trim();

                if (sline.StartsWith("disc size:", StringComparison.OrdinalIgnoreCase))
                {
                    var dscStr = _numericRegex.Match(sline).Groups[0].Value.Replace(",", "");
                    _ = long.TryParse(dscStr, out discSize);
                    continue;
                }

                if (sline.StartsWith("size:", StringComparison.OrdinalIgnoreCase))
                {
                    var dscStr = _numericRegex.Match(sline).Groups[0].Value.Replace(",", "");
                    _ = long.TryParse(dscStr, out playlistSize);
                    continue;
                }
            }

            return playlistSize <= discSize;
        }

        private static long GetPlaylistSize(string section)
        {
            foreach (var line in section.Split(Environment.NewLine))
            {
                if (line.StartsWith("size:", StringComparison.OrdinalIgnoreCase))
                {
                    var dscStr = _numericRegex.Match(line).Groups[0].Value.Replace(",", "");
                    _ = long.TryParse(dscStr, out var playlistSize);
                    return playlistSize;
                }
            }

            return 0L;
        }

        private static List<string> ExtractSummaries(string content)
        {
            List<string> summaries = [];
            bool quickStart = false;
            StringBuilder sb = new();
            int indexEmptyLine = 0;

            foreach (var line in content.Split(Environment.NewLine))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    indexEmptyLine++;
                    if (indexEmptyLine >= 4 && quickStart)
                    {
                        summaries.Add(sb.ToString());
                        indexEmptyLine = 0;
                        quickStart = false;
                    }

                    continue;
                }

                if (line.StartsWith("quick summary", StringComparison.OrdinalIgnoreCase))
                {
                    quickStart = true;
                    indexEmptyLine = 0;
                    sb = new();
                    continue;
                }

                if (quickStart && _allowedQuickSummary.Any(c => line.StartsWith(c)))
                {
                    sb.AppendLine(line);
                    indexEmptyLine = 0;
                    continue;
                }
            }

            summaries.RemoveAll(c => string.IsNullOrWhiteSpace(c));
            return summaries;
        }
    }
}
