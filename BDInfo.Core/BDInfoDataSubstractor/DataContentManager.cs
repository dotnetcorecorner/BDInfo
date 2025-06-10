using System.Text;
using System.Text.RegularExpressions;

namespace BDInfoDataSubstractor
{
    internal static class DataContentManager
    {
        private static readonly Regex _codeRegex = new(@"\[code\](.+?)\[\/code\]", RegexOptions.Singleline);
        private static readonly string[] _allowedQuickSummary = ["Disc Title", "Disc Label", "Disc Size", "Protection", "Playlist", "Size", "Length", "Total Bitrate", "Video", "Audio", "Subtitle"];

        public static async Task SubstractDataAsync(string inputFile, string? outputFileBdInfoContent = null, string? outputFileQuickSummary = null)
        {
            string name = Path.GetFileNameWithoutExtension(inputFile);
            string content = await File.ReadAllTextAsync(inputFile);

            var discInfosSection = ExtractDiscInfo(content);
            var summariesSection = ExtractSummaries(content);

            var filteredDiscSections = discInfosSection.Where(c => IsValidDiscInfoSection(c))
                .OrderByDescending(c => c.Playlist.LongTotalBitrate)
                .DistinctBy(c => c.Playlist.Name);

            var filteredSummariesSection = summariesSection.Where(c => IsValidDiscInfoSection(c))
                .OrderByDescending(c => c.Playlist.LongTotalBitrate)
                .DistinctBy(c => c.Playlist.Name);

            var outFile = outputFileBdInfoContent;
            if (string.IsNullOrWhiteSpace(outFile))
            {
                outFile = Path.Combine(Path.GetDirectoryName(inputFile)!, $"{name}.bdinfo.txt");
            }
            await File.WriteAllTextAsync(outFile, filteredDiscSections.First().InnerData);

            outFile = outputFileQuickSummary;
            if (string.IsNullOrWhiteSpace(outFile))
            {
                outFile = Path.Combine(Path.GetDirectoryName(inputFile)!, $"{name}.quicksummary.txt");
            }
            await File.WriteAllTextAsync(outFile, filteredSummariesSection.First().InnerData);
        }

        private static IEnumerable<BdInfoDataModel> ExtractDiscInfo(string content)
        {
            var matches = _codeRegex.Matches(content);

            foreach (Match match in matches)
            {
                if (match.Groups[1].Value.Contains("disc info:", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new(match.Groups[1].Value.Trim());
                }
            }
        }

        private static bool IsValidDiscInfoSection(BdInfoDataModel model)
        {
            // get by size and length greater than 10 minute(s)
            return model.Playlist.LongSize < model.LongDiscSize && model.Playlist.LongLength > 10 * 60;
        }

        private static IEnumerable<BdInfoDataModel> ExtractSummaries(string content)
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
            return summaries.Select(c => new BdInfoDataModel(c));
        }
    }
}
