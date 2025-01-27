using BDCommon;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace BDInfo
{
    internal class Program
    {
        private static BDROM BDROM = null;
        private static ScanBDROMResult ScanResult = null;

        private static readonly string ProductVersion = "0.8.0.0";
        private static BDSettings _bdinfoSettings;
        private static readonly string BDMV = "BDMV";

        private static string _error;
        private static string _debug;

        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CmdOptions>(args)
                .WithParsed(opts => Exec(opts));
        }

        private static void Exec(CmdOptions opts)
        {
            try
            {
                _error = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"error_{Path.GetFileName(opts.Path)}.log");
                _debug = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"debug_{Path.GetFileName(opts.Path)}.log");
                _bdinfoSettings = new BDSettings(opts);

                if (!opts.Path.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                {
                    var subItems = Directory.GetDirectories(opts.Path, BDMV, SearchOption.AllDirectories);
                    bool isIsoLevel = false;

                    if (subItems.Length == 0)
                    {
                        var di = new DirectoryInfo(opts.Path);
                        var files = di.GetFiles("*.*", SearchOption.AllDirectories);
                        subItems = files.Where(s => s.FullName.EndsWith(".iso", StringComparison.OrdinalIgnoreCase)).Select(s => s.FullName).ToArray();
                        isIsoLevel = subItems.LongLength > 0;
                    }

                    if (subItems.Length > 1 || isIsoLevel)
                    {
                        var oldOpt = Cloner.Clone(opts);
                        List<string> reports = [];

                        foreach (var subDir in subItems.OrderBy(s => s))
                        {
                            opts.Path = isIsoLevel ? subDir : Path.GetDirectoryName(subDir);
                            if (!string.IsNullOrWhiteSpace(opts.ReportFileName))
                            {
                                var parent = Path.GetDirectoryName(opts.Path);
                                opts.ReportFileName = isIsoLevel ? Path.Combine(parent, Path.GetFileNameWithoutExtension(opts.Path)) : Path.Combine(parent, Path.GetFileName(opts.Path)) + "." + Path.GetExtension(opts.ReportFileName).TrimStart('.');
                                reports.Add(opts.ReportFileName);
                            }

                            InitBDROM(opts.Path);
                            ScanBDROM();
                        }

                        if (reports.Count > 0)
                        {
                            var bigReport = oldOpt.ReportFileName;
                            if (reports.Count == 1)
                            {
                                File.AppendAllLines(_debug, [Environment.NewLine, $"move file from {reports[0]} to {bigReport}", Environment.NewLine]);
                                File.Move(reports[0], bigReport);
                                return;
                            }

                            foreach (var report in reports)
                            {
                                File.AppendAllLines(_debug, [Environment.NewLine, "appending big reports", Environment.NewLine]);
                                File.AppendAllLines(bigReport, File.ReadAllLines(report));
                                File.AppendAllLines(bigReport, Enumerable.Repeat(Environment.NewLine, 5));

                                File.AppendAllLines(_debug, [Environment.NewLine, "delete report file after appending to big report", Environment.NewLine]);
                                File.Delete(report);
                            }
                        }

                        return;
                    }
                }

                InitBDROM(opts.Path);
                ScanBDROM();
            }
            catch (Exception ex)
            {
                var color = Console.ForegroundColor;
                Console.Error.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"{opts.Path} ::: {ex.Message}");

                Console.ForegroundColor = color;

                try
                {
                    File.AppendAllText(_error, $"{ex}{Environment.NewLine}{Environment.NewLine}");
                }
                catch
                {
                    // kills error
                }

                Environment.Exit(1);
            }
        }

        private static void InitBDROM(string path)
        {
            InitBDROMCompleted(InitBDROMWork(path));
        }

        private static object InitBDROMWork(string path)
        {
            try
            {
                BDROM = new BDROM(path, _bdinfoSettings);
                BDROM.StreamClipFileScanError += new BDROM.OnStreamClipFileScanError(BDROM_StreamClipFileScanError);
                BDROM.StreamFileScanError += new BDROM.OnStreamFileScanError(BDROM_StreamFileScanError);
                BDROM.PlaylistFileScanError += new BDROM.OnPlaylistFileScanError(BDROM_PlaylistFileScanError);
                BDROM.Scan();
                return null;
            }
            catch (Exception ex)
            {
                File.AppendAllText(_error, $"{ex}{Environment.NewLine}{Environment.NewLine}");
                return ex;
            }
        }

        private static bool BDROM_StreamClipFileScanError(TSStreamClipFile streamClipFile, Exception ex)
        {
            Console.WriteLine($"An error occurred while scanning the stream clip file {streamClipFile.Name}.");
            Console.WriteLine("The disc may be copy-protected or damaged.");
            Console.WriteLine("Will continue scanning the stream clip files.");

            return true;
        }

        private static bool BDROM_StreamFileScanError(TSStreamFile streamFile, Exception ex)
        {
            Console.WriteLine($"An error occurred while scanning the stream file {streamFile.Name}.");
            Console.WriteLine("The disc may be copy-protected or damaged.");
            Console.WriteLine("Will continue scanning the stream files.");

            return true;
        }

        private static bool BDROM_PlaylistFileScanError(TSPlaylistFile playlistFile, Exception ex)
        {
            Console.WriteLine($"An error occurred while scanning the playlist file {playlistFile.Name}.");
            Console.WriteLine("The disc may be copy-protected or damaged.");
            Console.WriteLine("Will continue scanning the playlist files.");

            return true;
        }

        private static void InitBDROMCompleted(object result)
        {
            if (result != null)
            {
                Console.WriteLine(((Exception)result).Message);

                return;
            }

            Console.WriteLine($"Detected BDMV Folder: {BDROM.DirectoryBDMV.FullName}");
            Console.WriteLine($"Disc Title: {BDROM.DiscTitle}");
            Console.WriteLine($"Disc Label: {BDROM.VolumeLabel}");

            //         if (_isImage)
            //{
            //	Console.WriteLine($"Detected BDMV Folder: {BDROM.DirectoryBDMV.FullName}");
            //	Console.WriteLine($"Disc Title: {BDROM.DiscTitle}");
            //	Console.WriteLine($"Disc Label: {BDROM.VolumeLabel}");
            //	Console.WriteLine($"ISO Image: {BDROM.DiscTitle}");
            //}
            //else
            //{
            //	Console.WriteLine($"Detected BDMV Folder: {BDROM.DirectoryBDMV.FullName}");
            //	Console.WriteLine($"Disc Title: {BDROM.DiscTitle}");
            //	Console.WriteLine($"Disc Label: {BDROM.VolumeLabel}");
            //}

            List<string> features = [];
            if (BDROM.IsUHD)
            {
                features.Add("Ultra HD");
            }
            if (BDROM.Is50Hz)
            {
                features.Add("50Hz Content");
            }
            if (BDROM.IsBDPlus)
            {
                features.Add("BD+ Copy Protection");
            }
            if (BDROM.IsBDJava)
            {
                features.Add("BD-Java");
            }
            if (BDROM.Is3D)
            {
                features.Add("Blu-ray 3D");
            }
            if (BDROM.IsDBOX)
            {
                features.Add("D-BOX Motion Code");
            }
            if (BDROM.IsPSP)
            {
                features.Add("PSP Digital Copy");
            }
            if (features.Count > 0)
            {
                Console.WriteLine($"Detected Features: {string.Join(", ", [.. features])}");
            }

            Console.WriteLine($"Disc Size: {BDROM.Size:N0} bytes ({ToolBox.FormatFileSize(BDROM.Size, true)})");
            Console.WriteLine();
        }

        private static void ScanBDROM()
        {
            if (BDROM is null)
            {
                throw new Exception("BDROM is null");
            }

            List<TSStreamFile> streamFiles = new(BDROM.StreamFiles.Values);

            ScanBDROMWork(streamFiles);
            ScanBDROMCompleted();
        }

        private static void ScanBDROMWork(List<TSStreamFile> streamFiles)
        {
            ScanResult = new ScanBDROMResult { ScanException = new Exception("Scan is still running.") };

            Timer timer = null;
            try
            {
                ScanBDROMState scanState = new();
                scanState.OnReportChange += ScanBDROMProgress;

                foreach (TSStreamFile streamFile in streamFiles)
                {
                    if (_bdinfoSettings.EnableSSIF &&
                            streamFile.InterleavedFile != null)
                    {
                        if (streamFile.InterleavedFile.FileInfo != null)
                            scanState.TotalBytes += streamFile.InterleavedFile.FileInfo.Length;
                        else
                            scanState.TotalBytes += streamFile.InterleavedFile.FileInfo.Length;
                    }
                    else
                    {
                        if (streamFile.FileInfo != null)
                            scanState.TotalBytes += streamFile.FileInfo.Length;
                        else
                            scanState.TotalBytes += streamFile.FileInfo.Length;
                    }

                    if (!scanState.PlaylistMap.ContainsKey(streamFile.Name))
                    {
                        scanState.PlaylistMap[streamFile.Name] = [];
                    }

                    foreach (TSPlaylistFile playlist in BDROM.PlaylistFiles.Values)
                    {
                        playlist.ClearBitrates();

                        foreach (TSStreamClip clip in playlist.StreamClips)
                        {
                            if (clip.Name == streamFile.Name)
                            {
                                if (!scanState.PlaylistMap[streamFile.Name].Contains(playlist))
                                {
                                    scanState.PlaylistMap[streamFile.Name].Add(playlist);
                                }
                            }
                        }
                    }
                }

                timer = new Timer(ScanBDROMEvent, scanState, 1000, 1000);

                foreach (TSStreamFile streamFile in streamFiles)
                {
                    scanState.StreamFile = streamFile;

                    Thread thread = new(ScanBDROMThread);
                    thread.Start(scanState);
                    while (thread.IsAlive)
                    {
                        Thread.Sleep(10);
                    }

                    if (streamFile.FileInfo != null)
                        scanState.FinishedBytes += streamFile.FileInfo.Length;
                    else
                        scanState.FinishedBytes += streamFile.FileInfo.Length;
                    if (scanState.Exception != null)
                    {
                        ScanResult.FileExceptions[streamFile.Name] = scanState.Exception;
                    }
                }
                ScanResult.ScanException = null;
            }
            catch (Exception ex)
            {
                ScanResult.ScanException = ex;
                File.AppendAllText(_error, $"{ex}{Environment.NewLine}{Environment.NewLine}");
            }
            finally
            {
                timer?.Dispose();
            }
        }

        private static void ScanBDROMProgress(ScanBDROMState scanState)
        {
            try
            {
                if (scanState.StreamFile == null)
                {
                    Console.Write("\rStarting Scan");
                }
                else
                {
                    Console.Write($"\rScanning {scanState.StreamFile.DisplayName}");
                }

                long finishedBytes = scanState.FinishedBytes;
                if (scanState.StreamFile != null)
                {
                    finishedBytes += scanState.StreamFile.Size;
                }

                double progress = ((double)finishedBytes / scanState.TotalBytes);
                double progressValue = Math.Clamp(100 * progress, 0, 100);

                TimeSpan elapsedTime = DateTime.Now.Subtract(scanState.TimeStarted);
                TimeSpan remainingTime;
                if (progress > 0 && progress < 1)
                {
                    remainingTime = new TimeSpan(
                            (long)((double)elapsedTime.Ticks / progress) - elapsedTime.Ticks);
                }
                else
                {
                    remainingTime = new TimeSpan(0);
                }

                Console.Write($" | Progress: {progressValue,6:F2}%");
                Console.Write($" | Elapsed: {elapsedTime.Hours:D2}:{elapsedTime.Minutes:D2}:{elapsedTime.Seconds:D2}");
                Console.Write($" | Remaining: {remainingTime.Hours:D2}:{remainingTime.Minutes:D2}:{remainingTime.Seconds:D2}");
            }
            catch (Exception ex)
            {
                File.AppendAllText(_error, $"{ex}{Environment.NewLine}{Environment.NewLine}");
            }
        }

        private static void ScanBDROMCompleted()
        {
            Console.WriteLine();

            if (ScanResult.ScanException != null)
            {
                Console.WriteLine("Scan complete.");
                Console.WriteLine($"{ScanResult.ScanException.Message}");
                File.AppendAllText(_error, $"{ScanResult.ScanException}{Environment.NewLine}{Environment.NewLine}");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(_bdinfoSettings.ReportFileName))
                {
                    Console.WriteLine("Scan complete.");
                    GenerateReport();
                }
                else if (ScanResult.FileExceptions.Count > 0)
                {
                    Console.WriteLine("Scan completed with errors (see report).");
                }
                else
                {
                    Console.WriteLine("Scan completed successfully.");
                }
            }
        }

        private static void ScanBDROMEvent(object state)
        {
            ScanBDROMProgress(state as ScanBDROMState);
        }

        private static void ScanBDROMThread(object parameter)
        {
            ScanBDROMState scanState = (ScanBDROMState)parameter;
            try
            {
                TSStreamFile streamFile = scanState.StreamFile;
                List<TSPlaylistFile> playlists = scanState.PlaylistMap[streamFile.Name];
                streamFile.Scan(playlists, true);
            }
            catch (Exception ex)
            {
                scanState.Exception = ex;
                File.AppendAllText(_error, $"{ex}{Environment.NewLine}{Environment.NewLine}");
            }
        }

        private static void GenerateReport()
        {
            IEnumerable<TSPlaylistFile> playlists = BDROM.PlaylistFiles.OrderByDescending(s => s.Value.FileSize).Select(s => s.Value);

            try
            {
                Generate(BDROM, playlists, ScanResult);
            }
            catch (Exception ex)
            {
                File.AppendAllText(_error, $"{ex}{Environment.NewLine}{Environment.NewLine}");
                Console.WriteLine(ex.Message);
            }
        }

        private static void Generate(BDROM BDROM, IEnumerable<TSPlaylistFile> playlists, ScanBDROMResult scanResult)
        {
            string reportName = Regex.IsMatch(_bdinfoSettings.ReportFileName, @"\{\d+\}", RegexOptions.IgnoreCase) ?
                string.Format(_bdinfoSettings.ReportFileName, BDROM.VolumeLabel) :
                _bdinfoSettings.ReportFileName;

            if (!Regex.IsMatch(reportName, @"\.(\w+)$", RegexOptions.IgnoreCase))
            {
                reportName = $"{reportName}.bdinfo";
            }

            if (File.Exists(reportName))
            {
                // creates a backup
                File.Move(reportName, $"{reportName}{Guid.NewGuid()}");
            }

            using StreamWriter sw = File.AppendText(reportName);
            string protection = BDROM.IsBDPlus ? "BD+" : (BDROM.IsUHD ? "AACS2" : "AACS");

            if (!string.IsNullOrEmpty(BDROM.DiscTitle))
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-16}{1}", "Disc Title:",
                                                                BDROM.DiscTitle));
            sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-16}{1}", "Disc Label:",
                                                                    BDROM.VolumeLabel));
            sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-16}{1:N0} bytes", "Disc Size:",
                                                                    BDROM.Size));
            sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-16}{1}", "Protection:",
                                                                    protection));

            List<string> extraFeatures = [];
            if (BDROM.IsUHD)
            {
                extraFeatures.Add("Ultra HD");
            }
            if (BDROM.IsBDJava)
            {
                extraFeatures.Add("BD-Java");
            }
            if (BDROM.Is50Hz)
            {
                extraFeatures.Add("50Hz Content");
            }
            if (BDROM.Is3D)
            {
                extraFeatures.Add("Blu-ray 3D");
            }
            if (BDROM.IsDBOX)
            {
                extraFeatures.Add("D-BOX Motion Code");
            }
            if (BDROM.IsPSP)
            {
                extraFeatures.Add("PSP Digital Copy");
            }
            if (extraFeatures.Count > 0)
            {
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                        "{0,-16}{1}", "Extras:",
                                                                        string.Join(", ", [.. extraFeatures])));
            }
            sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-16}{1}", "BDInfo:",
                                                                    ProductVersion));

            sw.WriteLine(Environment.NewLine);

            if (_bdinfoSettings.IncludeVersionAndNotes)
            {
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                        "{0,-16}{1}", "Notes:", ""));
                sw.WriteLine(Environment.NewLine);
                sw.WriteLine("BDINFO HOME:");
                sw.WriteLine("  Cinema Squid (old)");
                sw.WriteLine("    http://www.cinemasquid.com/blu-ray/tools/bdinfo");
                sw.WriteLine("  UniqProject GitHub (new)");
                sw.WriteLine("   https://github.com/UniqProject/BDInfo");
                sw.WriteLine(Environment.NewLine);
                sw.WriteLine("INCLUDES FORUMS REPORT FOR:");
                sw.WriteLine("  AVS Forum Blu-ray Audio and Video Specifications Thread");
                sw.WriteLine("    http://www.avsforum.com/avs-vb/showthread.php?t=1155731");
                sw.WriteLine(Environment.NewLine);
            }

            if (scanResult.ScanException != null)
            {
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                        "WARNING: Report is incomplete because: {0}",
                                                                        scanResult.ScanException.Message));
            }
            if (scanResult.FileExceptions.Count > 0)
            {
                sw.WriteLine("WARNING: File errors were encountered during scan:");
                foreach (string fileName in scanResult.FileExceptions.Keys)
                {
                    Exception fileException = scanResult.FileExceptions[fileName];
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                            "\r\n{0}\t{1}",
                                                                            fileName, fileException.Message));
                    sw.WriteLine(fileException.StackTrace);
                }
            }

            string separator = new('#', 10);

            foreach (TSPlaylistFile playlist in playlists.Where(pl => !_bdinfoSettings.FilterLoopingPlaylists || pl.IsValid))
            {
                StringBuilder summary = new();
                string title = playlist.Name;
                string discSize = string.Format(CultureInfo.InvariantCulture,
                                                                                                        "{0:N0}", BDROM.Size);

                TimeSpan playlistTotalLength =
                        new((long)(playlist.TotalLength * 10000000));

                string totalLength = string.Format(CultureInfo.InvariantCulture,
                                                                                                        "{0:D1}:{1:D2}:{2:D2}.{3:D3}",
                                                                                                        playlistTotalLength.Hours,
                                                                                                        playlistTotalLength.Minutes,
                                                                                                        playlistTotalLength.Seconds,
                                                                                                        playlistTotalLength.Milliseconds);

                string totalLengthShort = string.Format(CultureInfo.InvariantCulture,
                                                                                                        "{0:D1}:{1:D2}:{2:D2}",
                                                                                                        playlistTotalLength.Hours,
                                                                                                        playlistTotalLength.Minutes,
                                                                                                        playlistTotalLength.Seconds);

                string totalSize = string.Format(CultureInfo.InvariantCulture,
                                                                                                        "{0:N0}", playlist.TotalSize);

                string totalBitrate = string.Format(CultureInfo.InvariantCulture,
                                                                                                        "{0:F2}",
                                                                                                        Math.Round((double)playlist.TotalBitRate / 10000) / 100);

                TimeSpan playlistAngleLength = new((long)(playlist.TotalAngleLength * 10000000));

                string totalAngleLength = string.Format(CultureInfo.InvariantCulture,
                                                                                                        "{0:D1}:{1:D2}:{2:D2}.{3:D3}",
                                                                                                        playlistAngleLength.Hours,
                                                                                                        playlistAngleLength.Minutes,
                                                                                                        playlistAngleLength.Seconds,
                                                                                                        playlistAngleLength.Milliseconds);

                string totalAngleSize = string.Format(CultureInfo.InvariantCulture,
                                                                                                        "{0:N0}", playlist.TotalAngleSize);

                string totalAngleBitrate = string.Format(CultureInfo.InvariantCulture,
                                                                                                        "{0:F2}",
                                                                                                        Math.Round((double)playlist.TotalAngleBitRate / 10000) / 100);

                List<string> angleLengths = [];
                List<string> angleSizes = [];
                List<string> angleBitrates = [];
                List<string> angleTotalLengths = [];
                List<string> angleTotalSizes = [];
                List<string> angleTotalBitrates = [];
                if (playlist.AngleCount > 0)
                {
                    for (int angleIndex = 0; angleIndex < playlist.AngleCount; angleIndex++)
                    {
                        double angleLength = 0;
                        ulong angleSize = 0;
                        ulong angleTotalSize = 0;
                        if (angleIndex < playlist.AngleClips.Count &&
                                playlist.AngleClips[angleIndex] != null)
                        {
                            foreach (TSStreamClip clip in playlist.AngleClips[angleIndex].Values)
                            {
                                angleTotalSize += clip.PacketSize;
                                if (clip.AngleIndex == angleIndex + 1)
                                {
                                    angleSize += clip.PacketSize;
                                    angleLength += clip.Length;
                                }
                            }
                        }

                        angleSizes.Add(string.Format(CultureInfo.InvariantCulture, "{0:N0}", angleSize));

                        TimeSpan angleTimeSpan = new((long)(angleLength * 10000000));

                        angleLengths.Add(string.Format(CultureInfo.InvariantCulture,
                                                                                        "{0:D1}:{1:D2}:{2:D2}.{3:D3}",
                                                                                        angleTimeSpan.Hours,
                                                                                        angleTimeSpan.Minutes,
                                                                                        angleTimeSpan.Seconds,
                                                                                        angleTimeSpan.Milliseconds));

                        angleTotalSizes.Add(string.Format(CultureInfo.InvariantCulture, "{0:N0}", angleTotalSize));

                        angleTotalLengths.Add(totalLength);

                        double angleBitrate = 0;
                        if (angleLength > 0)
                        {
                            angleBitrate = Math.Round((double)(angleSize * 8) / angleLength / 10000) / 100;
                        }
                        angleBitrates.Add(string.Format(CultureInfo.InvariantCulture, "{0:F2}", angleBitrate));

                        double angleTotalBitrate = 0;
                        if (playlist.TotalLength > 0)
                        {
                            angleTotalBitrate = Math.Round((double)(angleTotalSize * 8) / playlist.TotalLength / 10000) / 100;
                        }
                        angleTotalBitrates.Add(string.Format(CultureInfo.InvariantCulture, "{0:F2}", angleTotalBitrate));
                    }
                }

                string videoCodec = "";
                string videoBitrate = "";
                if (playlist.VideoStreams.Count > 0)
                {
                    TSStream videoStream = playlist.VideoStreams[0];
                    videoCodec = videoStream.CodecAltName;
                    videoBitrate = string.Format(CultureInfo.InvariantCulture, "{0:F2}", Math.Round((double)videoStream.BitRate / 10000) / 100);
                }

                StringBuilder audio1 = new();
                string languageCode1 = "";
                if (playlist.AudioStreams.Count > 0)
                {
                    TSAudioStream audioStream = playlist.AudioStreams[0];

                    languageCode1 = audioStream.LanguageCode;

                    audio1.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1}", audioStream.CodecAltName, audioStream.ChannelDescription));

                    if (audioStream.BitRate > 0)
                    {
                        audio1.Append(string.Format(CultureInfo.InvariantCulture,
                                                                                " {0}Kbps",
                                                                                (int)Math.Round((double)audioStream.BitRate / 1000)));
                    }

                    if (audioStream.SampleRate > 0 &&
                            audioStream.BitDepth > 0)
                    {
                        audio1.Append(string.Format(CultureInfo.InvariantCulture,
                                                                                " ({0}kHz/{1}-bit)",
                                                                                (int)Math.Round((double)audioStream.SampleRate / 1000),
                                                                                audioStream.BitDepth));
                    }
                }

                StringBuilder audio2 = new();
                if (playlist.AudioStreams.Count > 1)
                {
                    for (int i = 1; i < playlist.AudioStreams.Count; i++)
                    {
                        TSAudioStream audioStream = playlist.AudioStreams[i];

                        if (audioStream.LanguageCode == languageCode1 &&
                                audioStream.StreamType != TSStreamType.AC3_PLUS_SECONDARY_AUDIO &&
                                audioStream.StreamType != TSStreamType.DTS_HD_SECONDARY_AUDIO &&
                                !(audioStream.StreamType == TSStreamType.AC3_AUDIO &&
                                    audioStream.ChannelCount == 2))
                        {
                            audio2.Append(string.Format(CultureInfo.InvariantCulture,
                                                                                            "{0} {1}",
                                                                                            audioStream.CodecAltName, audioStream.ChannelDescription));

                            if (audioStream.BitRate > 0)
                            {
                                audio2.Append(string.Format(CultureInfo.InvariantCulture,
                                        " {0}Kbps",
                                        (int)Math.Round((double)audioStream.BitRate / 1000)));
                            }

                            if (audioStream.SampleRate > 0 &&
                                    audioStream.BitDepth > 0)
                            {
                                audio2.Append(string.Format(CultureInfo.InvariantCulture,
                                                                                        " ({0}kHz/{1}-bit)",
                                                                                        (int)Math.Round((double)audioStream.SampleRate / 1000),
                                                                                        audioStream.BitDepth));
                            }
                            break;
                        }
                    }
                }

                sw.WriteLine(Environment.NewLine);
                sw.WriteLine("********************");
                sw.WriteLine("PLAYLIST: " + playlist.Name);
                sw.WriteLine("********************");
                sw.WriteLine(Environment.NewLine);
                sw.WriteLine("<--- BEGIN FORUMS PASTE --->");
                sw.WriteLine("[code]");

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}",
                                                                "",
                                                                "",
                                                                "",
                                                                "",
                                                                "",
                                                                "Total",
                                                                "Video",
                                                                "",
                                                                ""));

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}",
                                                                "Title",
                                                                "Codec",
                                                                "Length",
                                                                "Movie Size",
                                                                "Disc Size",
                                                                "Bitrate",
                                                                "Bitrate",
                                                                "Main Audio Track",
                                                                "Secondary Audio Track"));

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}",
                                                                "-----",
                                                                "------",
                                                                "-------",
                                                                "--------------",
                                                                "--------------",
                                                                "-------",
                                                                "-------",
                                                                "------------------",
                                                                "---------------------"));

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}",
                                                                title,
                                                                videoCodec,
                                                                totalLengthShort,
                                                                totalSize,
                                                                discSize,
                                                                totalBitrate,
                                                                videoBitrate,
                                                                audio1.ToString(),
                                                                audio2.ToString()));

                sw.WriteLine("[/code]");
                sw.WriteLine(Environment.NewLine);
                sw.WriteLine("[code]");

                if (_bdinfoSettings.GroupByTime)
                {
                    sw.WriteLine($"{Environment.NewLine}{separator}Start group {playlistTotalLength.TotalMilliseconds}{separator}");
                }

                sw.WriteLine(Environment.NewLine);
                sw.WriteLine("DISC INFO:");
                sw.WriteLine(Environment.NewLine);

                if (!string.IsNullOrEmpty(BDROM.DiscTitle))
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-16}{1}", "Disc Title:", BDROM.DiscTitle));

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-16}{1}", "Disc Label:", BDROM.VolumeLabel));

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-16}{1:N0} bytes", "Disc Size:", BDROM.Size));

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-16}{1}", "Protection:", protection));

                if (extraFeatures.Count > 0)
                {
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-16}{1}", "Extras:", string.Join(", ", [.. extraFeatures])));
                }
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-16}{1}", "BDInfo:", ProductVersion));

                sw.WriteLine(Environment.NewLine);
                sw.WriteLine("PLAYLIST REPORT:");
                sw.WriteLine(Environment.NewLine);
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-24}{1}", "Name:", title));

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-24}{1} (h:m:s.ms)", "Length:", totalLength));

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-24}{1:N0} bytes", "Size:", totalSize));

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-24}{1} Mbps", "Total Bitrate:", totalBitrate));
                if (playlist.AngleCount > 0)
                {
                    for (int angleIndex = 0; angleIndex < playlist.AngleCount; angleIndex++)
                    {
                        sw.WriteLine(Environment.NewLine);
                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                        "{0,-24}{1} (h:m:s.ms) / {2} (h:m:s.ms)",
                                                                        string.Format(CultureInfo.InvariantCulture, "Angle {0} Length:", angleIndex + 1),
                                                                        angleLengths[angleIndex], angleTotalLengths[angleIndex]));

                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                        "{0,-24}{1:N0} bytes / {2:N0} bytes",
                                                                        string.Format(CultureInfo.InvariantCulture, "Angle {0} Size:", angleIndex + 1),
                                                                        angleSizes[angleIndex], angleTotalSizes[angleIndex]));

                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                        "{0,-24}{1} Mbps / {2} Mbps",
                                                                        string.Format(CultureInfo.InvariantCulture, "Angle {0} Total Bitrate:", angleIndex + 1),
                                                                        angleBitrates[angleIndex], angleTotalBitrates[angleIndex], angleIndex));
                    }
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-24}{1} (h:m:s.ms)", "All Angles Length:", totalAngleLength));

                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-24}{1} bytes", "All Angles Size:", totalAngleSize));

                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-24}{1} Mbps", "All Angles Bitrate:", totalAngleBitrate));
                }

                if (!string.IsNullOrEmpty(BDROM.DiscTitle))
                    summary.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "Disc Title: {0}", BDROM.DiscTitle));

                summary.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                                                 "Disc Label: {0}", BDROM.VolumeLabel));

                summary.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                                                 "Disc Size: {0:N0} bytes", BDROM.Size));

                summary.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                                                 "Protection: {0}", protection));

                summary.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                                                 "Playlist: {0}", title));

                summary.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                                                 "Size: {0:N0} bytes", totalSize));

                summary.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                                                 "Length: {0}", totalLength));

                summary.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                                                 "Total Bitrate: {0} Mbps", totalBitrate));

                if (playlist.HasHiddenTracks)
                {
                    sw.WriteLine("\r\n(*) Indicates included stream hidden by this playlist.");
                }

                if (playlist.VideoStreams.Count > 0)
                {
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine("VIDEO:");
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-24}{1,-20}{2,-16}",
                                                                    "Codec",
                                                                    "Bitrate",
                                                                    "Description"));
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-24}{1,-20}{2,-16}",
                                                                    "-----",
                                                                    "-------",
                                                                    "-----------"));

                    foreach (TSStream stream in playlist.SortedStreams)
                    {
                        if (!stream.IsVideoStream) continue;

                        string streamName = stream.CodecName;
                        if (stream.AngleIndex > 0)
                        {
                            streamName = string.Format(CultureInfo.InvariantCulture,
                                                                                    "{0} ({1})", streamName, stream.AngleIndex);
                        }

                        string streamBitrate = string.Format(CultureInfo.InvariantCulture,
                                                                                                "{0:D}",
                                                                                                (int)Math.Round((double)stream.BitRate / 1000));
                        if (stream.AngleIndex > 0)
                        {
                            streamBitrate = string.Format(CultureInfo.InvariantCulture,
                                                                                            "{0} ({1:D})",
                                                                                            streamBitrate,
                                                                                            (int)Math.Round((double)stream.ActiveBitRate / 1000));
                        }
                        streamBitrate = $"{streamBitrate} kbps";

                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                        "{0,-24}{1,-20}{2,-16}",
                                                                        (stream.IsHidden ? "* " : "") + streamName,
                                                                        streamBitrate,
                                                                        stream.Description));

                        summary.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                                                        (stream.IsHidden ? "* " : "") + "Video: {0} / {1} / {2}",
                                                                        streamName,
                                                                        streamBitrate,
                                                                        stream.Description));
                    }
                }

                if (playlist.AudioStreams.Count > 0)
                {
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine("AUDIO:");
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-32}{1,-16}{2,-16}{3,-16}",
                                                                    "Codec",
                                                                    "Language",
                                                                    "Bitrate",
                                                                    "Description"));
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-32}{1,-16}{2,-16}{3,-16}",
                                                                    "-----",
                                                                    "--------",
                                                                    "-------",
                                                                    "-----------"));

                    foreach (TSStream stream in playlist.SortedStreams)
                    {
                        if (!stream.IsAudioStream) continue;

                        string streamBitrate = string.Format(CultureInfo.InvariantCulture,
                                                                                                "{0:D} kbps",
                                                                                                (int)Math.Round((double)stream.BitRate / 1000));

                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                        "{0,-32}{1,-16}{2,-16}{3,-16}",
                                                                        (stream.IsHidden ? "* " : "") + stream.CodecName,
                                                                        stream.LanguageName,
                                                                        streamBitrate,
                                                                        stream.Description));

                        summary.AppendLine(string.Format(
                                (stream.IsHidden ? "* " : "") + "Audio: {0} / {1} / {2}",
                                stream.LanguageName,
                                stream.CodecName,
                                stream.Description));
                    }
                }

                if (playlist.GraphicsStreams.Count > 0)
                {
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine("SUBTITLES:");
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-32}{1,-16}{2,-16}{3,-16}",
                                                                    "Codec",
                                                                    "Language",
                                                                    "Bitrate",
                                                                    "Description"));
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-32}{1,-16}{2,-16}{3,-16}",
                                                                    "-----",
                                                                    "--------",
                                                                    "-------",
                                                                    "-----------"));

                    foreach (TSStream stream in playlist.SortedStreams)
                    {
                        if (!stream.IsGraphicsStream) continue;

                        string streamBitrate = string.Format(CultureInfo.InvariantCulture,
                                                                                                 "{0:F3} kbps",
                                                                                                 (double)stream.BitRate / 1000);

                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                        "{0,-32}{1,-16}{2,-16}{3,-16}",
                                                                        (stream.IsHidden ? "* " : "") + stream.CodecName,
                                                                        stream.LanguageName,
                                                                        streamBitrate,
                                                                        stream.Description));

                        summary.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                                                         (stream.IsHidden ? "* " : "") + "Subtitle: {0} / {1}",
                                                                         stream.LanguageName,
                                                                         streamBitrate,
                                                                         stream.Description));
                    }
                }

                if (playlist.TextStreams.Count > 0)
                {
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine("TEXT:");
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-32}{1,-16}{2,-16}{3,-16}",
                                                                    "Codec",
                                                                    "Language",
                                                                    "Bitrate",
                                                                    "Description"));
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-32}{1,-16}{2,-16}{3,-16}",
                                                                    "-----",
                                                                    "--------",
                                                                    "-------",
                                                                    "-----------"));

                    foreach (TSStream stream in playlist.SortedStreams)
                    {
                        if (!stream.IsTextStream) continue;

                        string streamBitrate = string.Format(CultureInfo.InvariantCulture,
                                                                                                 "{0:F3} kbps",
                                                                                                 (double)stream.BitRate / 1000);

                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                        "{0,-32}{1,-16}{2,-16}{3,-16}",
                                                                        (stream.IsHidden ? "* " : "") + stream.CodecName,
                                                                        stream.LanguageName,
                                                                        streamBitrate,
                                                                        stream.Description));
                    }
                }

                sw.WriteLine(Environment.NewLine);
                sw.WriteLine("FILES:");
                sw.WriteLine(Environment.NewLine);
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}",
                                                                "Name",
                                                                "Time In",
                                                                "Length",
                                                                "Size",
                                                                "Total Bitrate"));
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}",
                                                                "----",
                                                                "-------",
                                                                "------",
                                                                "----",
                                                                "-------------"));

                foreach (TSStreamClip clip in playlist.StreamClips)
                {
                    string clipName = clip.DisplayName;

                    if (clip.AngleIndex > 0)
                    {
                        clipName = string.Format(CultureInfo.InvariantCulture,
                                                                            "{0} ({1})", clipName, clip.AngleIndex);
                    }

                    string clipSize = string.Format(CultureInfo.InvariantCulture,
                                                                                    "{0:N0}", clip.PacketSize);

                    TimeSpan clipInSpan =
                            new((long)(clip.RelativeTimeIn * 10000000));
                    TimeSpan clipOutSpan =
                            new((long)(clip.RelativeTimeOut * 10000000));
                    TimeSpan clipLengthSpan =
                            new((long)(clip.Length * 10000000));

                    string clipTimeIn = string.Format(CultureInfo.InvariantCulture,
                                                                                            "{0:D1}:{1:D2}:{2:D2}.{3:D3}",
                                                                                            clipInSpan.Hours,
                                                                                            clipInSpan.Minutes,
                                                                                            clipInSpan.Seconds,
                                                                                            clipInSpan.Milliseconds);
                    string clipLength = string.Format(CultureInfo.InvariantCulture,
                                                                                        "{0:D1}:{1:D2}:{2:D2}.{3:D3}",
                                                                                        clipLengthSpan.Hours,
                                                                                        clipLengthSpan.Minutes,
                                                                                        clipLengthSpan.Seconds,
                                                                                        clipLengthSpan.Milliseconds);

                    string clipBitrate = Math.Round(
                            (double)clip.PacketBitRate / 1000).ToString("N0", CultureInfo.InvariantCulture);

                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}",
                                                                    clipName,
                                                                    clipTimeIn,
                                                                    clipLength,
                                                                    clipSize,
                                                                    clipBitrate));
                }

                if (_bdinfoSettings.GroupByTime)
                {
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine(separator + "End group" + separator);
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine(Environment.NewLine);
                }

                sw.WriteLine(Environment.NewLine);
                sw.WriteLine("CHAPTERS:");
                sw.WriteLine(Environment.NewLine);
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}{5,-16}{6,-16}{7,-16}{8,-16}{9,-16}{10,-16}{11,-16}{12,-16}",
                                                                "Number",
                                                                "Time In",
                                                                "Length",
                                                                "Avg Video Rate",
                                                                "Max 1-Sec Rate",
                                                                "Max 1-Sec Time",
                                                                "Max 5-Sec Rate",
                                                                "Max 5-Sec Time",
                                                                "Max 10Sec Rate",
                                                                "Max 10Sec Time",
                                                                "Avg Frame Size",
                                                                "Max Frame Size",
                                                                "Max Frame Time"));
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}{5,-16}{6,-16}{7,-16}{8,-16}{9,-16}{10,-16}{11,-16}{12,-16}",
                                                                "------",
                                                                "-------",
                                                                "------",
                                                                "--------------",
                                                                "--------------",
                                                                "--------------",
                                                                "--------------",
                                                                "--------------",
                                                                "--------------",
                                                                "--------------",
                                                                "--------------",
                                                                "--------------",
                                                                "--------------"));

                Queue<double> window1Bits = new();
                Queue<double> window1Seconds = new();
                double window1BitsSum = 0;
                double window1SecondsSum = 0;
                double window1PeakBitrate = 0;
                double window1PeakLocation = 0;

                Queue<double> window5Bits = new();
                Queue<double> window5Seconds = new();
                double window5BitsSum = 0;
                double window5SecondsSum = 0;
                double window5PeakBitrate = 0;
                double window5PeakLocation = 0;

                Queue<double> window10Bits = new();
                Queue<double> window10Seconds = new();
                double window10BitsSum = 0;
                double window10SecondsSum = 0;
                double window10PeakBitrate = 0;
                double window10PeakLocation = 0;

                double chapterPosition = 0;
                double chapterBits = 0;
                long chapterFrameCount = 0;
                double chapterSeconds = 0;
                double chapterMaxFrameSize = 0;
                double chapterMaxFrameLocation = 0;

                ushort diagPID = playlist.VideoStreams.FirstOrDefault()?.PID ?? 0;

                int chapterIndex = 0;
                int clipIndex = 0;
                int diagIndex = 0;

                while (chapterIndex < playlist.Chapters.Count)
                {
                    TSStreamClip clip = null;
                    TSStreamFile file = null;

                    if (clipIndex < playlist.StreamClips.Count)
                    {
                        clip = playlist.StreamClips[clipIndex];
                        file = clip.StreamFile;
                    }

                    double chapterStart = playlist.Chapters[chapterIndex];
                    double chapterEnd;
                    if (chapterIndex < playlist.Chapters.Count - 1)
                    {
                        chapterEnd = playlist.Chapters[chapterIndex + 1];
                    }
                    else
                    {
                        chapterEnd = playlist.TotalLength;
                    }
                    double chapterLength = chapterEnd - chapterStart;

                    List<TSStreamDiagnostics> diagList = null;

                    if (clip != null &&
                            clip.AngleIndex == 0 &&
                            file != null &&
                            file.StreamDiagnostics.ContainsKey(diagPID))
                    {
                        diagList = file.StreamDiagnostics[diagPID];

                        while (diagIndex < diagList.Count &&
                                chapterPosition < chapterEnd)
                        {
                            TSStreamDiagnostics diag = diagList[diagIndex++];

                            if (diag.Marker < clip.TimeIn) continue;

                            chapterPosition =
                                    diag.Marker -
                                    clip.TimeIn +
                                    clip.RelativeTimeIn;

                            double seconds = diag.Interval;
                            double bits = diag.Bytes * 8.0;

                            chapterBits += bits;
                            chapterSeconds += seconds;

                            if (diag.Tag != null)
                            {
                                chapterFrameCount++;
                            }

                            window1SecondsSum += seconds;
                            window1Seconds.Enqueue(seconds);
                            window1BitsSum += bits;
                            window1Bits.Enqueue(bits);

                            window5SecondsSum += diag.Interval;
                            window5Seconds.Enqueue(diag.Interval);
                            window5BitsSum += bits;
                            window5Bits.Enqueue(bits);

                            window10SecondsSum += seconds;
                            window10Seconds.Enqueue(seconds);
                            window10BitsSum += bits;
                            window10Bits.Enqueue(bits);

                            if (bits > chapterMaxFrameSize * 8)
                            {
                                chapterMaxFrameSize = bits / 8;
                                chapterMaxFrameLocation = chapterPosition;
                            }
                            if (window1SecondsSum > 1.0)
                            {
                                double bitrate = window1BitsSum / window1SecondsSum;
                                if (bitrate > window1PeakBitrate &&
                                        chapterPosition - window1SecondsSum > 0)
                                {
                                    window1PeakBitrate = bitrate;
                                    window1PeakLocation = chapterPosition - window1SecondsSum;
                                }
                                window1BitsSum -= window1Bits.Dequeue();
                                window1SecondsSum -= window1Seconds.Dequeue();
                            }
                            if (window5SecondsSum > 5.0)
                            {
                                double bitrate = window5BitsSum / window5SecondsSum;
                                if (bitrate > window5PeakBitrate &&
                                        chapterPosition - window5SecondsSum > 0)
                                {
                                    window5PeakBitrate = bitrate;
                                    window5PeakLocation = chapterPosition - window5SecondsSum;
                                    if (window5PeakLocation < 0)
                                    {
                                        window5PeakLocation = 0;
                                        window5PeakLocation = 0;
                                    }
                                }
                                window5BitsSum -= window5Bits.Dequeue();
                                window5SecondsSum -= window5Seconds.Dequeue();
                            }
                            if (window10SecondsSum > 10.0)
                            {
                                double bitrate = window10BitsSum / window10SecondsSum;
                                if (bitrate > window10PeakBitrate &&
                                        chapterPosition - window10SecondsSum > 0)
                                {
                                    window10PeakBitrate = bitrate;
                                    window10PeakLocation = chapterPosition - window10SecondsSum;
                                }
                                window10BitsSum -= window10Bits.Dequeue();
                                window10SecondsSum -= window10Seconds.Dequeue();
                            }
                        }
                    }
                    if (diagList == null ||
                            diagIndex == diagList.Count)
                    {
                        if (clipIndex < playlist.StreamClips.Count)
                        {
                            clipIndex++; diagIndex = 0;
                        }
                        else
                        {
                            chapterPosition = chapterEnd;
                        }
                    }
                    if (chapterPosition >= chapterEnd)
                    {
                        ++chapterIndex;

                        TimeSpan window1PeakSpan = new((long)(window1PeakLocation * 10000000));
                        TimeSpan window5PeakSpan = new((long)(window5PeakLocation * 10000000));
                        TimeSpan window10PeakSpan = new((long)(window10PeakLocation * 10000000));
                        TimeSpan chapterMaxFrameSpan = new((long)(chapterMaxFrameLocation * 10000000));
                        TimeSpan chapterStartSpan = new((long)(chapterStart * 10000000));
                        TimeSpan chapterEndSpan = new((long)(chapterEnd * 10000000));
                        TimeSpan chapterLengthSpan = new((long)(chapterLength * 10000000));

                        double chapterBitrate = 0;
                        if (chapterLength > 0)
                        {
                            chapterBitrate = chapterBits / chapterLength;
                        }
                        double chapterAvgFrameSize = 0;
                        if (chapterFrameCount > 0)
                        {
                            chapterAvgFrameSize = chapterBits / chapterFrameCount / 8;
                        }

                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                        "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}{5,-16}{6,-16}{7,-16}{8,-16}{9,-16}{10,-16}{11,-16}{12,-16}",
                                                                        chapterIndex,
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:D1}:{1:D2}:{2:D2}.{3:D3}", chapterStartSpan.Hours, chapterStartSpan.Minutes, chapterStartSpan.Seconds, chapterStartSpan.Milliseconds),
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:D1}:{1:D2}:{2:D2}.{3:D3}", chapterLengthSpan.Hours, chapterLengthSpan.Minutes, chapterLengthSpan.Seconds, chapterLengthSpan.Milliseconds),
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:N0} kbps", Math.Round(chapterBitrate / 1000)),
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:N0} kbps", Math.Round(window1PeakBitrate / 1000)),
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}.{3:D3}", window1PeakSpan.Hours, window1PeakSpan.Minutes, window1PeakSpan.Seconds, window1PeakSpan.Milliseconds),
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:N0} kbps", Math.Round(window5PeakBitrate / 1000)),
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}.{3:D3}", window5PeakSpan.Hours, window5PeakSpan.Minutes, window5PeakSpan.Seconds, window5PeakSpan.Milliseconds),
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:N0} kbps", Math.Round(window10PeakBitrate / 1000)),
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}.{3:D3}", window10PeakSpan.Hours, window10PeakSpan.Minutes, window10PeakSpan.Seconds, window10PeakSpan.Milliseconds),
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:N0} bytes", chapterAvgFrameSize),
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:N0} bytes", chapterMaxFrameSize),
                                                                        string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}.{3:D3}", chapterMaxFrameSpan.Hours, chapterMaxFrameSpan.Minutes, chapterMaxFrameSpan.Seconds, chapterMaxFrameSpan.Milliseconds)));

                        window1Bits = new Queue<double>();
                        window1Seconds = new Queue<double>();
                        window1BitsSum = 0;
                        window1SecondsSum = 0;
                        window1PeakBitrate = 0;
                        window1PeakLocation = 0;

                        window5Bits = new Queue<double>();
                        window5Seconds = new Queue<double>();
                        window5BitsSum = 0;
                        window5SecondsSum = 0;
                        window5PeakBitrate = 0;
                        window5PeakLocation = 0;

                        window10Bits = new Queue<double>();
                        window10Seconds = new Queue<double>();
                        window10BitsSum = 0;
                        window10SecondsSum = 0;
                        window10PeakBitrate = 0;
                        window10PeakLocation = 0;

                        chapterBits = 0;
                        chapterSeconds = 0;
                        chapterFrameCount = 0;
                        chapterMaxFrameSize = 0;
                        chapterMaxFrameLocation = 0;
                    }
                }

                if (_bdinfoSettings.GenerateStreamDiagnostics)
                {
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine("STREAM DIAGNOSTICS:");
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-16}{1,-16}{2,-16}{3,-16}{4,-24}{5,-24}{6,-24}{7,-16}{8,-16}",
                                                                    "File",
                                                                    "PID",
                                                                    "Type",
                                                                    "Codec",
                                                                    "Language",
                                                                    "Seconds",
                                                                    "Bitrate",
                                                                    "Bytes",
                                                                    "Packets"));
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                    "{0,-16}{1,-16}{2,-16}{3,-16}{4,-24}{5,-24}{6,-24}{7,-16}{8,-16}",
                                                                    "----",
                                                                    "---",
                                                                    "----",
                                                                    "-----",
                                                                    "--------",
                                                                    "--------------",
                                                                    "--------------",
                                                                    "-------------",
                                                                    "-----",
                                                                    "-------"));

                    Dictionary<string, TSStreamClip> reportedClips = [];
                    foreach (TSStreamClip clip in playlist.StreamClips)
                    {
                        if (clip.StreamFile == null) continue;
                        if (reportedClips.ContainsKey(clip.Name)) continue;
                        reportedClips[clip.Name] = clip;

                        string clipName = clip.DisplayName;
                        if (clip.AngleIndex > 0)
                        {
                            clipName = string.Format(CultureInfo.InvariantCulture, "{0} ({1})", clipName, clip.AngleIndex);
                        }
                        foreach (TSStream clipStream in clip.StreamFile.Streams.Values)
                        {
                            if (!playlist.Streams.ContainsKey(clipStream.PID)) continue;

                            TSStream playlistStream =
                                    playlist.Streams[clipStream.PID];

                            string clipBitRate = "0";
                            string clipSeconds = "0";

                            if (clip.StreamFile.Length > 0)
                            {
                                clipSeconds =
                                        clip.StreamFile.Length.ToString("F3", CultureInfo.InvariantCulture);
                                clipBitRate = Math.Round(
                                         (double)clipStream.PayloadBytes * 8 /
                                         clip.StreamFile.Length / 1000).ToString("N0", CultureInfo.InvariantCulture);
                            }
                            string language = "";
                            if (!string.IsNullOrEmpty(playlistStream.LanguageCode))
                            {
                                language = string.Format(CultureInfo.InvariantCulture,
                                        "{0} ({1})", playlistStream.LanguageCode, playlistStream.LanguageName);
                            }

                            sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                                            "{0,-16}{1,-16}{2,-16}{3,-16}{4,-24}{5,-24}{6,-24}{7,-16}{8,-16}",
                                                                            clipName,
                                                                            string.Format(CultureInfo.InvariantCulture, "{0} (0x{1:X})", clipStream.PID, clipStream.PID),
                                                                            string.Format(CultureInfo.InvariantCulture, "0x{0:X2}", (byte)clipStream.StreamType),
                                                                            clipStream.CodecShortName,
                                                                            language,
                                                                            clipSeconds,
                                                                            clipBitRate,
                                                                            clipStream.PayloadBytes.ToString("N0", CultureInfo.InvariantCulture),
                                                                            clipStream.PacketCount.ToString("N0", CultureInfo.InvariantCulture)));
                        }
                    }
                }

                sw.WriteLine(Environment.NewLine);
                sw.WriteLine("[/code]");
                sw.WriteLine("<---- END FORUMS PASTE ---->");
                sw.WriteLine(Environment.NewLine);

                if (_bdinfoSettings.GenerateTextSummary)
                {
                    sw.WriteLine("QUICK SUMMARY:");
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine(summary.ToString());
                    sw.WriteLine(Environment.NewLine);
                }

                sw.WriteLine(Environment.NewLine);

                File.AppendAllLines(_debug, [Environment.NewLine, "appending report to tmp", Environment.NewLine]);
                GC.Collect();
            }
        }
    }
}