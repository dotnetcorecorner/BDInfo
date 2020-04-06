using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BDInfo.Core
{
  class Program
  {
    private BDROM _rom;
    private ScanBDROMResult _scanResult = new ScanBDROMResult();

    const string PROD_VERSION = "0.7.5.5";

    static void Main(string[] args)
    {
      if (args.Length == 0)
      {
        Console.WriteLine("No path specified !");
        return;
      }

      var pr = new Program();

      Console.WriteLine("Scanning content");
      pr.InitBDRom(args[0]).GetAwaiter().GetResult();

      Console.WriteLine("Scanning bitrate");
      pr.Scan(args[0]).GetAwaiter().GetResult();

      Console.WriteLine("Getting report");
      Console.Clear();

      pr.GetReport().GetAwaiter().GetResult();

      //Console.WriteLine("done");
      //Console.Read();
    }

    async Task Scan(string path)
    {
      Console.WriteLine("Scanning...");

      Timer timer = null;

      await Task.Factory.StartNew(() =>
      {
        try
        {
          List<TSStreamFile> streamFiles = new List<TSStreamFile>();
          foreach (var pls in _rom.PlaylistFiles)
          {
            foreach (TSStreamClip clip in pls.Value.StreamClips)
            {
              if (!streamFiles.Contains(clip.StreamFile))
              {
                streamFiles.Add(clip.StreamFile);
              }
            }
          }

          ScanBDROMState scanState = new ScanBDROMState();

          foreach (TSStreamFile streamFile in streamFiles)
          {
            if (BDInfoSettings.EnableSSIF &&
                streamFile.InterleavedFile != null)
            {
              if (streamFile.InterleavedFile.FileInfo != null)
                scanState.TotalBytes += streamFile.InterleavedFile.FileInfo.Length;
              else
                scanState.TotalBytes += streamFile.InterleavedFile.DFileInfo.Length;
            }
            else
            {
              if (streamFile.FileInfo != null)
                scanState.TotalBytes += streamFile.FileInfo.Length;
              else
                scanState.TotalBytes += streamFile.DFileInfo.Length;
            }

            if (!scanState.PlaylistMap.ContainsKey(streamFile.Name))
            {
              scanState.PlaylistMap[streamFile.Name] = new List<TSPlaylistFile>();
            }

            foreach (TSPlaylistFile playlist
                in _rom.PlaylistFiles.Values)
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

            Thread thread = new Thread(ScanBDROMThread);
            thread.Start(scanState);
            while (thread.IsAlive)
            {
              Thread.Sleep(100);
            }

            if (streamFile.FileInfo != null)
            {
              scanState.FinishedBytes += streamFile.FileInfo.Length;
            }
            else
            {
              scanState.FinishedBytes += streamFile.DFileInfo.Length;
            }

            if (scanState.Exception != null)
            {
              _scanResult.FileExceptions[streamFile.Name] = scanState.Exception;
            }
          }
          _scanResult.ScanException = null;
        }
        catch (Exception ex)
        {
          _scanResult.ScanException = ex;
        }
        finally
        {
          timer?.Dispose();
        }
      });
    }

    private void ScanBDROMEvent(object state)
    {
      try
      {
        if (state is ScanBDROMState scanState)
        {
          Console.WriteLine($"State: {scanState.TotalBytes}");
        }
      }
      catch { }
    }

    private void ScanBDROMThread(object parameter)
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
      }
    }

    async Task InitBDRom(string path)
    {
      Console.WriteLine("Please wait while we scan the disc...");

      await Task.Factory.StartNew(() =>
      {
        _rom = new BDROM(path);
        if (_rom != null && _rom.IsImage && _rom.CdReader != null)
        {
          _rom.CloseDiscImage();
        }

        _rom.StreamClipFileScanError += new BDROM.OnStreamClipFileScanError(BDROM_StreamClipFileScanError);
        _rom.StreamFileScanError += new BDROM.OnStreamFileScanError(BDROM_StreamFileScanError);
        _rom.PlaylistFileScanError += new BDROM.OnPlaylistFileScanError(BDROM_PlaylistFileScanError);
        _rom.Scan();
      });
    }

    async Task GetReport()
    {
      await Task.Factory.StartNew(() =>
      {
        if (BDInfoSettings.AutosaveReport)
        {
          List<string> report = new List<string>();
          string reportName = string.Format("report_{0}.txt", _rom.VolumeLabel);
          using (var reportFile = File.CreateText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reportName)))
          {
            string protection = (_rom.IsBDPlus ? "BD+" : _rom.IsUHD ? "AACS2" : "AACS");

            if (!string.IsNullOrEmpty(_rom.DiscTitle))
            {
              report.Add(string.Format("{0,-16}{1}\r\n", "Disc Title:", _rom.DiscTitle));
            }

            report.Add(string.Format("{0,-16}{1}\r\n", "Disc Label:", _rom.VolumeLabel));
            report.Add(string.Format("{0,-16}{1:N0} bytes\r\n", "Disc Size:", _rom.Size));
            report.Add(string.Format("{0,-16}{1}\r\n", "Protection:", protection));

            List<string> extraFeatures = new List<string>();
            if (_rom.IsUHD)
            {
              extraFeatures.Add("Ultra HD");
            }
            if (_rom.IsBDJava)
            {
              extraFeatures.Add("BD-Java");
            }
            if (_rom.Is50Hz)
            {
              extraFeatures.Add("50Hz Content");
            }
            if (_rom.Is3D)
            {
              extraFeatures.Add("Blu-ray 3D");
            }
            if (_rom.IsDBOX)
            {
              extraFeatures.Add("D-BOX Motion Code");
            }
            if (_rom.IsPSP)
            {
              extraFeatures.Add("PSP Digital Copy");
            }
            if (extraFeatures.Count > 0)
            {
              report.Add(string.Format("{0,-16}{1}\r\n", "Extras:", string.Join(", ", extraFeatures.ToArray())));
            }
            report.Add(string.Format("{0,-16}{1}\r\n", "BDInfo:", PROD_VERSION));

            report.Add("\r\n");
            report.Add(string.Format("{0,-16}{1}\r\n", "Notes:", ""));
            report.Add("\r\n");
            report.Add("BDINFO HOME:\r\n");
            report.Add("  Cinema Squid (old)\r\n");
            report.Add("    http://www.cinemasquid.com/blu-ray/tools/bdinfo\r\n");
            report.Add("  UniqProject GitHub (new)\r\n");
            report.Add("   https://github.com/UniqProject/BDInfo\r\n");
            report.Add("\r\n");
            report.Add("INCLUDES FORUMS REPORT FOR:\r\n");
            report.Add("  AVS Forum Blu-ray Audio and Video Specifications Thread\r\n");
            report.Add("    http://www.avsforum.com/avs-vb/showthread.php?t=1155731\r\n");
            report.Add("\r\n");

            if (_scanResult.FileExceptions.Count > 0)
            {
              report.Add("WARNING: File errors were encountered during scan:\r\n");
              foreach (string fileName in _scanResult.FileExceptions.Keys)
              {
                Exception fileException = _scanResult.FileExceptions[fileName];
                report.Add(string.Format("\r\n{0}\t{1}\r\n", fileName, fileException.Message));
                report.Add(string.Format("{0}\r\n", fileException.StackTrace));
              }
            }

            foreach (TSPlaylistFile playlist in _rom.PlaylistFiles.Select(s => s.Value))
            {
              string summary = "";

              string title = playlist.Name;
              string discSize = string.Format("{0:N0}", _rom.Size);

              TimeSpan playlistTotalLength = new TimeSpan((long)(playlist.TotalLength * 10000000));
              string totalLength = string.Format("{0:D1}:{1:D2}:{2:D2}.{3:D3}",
                                                          playlistTotalLength.Hours,
                                                          playlistTotalLength.Minutes,
                                                          playlistTotalLength.Seconds,
                                                          playlistTotalLength.Milliseconds);

              string totalLengthShort = string.Format(
                                                          "{0:D1}:{1:D2}:{2:D2}",
                                                          playlistTotalLength.Hours,
                                                          playlistTotalLength.Minutes,
                                                          playlistTotalLength.Seconds);

              string totalSize = string.Format("{0:N0}", playlist.TotalSize);

              string totalBitrate = string.Format(
                                                          "{0:F2}",
                                                          Math.Round((double)playlist.TotalBitRate / 10000) / 100);

              TimeSpan playlistAngleLength = new TimeSpan((long)(playlist.TotalAngleLength * 10000000));

              string totalAngleLength = string.Format(
                                                          "{0:D1}:{1:D2}:{2:D2}.{3:D3}",
                                                          playlistAngleLength.Hours,
                                                          playlistAngleLength.Minutes,
                                                          playlistAngleLength.Seconds,
                                                          playlistAngleLength.Milliseconds);

              string totalAngleSize = string.Format(
                                                          "{0:N0}", playlist.TotalAngleSize);

              string totalAngleBitrate = string.Format(
                                                          "{0:F2}",
                                                          Math.Round((double)playlist.TotalAngleBitRate / 10000) / 100);

              List<string> angleLengths = new List<string>();
              List<string> angleSizes = new List<string>();
              List<string> angleBitrates = new List<string>();
              List<string> angleTotalLengths = new List<string>();
              List<string> angleTotalSizes = new List<string>();
              List<string> angleTotalBitrates = new List<string>();

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

                  angleSizes.Add(string.Format("{0:N0}", angleSize));

                  TimeSpan angleTimeSpan = new TimeSpan((long)(angleLength * 10000000));

                  angleLengths.Add(string.Format(
                                                  "{0:D1}:{1:D2}:{2:D2}.{3:D3}",
                                                  angleTimeSpan.Hours,
                                                  angleTimeSpan.Minutes,
                                                  angleTimeSpan.Seconds,
                                                  angleTimeSpan.Milliseconds));

                  angleTotalSizes.Add(string.Format("{0:N0}", angleTotalSize));

                  angleTotalLengths.Add(totalLength);

                  double angleBitrate = 0;
                  if (angleLength > 0)
                  {
                    angleBitrate = Math.Round((double)(angleSize * 8) / angleLength / 10000) / 100;
                  }
                  angleBitrates.Add(string.Format("{0:F2}", angleBitrate));

                  double angleTotalBitrate = 0;
                  if (playlist.TotalLength > 0)
                  {
                    angleTotalBitrate = Math.Round((double)(angleTotalSize * 8) / playlist.TotalLength / 10000) / 100;
                  }
                  angleTotalBitrates.Add(string.Format("{0:F2}", angleTotalBitrate));
                }
              }

              string videoCodec = "";
              string videoBitrate = "";
              if (playlist.VideoStreams.Count > 0)
              {
                TSStream videoStream = playlist.VideoStreams[0];
                videoCodec = videoStream.CodecAltName;
                videoBitrate = string.Format("{0:F2}", Math.Round((double)videoStream.BitRate / 10000) / 100);
              }

              string audio1 = "";
              string languageCode1 = "";
              if (playlist.AudioStreams.Count > 0)
              {
                TSAudioStream audioStream = playlist.AudioStreams[0];

                languageCode1 = audioStream.LanguageCode;

                audio1 = string.Format("{0} {1}", audioStream.CodecAltName, audioStream.ChannelDescription);

                if (audioStream.BitRate > 0)
                {
                  audio1 += string.Format(
                                              " {0}Kbps",
                                              (int)Math.Round((double)audioStream.BitRate / 1000));
                }

                if (audioStream.SampleRate > 0 &&
                    audioStream.BitDepth > 0)
                {
                  audio1 += string.Format(
                                              " ({0}kHz/{1}-bit)",
                                              (int)Math.Round((double)audioStream.SampleRate / 1000),
                                              audioStream.BitDepth);
                }
              }

              string audio2 = "";
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
                    audio2 = string.Format(
                                                    "{0} {1}",
                                                    audioStream.CodecAltName, audioStream.ChannelDescription);

                    if (audioStream.BitRate > 0)
                    {
                      audio2 += string.Format(
                          " {0}Kbps",
                          (int)Math.Round((double)audioStream.BitRate / 1000));
                    }

                    if (audioStream.SampleRate > 0 &&
                        audioStream.BitDepth > 0)
                    {
                      audio2 += string.Format(
                                                  " ({0}kHz/{1}-bit)",
                                                  (int)Math.Round((double)audioStream.SampleRate / 1000),
                                                  audioStream.BitDepth);
                    }
                    break;
                  }
                }
              }

              report.Add("\r\n");
              report.Add("********************\r\n");
              report.Add("PLAYLIST: " + playlist.Name + "\r\n");
              report.Add("********************\r\n");
              report.Add("\r\n");
              report.Add("<--- BEGIN FORUMS PASTE --->\r\n");
              report.Add("[code]\r\n");

              report.Add(string.Format(
                                      "{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}\r\n",
                                      "",
                                      "",
                                      "",
                                      "",
                                      "",
                                      "Total",
                                      "Video",
                                      "",
                                      ""));

              report.Add(string.Format(
                                      "{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}\r\n",
                                      "Title",
                                      "Codec",
                                      "Length",
                                      "Movie Size",
                                      "Disc Size",
                                      "Bitrate",
                                      "Bitrate",
                                      "Main Audio Track",
                                      "Secondary Audio Track"));

              report.Add(string.Format(
                                      "{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}\r\n",
                                      "-----",
                                      "------",
                                      "-------",
                                      "--------------",
                                      "--------------",
                                      "-------",
                                      "-------",
                                      "------------------",
                                      "---------------------"));

              report.Add(string.Format(
                                      "{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}\r\n",
                                      title,
                                      videoCodec,
                                      totalLengthShort,
                                      totalSize,
                                      discSize,
                                      totalBitrate,
                                      videoBitrate,
                                      audio1,
                                      audio2));

              report.Add("[/code]\r\n");
              report.Add("\r\n");
              report.Add("[code]\r\n");

              report.Add("\r\n");
              report.Add("DISC INFO:\r\n");
              report.Add("\r\n");

              if (!string.IsNullOrEmpty(_rom.DiscTitle))
                report.Add(string.Format(
                                        "{0,-16}{1}\r\n", "Disc Title:", _rom.DiscTitle));

              report.Add(string.Format(
                                      "{0,-16}{1}\r\n", "Disc Label:", _rom.VolumeLabel));

              report.Add(string.Format(
                                      "{0,-16}{1:N0} bytes\r\n", "Disc Size:", _rom.Size));

              report.Add(string.Format(
                                      "{0,-16}{1}\r\n", "Protection:", protection));

              if (extraFeatures.Count > 0)
              {
                report.Add(string.Format(
                                        "{0,-16}{1}\r\n", "Extras:", string.Join(", ", extraFeatures.ToArray())));
              }
              report.Add(string.Format(
                                      "{0,-16}{1}\r\n", "BDInfo:", PROD_VERSION));

              report.Add("\r\n");
              report.Add("PLAYLIST REPORT:\r\n");
              report.Add("\r\n");
              report.Add(string.Format(
                                      "{0,-24}{1}\r\n", "Name:", title));

              report.Add(string.Format(
                                      "{0,-24}{1} (h:m:s.ms)\r\n", "Length:", totalLength));

              report.Add(string.Format(
                                      "{0,-24}{1:N0} bytes\r\n", "Size:", totalSize));

              report.Add(string.Format(
                                      "{0,-24}{1} Mbps\r\n", "Total Bitrate:", totalBitrate));
              if (playlist.AngleCount > 0)
              {
                for (int angleIndex = 0; angleIndex < playlist.AngleCount; angleIndex++)
                {
                  report.Add("\r\n");
                  report.Add(string.Format(
                                          "{0,-24}{1} (h:m:s.ms) / {2} (h:m:s.ms)\r\n",
                                          string.Format("Angle {0} Length:", angleIndex + 1),
                                          angleLengths[angleIndex], angleTotalLengths[angleIndex]));

                  report.Add(string.Format(
                                          "{0,-24}{1:N0} bytes / {2:N0} bytes\r\n",
                                          string.Format("Angle {0} Size:", angleIndex + 1),
                                          angleSizes[angleIndex], angleTotalSizes[angleIndex]));

                  report.Add(string.Format(
                                          "{0,-24}{1} Mbps / {2} Mbps\r\n",
                                          string.Format("Angle {0} Total Bitrate:", angleIndex + 1),
                                          angleBitrates[angleIndex], angleTotalBitrates[angleIndex], angleIndex));
                }
                report.Add("\r\n");
                report.Add(string.Format(
                                        "{0,-24}{1} (h:m:s.ms)\r\n", "All Angles Length:", totalAngleLength));

                report.Add(string.Format(
                                        "{0,-24}{1} bytes\r\n", "All Angles Size:", totalAngleSize));

                report.Add(string.Format(
                                        "{0,-24}{1} Mbps\r\n", "All Angles Bitrate:", totalAngleBitrate));
              }
              /*
              report += string.Format(
                  "{0,-24}{1}\r\n", "Description:", "");
               */
              if (!string.IsNullOrEmpty(_rom.DiscTitle))
                summary += string.Format(
                                        "Disc Title: {0}\r\n", _rom.DiscTitle);

              summary += string.Format(
                                       "Disc Label: {0}\r\n", _rom.VolumeLabel);

              summary += string.Format(
                                       "Disc Size: {0:N0} bytes\r\n", _rom.Size);

              summary += string.Format(
                                       "Protection: {0}\r\n", protection);

              summary += string.Format(
                                       "Playlist: {0}\r\n", title);

              summary += string.Format(
                                       "Size: {0:N0} bytes\r\n", totalSize);

              summary += string.Format(
                                       "Length: {0}\r\n", totalLength);

              summary += string.Format(
                                       "Total Bitrate: {0} Mbps\r\n", totalBitrate);

              if (playlist.HasHiddenTracks)
              {
                report.Add("\r\n(*) Indicates included stream hidden by this playlist.\r\n");
              }

              if (playlist.VideoStreams.Count > 0)
              {
                report.Add("\r\n");
                report.Add("VIDEO:\r\n");
                report.Add("\r\n");
                report.Add(string.Format(
                                        "{0,-24}{1,-20}{2,-16}\r\n",
                                        "Codec",
                                        "Bitrate",
                                        "Description"));
                report.Add(string.Format(
                                        "{0,-24}{1,-20}{2,-16}\r\n",
                                        "-----",
                                        "-------",
                                        "-----------"));

                foreach (TSStream stream in playlist.SortedStreams)
                {
                  if (!stream.IsVideoStream) continue;

                  string streamName = stream.CodecName;
                  if (stream.AngleIndex > 0)
                  {
                    streamName += string.Format(
                                                " ({0})", stream.AngleIndex);
                  }

                  string streamBitrate = string.Format(
                                                      "{0:D}",
                                                      (int)Math.Round((double)stream.BitRate / 1000));
                  if (stream.AngleIndex > 0)
                  {
                    streamBitrate += string.Format(
                                                    " ({0:D})",
                                                    (int)Math.Round((double)stream.ActiveBitRate / 1000));
                  }
                  streamBitrate += " kbps";

                  report.Add(string.Format(
                                          "{0,-24}{1,-20}{2,-16}\r\n",
                                          (stream.IsHidden ? "* " : "") + streamName,
                                          streamBitrate,
                                          stream.Description));

                  summary += string.Format(
                                          (stream.IsHidden ? "* " : "") + "Video: {0} / {1} / {2}\r\n",
                                          streamName,
                                          streamBitrate,
                                          stream.Description);
                }
              }

              if (playlist.AudioStreams.Count > 0)
              {
                report.Add("\r\n");
                report.Add("AUDIO:\r\n");
                report.Add("\r\n");
                report.Add(string.Format(
                                        "{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
                                        "Codec",
                                        "Language",
                                        "Bitrate",
                                        "Description"));
                report.Add(string.Format(
                                        "{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
                                        "-----",
                                        "--------",
                                        "-------",
                                        "-----------"));

                foreach (TSStream stream in playlist.SortedStreams)
                {
                  if (!stream.IsAudioStream) continue;

                  string streamBitrate = string.Format(
                                                      "{0:D} kbps",
                                                      (int)Math.Round((double)stream.BitRate / 1000));

                  report.Add(string.Format(
                                          "{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
                                          (stream.IsHidden ? "* " : "") + stream.CodecName,
                                          stream.LanguageName,
                                          streamBitrate,
                                          stream.Description));

                  summary += string.Format(
                      (stream.IsHidden ? "* " : "") + "Audio: {0} / {1} / {2}\r\n",
                      stream.LanguageName,
                      stream.CodecName,
                      stream.Description);
                }
              }

              if (playlist.GraphicsStreams.Count > 0)
              {
                report.Add("\r\n");
                report.Add("SUBTITLES:\r\n");
                report.Add("\r\n");
                report.Add(string.Format(
                                        "{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
                                        "Codec",
                                        "Language",
                                        "Bitrate",
                                        "Description"));
                report.Add(string.Format(
                                        "{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
                                        "-----",
                                        "--------",
                                        "-------",
                                        "-----------"));

                foreach (TSStream stream in playlist.SortedStreams)
                {
                  if (!stream.IsGraphicsStream) continue;

                  string streamBitrate = string.Format(
                                                       "{0:F3} kbps",
                                                       (double)stream.BitRate / 1000);

                  report.Add(string.Format(
                                          "{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
                                          (stream.IsHidden ? "* " : "") + stream.CodecName,
                                          stream.LanguageName,
                                          streamBitrate,
                                          stream.Description));

                  summary += string.Format(
                                           (stream.IsHidden ? "* " : "") + "Subtitle: {0} / {1}\r\n",
                                           stream.LanguageName,
                                           streamBitrate,
                                           stream.Description);
                }
              }

              if (playlist.TextStreams.Count > 0)
              {
                report.Add("\r\n");
                report.Add("TEXT:\r\n");
                report.Add("\r\n");
                report.Add(string.Format(
                                        "{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
                                        "Codec",
                                        "Language",
                                        "Bitrate",
                                        "Description"));
                report.Add(string.Format(
                                        "{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
                                        "-----",
                                        "--------",
                                        "-------",
                                        "-----------"));

                foreach (TSStream stream in playlist.SortedStreams)
                {
                  if (!stream.IsTextStream) continue;

                  string streamBitrate = string.Format(
                                                       "{0:F3} kbps",
                                                       (double)stream.BitRate / 1000);

                  report.Add(string.Format(
                                          "{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
                                          (stream.IsHidden ? "* " : "") + stream.CodecName,
                                          stream.LanguageName,
                                          streamBitrate,
                                          stream.Description));
                }
              }

              report.Add("\r\n");
              report.Add("FILES:\r\n");
              report.Add("\r\n");
              report.Add(string.Format(
                                      "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}\r\n",
                                      "Name",
                                      "Time In",
                                      "Length",
                                      "Size",
                                      "Total Bitrate"));
              report.Add(string.Format(
                                      "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}\r\n",
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
                  clipName += string.Format(
                                            " ({0})", clip.AngleIndex);
                }

                string clipSize = string.Format(
                                                "{0:N0}", clip.PacketSize);

                TimeSpan clipInSpan =
                    new TimeSpan((long)(clip.RelativeTimeIn * 10000000));
                TimeSpan clipOutSpan =
                    new TimeSpan((long)(clip.RelativeTimeOut * 10000000));
                TimeSpan clipLengthSpan =
                    new TimeSpan((long)(clip.Length * 10000000));

                string clipTimeIn = string.Format(
                                                    "{0:D1}:{1:D2}:{2:D2}.{3:D3}",
                                                    clipInSpan.Hours,
                                                    clipInSpan.Minutes,
                                                    clipInSpan.Seconds,
                                                    clipInSpan.Milliseconds);
                string clipLength = string.Format(
                                                  "{0:D1}:{1:D2}:{2:D2}.{3:D3}",
                                                  clipLengthSpan.Hours,
                                                  clipLengthSpan.Minutes,
                                                  clipLengthSpan.Seconds,
                                                  clipLengthSpan.Milliseconds);

                string clipBitrate = Math.Round(
                    (double)clip.PacketBitRate / 1000).ToString("N0", CultureInfo.InvariantCulture);

                report.Add(string.Format(
                                        "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}\r\n",
                                        clipName,
                                        clipTimeIn,
                                        clipLength,
                                        clipSize,
                                        clipBitrate));
              }

              report.Add("\r\n");
              report.Add("CHAPTERS:\r\n");
              report.Add("\r\n");
              report.Add(string.Format(
                                      "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}{5,-16}{6,-16}{7,-16}{8,-16}{9,-16}{10,-16}{11,-16}{12,-16}\r\n",
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
              report.Add(string.Format(
                                      "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}{5,-16}{6,-16}{7,-16}{8,-16}{9,-16}{10,-16}{11,-16}{12,-16}\r\n",
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

              Queue<double> window1Bits = new Queue<double>();
              Queue<double> window1Seconds = new Queue<double>();
              double window1BitsSum = 0;
              double window1SecondsSum = 0;
              double window1PeakBitrate = 0;
              double window1PeakLocation = 0;

              Queue<double> window5Bits = new Queue<double>();
              Queue<double> window5Seconds = new Queue<double>();
              double window5BitsSum = 0;
              double window5SecondsSum = 0;
              double window5PeakBitrate = 0;
              double window5PeakLocation = 0;

              Queue<double> window10Bits = new Queue<double>();
              Queue<double> window10Seconds = new Queue<double>();
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

              ushort diagPID = playlist.VideoStreams[0].PID;

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

                  TimeSpan window1PeakSpan = new TimeSpan((long)(window1PeakLocation * 10000000));
                  TimeSpan window5PeakSpan = new TimeSpan((long)(window5PeakLocation * 10000000));
                  TimeSpan window10PeakSpan = new TimeSpan((long)(window10PeakLocation * 10000000));
                  TimeSpan chapterMaxFrameSpan = new TimeSpan((long)(chapterMaxFrameLocation * 10000000));
                  TimeSpan chapterStartSpan = new TimeSpan((long)(chapterStart * 10000000));
                  TimeSpan chapterEndSpan = new TimeSpan((long)(chapterEnd * 10000000));
                  TimeSpan chapterLengthSpan = new TimeSpan((long)(chapterLength * 10000000));

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

                  report.Add(string.Format(
                                          "{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}{5,-16}{6,-16}{7,-16}{8,-16}{9,-16}{10,-16}{11,-16}{12,-16}\r\n",
                                          chapterIndex,
                                          string.Format("{0:D1}:{1:D2}:{2:D2}.{3:D3}", chapterStartSpan.Hours, chapterStartSpan.Minutes, chapterStartSpan.Seconds, chapterStartSpan.Milliseconds),
                                          string.Format("{0:D1}:{1:D2}:{2:D2}.{3:D3}", chapterLengthSpan.Hours, chapterLengthSpan.Minutes, chapterLengthSpan.Seconds, chapterLengthSpan.Milliseconds),
                                          string.Format("{0:N0} kbps", Math.Round(chapterBitrate / 1000)),
                                          string.Format("{0:N0} kbps", Math.Round(window1PeakBitrate / 1000)),
                                          string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", window1PeakSpan.Hours, window1PeakSpan.Minutes, window1PeakSpan.Seconds, window1PeakSpan.Milliseconds),
                                          string.Format("{0:N0} kbps", Math.Round(window5PeakBitrate / 1000)),
                                          string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", window5PeakSpan.Hours, window5PeakSpan.Minutes, window5PeakSpan.Seconds, window5PeakSpan.Milliseconds),
                                          string.Format("{0:N0} kbps", Math.Round(window10PeakBitrate / 1000)),
                                          string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", window10PeakSpan.Hours, window10PeakSpan.Minutes, window10PeakSpan.Seconds, window10PeakSpan.Milliseconds),
                                          string.Format("{0:N0} bytes", chapterAvgFrameSize),
                                          string.Format("{0:N0} bytes", chapterMaxFrameSize),
                                          string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", chapterMaxFrameSpan.Hours, chapterMaxFrameSpan.Minutes, chapterMaxFrameSpan.Seconds, chapterMaxFrameSpan.Milliseconds)));

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

              if (BDInfoSettings.GenerateStreamDiagnostics)
              {
                report.Add("\r\n");
                report.Add("STREAM DIAGNOSTICS:\r\n");
                report.Add("\r\n");
                report.Add(string.Format(
                                        "{0,-16}{1,-16}{2,-16}{3,-16}{4,-24}{5,-24}{6,-24}{7,-16}{8,-16}\r\n",
                                        "File",
                                        "PID",
                                        "Type",
                                        "Codec",
                                        "Language",
                                        "Seconds",
                                        "Bitrate",
                                        "Bytes",
                                        "Packets"));
                report.Add(string.Format(
                                        "{0,-16}{1,-16}{2,-16}{3,-16}{4,-24}{5,-24}{6,-24}{7,-16}{8,-16}\r\n",
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

                Dictionary<string, TSStreamClip> reportedClips = new Dictionary<string, TSStreamClip>();
                foreach (TSStreamClip clip in playlist.StreamClips)
                {
                  if (clip.StreamFile == null) continue;
                  if (reportedClips.ContainsKey(clip.Name)) continue;
                  reportedClips[clip.Name] = clip;

                  string clipName = clip.DisplayName;
                  if (clip.AngleIndex > 0)
                  {
                    clipName += string.Format(" ({0})", clip.AngleIndex);
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
                      language = string.Format(
                          "{0} ({1})", playlistStream.LanguageCode, playlistStream.LanguageName);
                    }

                    report.Add(string.Format(
                                            "{0,-16}{1,-16}{2,-16}{3,-16}{4,-24}{5,-24}{6,-24}{7,-16}{8,-16}\r\n",
                                            clipName,
                                            string.Format("{0} (0x{1:X})", clipStream.PID, clipStream.PID),
                                            string.Format("0x{0:X2}", (byte)clipStream.StreamType),
                                            clipStream.CodecShortName,
                                            language,
                                            clipSeconds,
                                            clipBitRate,
                                            clipStream.PayloadBytes.ToString("N0", CultureInfo.InvariantCulture),
                                            clipStream.PacketCount.ToString("N0", CultureInfo.InvariantCulture)));
                  }
                }
              }

              report.Add("\r\n");
              report.Add("[/code]\r\n");
              report.Add("<---- END FORUMS PASTE ---->\r\n");
              report.Add("\r\n");

              if (BDInfoSettings.GenerateTextSummary)
              {
                report.Add("QUICK SUMMARY:\r\n\r\n");
                report.Add(summary);
                report.Add("\r\n");
              }

              GC.Collect();
            }

            if (BDInfoSettings.AutosaveReport && reportFile != null)
            {
              try { reportFile.Write(string.Join("", report)); }
              catch { }
            }
          }

          Console.WriteLine(string.Join("", report));
        }
      });
    }

    bool BDROM_StreamClipFileScanError(TSStreamClipFile streamClipFile, Exception ex)
    {
      Console.WriteLine("An error occurred while scanning the stream clip file {0}.\n\nThe disc may be copy-protected or damaged.\n\n", streamClipFile.Name);
      return false;
    }

    bool BDROM_StreamFileScanError(TSStreamFile streamFile, Exception ex)
    {
      Console.WriteLine("An error occurred while scanning the stream file {0}.\n\nThe disc may be copy-protected or damaged.\n\nDo you want to continue scanning the stream files?", streamFile.Name);
      return false;
    }

    bool BDROM_PlaylistFileScanError(TSPlaylistFile playlistFile, Exception ex)
    {
      Console.WriteLine("An error occurred while scanning the playlist file {0}.\n\nThe disc may be copy-protected or damaged.\n\nDo you want to continue scanning the playlist files?", playlistFile.Name);
      return false;
    }
  }
}