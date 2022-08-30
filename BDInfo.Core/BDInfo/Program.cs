﻿using BDCommon;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace BDInfo
{
	internal class Program
	{
		private static BDROM BDROM = null;
		private static ScanBDROMResult ScanResult = null;

		private static readonly string ProductVersion = "0.7.5.6";
		private static BDSettings _bdinfoSettings;
		private static readonly string BDMV = "BDMV";

		private static string _error;

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
				_bdinfoSettings = new BDSettings(opts);

				if (!opts.Path.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
				{
					var subItems = Directory.GetDirectories(opts.Path, BDMV, SearchOption.AllDirectories);
					bool isIsoLevel = false;

					if (!subItems.Any())
					{
						var di = new DirectoryInfo(opts.Path);
						var files = di.GetFiles("*.*", SearchOption.AllDirectories);
						subItems = files.Where(s => s.FullName.EndsWith(".iso", StringComparison.OrdinalIgnoreCase)).Select(s => s.FullName).ToArray();
						isIsoLevel = subItems.LongLength > 0;
					}

					if (subItems.Any())
					{
						var oldOpt = Cloner.Clone(opts);
						List<string> reports = new List<string>();

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
								File.Move(reports[0], bigReport);
								return;
							}

							foreach (var report in reports)
							{
								File.AppendAllLines(bigReport, File.ReadAllLines(report));
								File.AppendAllLines(bigReport, Enumerable.Repeat(Environment.NewLine, 5));

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
			if (BDROM != null && BDROM.IsImage && BDROM.CdReader != null)
			{
				BDROM.CloseDiscImage();
			}

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

			if (BDROM.IsImage)
			{
				Console.WriteLine($"Detected BDMV Folder: {BDROM.DiscDirectoryBDMV.FullName}");
				Console.WriteLine($"Disc Title: {BDROM.DiscTitle}");
				Console.WriteLine($"Disc Label: {BDROM.VolumeLabel}");
				Console.WriteLine($"ISO Image: {BDROM.DiscTitle}");
			}
			else
			{
				Console.WriteLine($"Detected BDMV Folder: {BDROM.DirectoryBDMV.FullName}");
				Console.WriteLine($"Disc Title: {BDROM.DiscTitle}");
				Console.WriteLine($"Disc Label: {BDROM.VolumeLabel}");
			}

			List<string> features = new List<string>();
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
				Console.WriteLine($"Detected Features: {string.Join(", ", features.ToArray())}");
			}

			Console.WriteLine($"Disc Size: {BDROM.Size:N0} bytes ({ToolBox.FormatFileSize(BDROM.Size)})");
			Console.WriteLine();
		}

		private static void ScanBDROM()
		{
			if (BDROM is null)
			{
				throw new Exception("BDROM is null");
			}

			List<TSStreamFile> streamFiles = new List<TSStreamFile>(BDROM.StreamFiles.Values);

			ScanBDROMWork(streamFiles);
			ScanBDROMCompleted();
		}

		private static void ScanBDROMWork(List<TSStreamFile> streamFiles)
		{
			ScanResult = new ScanBDROMResult { ScanException = new Exception("Scan is still running.") };

			Timer timer = null;
			try
			{
				ScanBDROMState scanState = new ScanBDROMState();
				scanState.OnReportChange += ScanBDROMProgress;

				foreach (TSStreamFile streamFile in streamFiles)
				{
					if (_bdinfoSettings.EnableSSIF &&
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

					Thread thread = new Thread(ScanBDROMThread);
					thread.Start(scanState);
					while (thread.IsAlive)
					{
						Thread.Sleep(10);
					}

					if (streamFile.FileInfo != null)
						scanState.FinishedBytes += streamFile.FileInfo.Length;
					else
						scanState.FinishedBytes += streamFile.DFileInfo.Length;
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
				else {
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
				if (_bdinfoSettings.AutosaveReport)
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
			if (_bdinfoSettings.PrintReportToConsole)
			{
				Console.WriteLine("Please wait while we generate the report...");
			}
			else
			{
				Console.WriteLine("Done !");
			}

			IEnumerable<TSPlaylistFile> playlists = BDROM.PlaylistFiles.OrderByDescending(s => s.Value.FileSize).Select(s => s.Value);

			if (_bdinfoSettings.PrintOnlyForBigPlaylist)
			{
				playlists = playlists.Take(2);
			}

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

			string report = "";
			string protection = BDROM.IsBDPlus ? "BD+" : (BDROM.IsUHD ? "AACS2" : "AACS");

			if (!string.IsNullOrEmpty(BDROM.DiscTitle))
				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-16}{1}\r\n", "Disc Title:",
																BDROM.DiscTitle);
			report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-16}{1}\r\n", "Disc Label:",
																	BDROM.VolumeLabel);
			report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-16}{1:N0} bytes\r\n", "Disc Size:",
																	BDROM.Size);
			report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-16}{1}\r\n", "Protection:",
																	protection);

			List<string> extraFeatures = new List<string>();
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
				report += string.Format(CultureInfo.InvariantCulture,
																		"{0,-16}{1}\r\n", "Extras:",
																		string.Join(", ", extraFeatures.ToArray()));
			}
			report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-16}{1}\r\n", "BDInfo:",
																	ProductVersion);

			report += "\r\n";

			if (_bdinfoSettings.IncludeVersionAndNotes)
			{
				report += string.Format(CultureInfo.InvariantCulture,
																		"{0,-16}{1}\r\n", "Notes:", "");
				report += "\r\n";
				report += "BDINFO HOME:\r\n";
				report += "  Cinema Squid (old)\r\n";
				report += "    http://www.cinemasquid.com/blu-ray/tools/bdinfo\r\n";
				report += "  UniqProject GitHub (new)\r\n";
				report += "   https://github.com/UniqProject/BDInfo\r\n";
				report += "\r\n";
				report += "INCLUDES FORUMS REPORT FOR:\r\n";
				report += "  AVS Forum Blu-ray Audio and Video Specifications Thread\r\n";
				report += "    http://www.avsforum.com/avs-vb/showthread.php?t=1155731\r\n";
				report += "\r\n";
			}

			if (scanResult.ScanException != null)
			{
				report += string.Format(CultureInfo.InvariantCulture,
																		"WARNING: Report is incomplete because: {0}\r\n",
																		scanResult.ScanException.Message);
			}
			if (scanResult.FileExceptions.Count > 0)
			{
				report += "WARNING: File errors were encountered during scan:\r\n";
				foreach (string fileName in scanResult.FileExceptions.Keys)
				{
					Exception fileException = scanResult.FileExceptions[fileName];
					report += string.Format(CultureInfo.InvariantCulture,
																			"\r\n{0}\t{1}\r\n",
																			fileName, fileException.Message);
					report += string.Format(CultureInfo.InvariantCulture,
																			"{0}\r\n",
																			fileException.StackTrace);
				}
			}

			string separator = new string('#', 10);
			var tmp = reportName;

			foreach (TSPlaylistFile playlist in playlists.Where(pl => !_bdinfoSettings.FilterLoopingPlaylists || pl.IsValid))
			{
				string summary = "";
				string title = playlist.Name;
				string discSize = string.Format(CultureInfo.InvariantCulture,
																										"{0:N0}", BDROM.Size);

				TimeSpan playlistTotalLength =
						new TimeSpan((long)(playlist.TotalLength * 10000000));

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

				TimeSpan playlistAngleLength = new TimeSpan((long)(playlist.TotalAngleLength * 10000000));

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

						angleSizes.Add(string.Format(CultureInfo.InvariantCulture, "{0:N0}", angleSize));

						TimeSpan angleTimeSpan = new TimeSpan((long)(angleLength * 10000000));

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

				string audio1 = "";
				string languageCode1 = "";
				if (playlist.AudioStreams.Count > 0)
				{
					TSAudioStream audioStream = playlist.AudioStreams[0];

					languageCode1 = audioStream.LanguageCode;

					audio1 = string.Format(CultureInfo.InvariantCulture, "{0} {1}", audioStream.CodecAltName, audioStream.ChannelDescription);

					if (audioStream.BitRate > 0)
					{
						audio1 += string.Format(CultureInfo.InvariantCulture,
																				" {0}Kbps",
																				(int)Math.Round((double)audioStream.BitRate / 1000));
					}

					if (audioStream.SampleRate > 0 &&
							audioStream.BitDepth > 0)
					{
						audio1 += string.Format(CultureInfo.InvariantCulture,
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
							audio2 = string.Format(CultureInfo.InvariantCulture,
																							"{0} {1}",
																							audioStream.CodecAltName, audioStream.ChannelDescription);

							if (audioStream.BitRate > 0)
							{
								audio2 += string.Format(CultureInfo.InvariantCulture,
										" {0}Kbps",
										(int)Math.Round((double)audioStream.BitRate / 1000));
							}

							if (audioStream.SampleRate > 0 &&
									audioStream.BitDepth > 0)
							{
								audio2 += string.Format(CultureInfo.InvariantCulture,
																						" ({0}kHz/{1}-bit)",
																						(int)Math.Round((double)audioStream.SampleRate / 1000),
																						audioStream.BitDepth);
							}
							break;
						}
					}
				}

				report += "\r\n";
				report += "********************\r\n";
				report += "PLAYLIST: " + playlist.Name + "\r\n";
				report += "********************\r\n";
				report += "\r\n";
				report += "<--- BEGIN FORUMS PASTE --->\r\n";
				report += "[code]\r\n";

				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}\r\n",
																"",
																"",
																"",
																"",
																"",
																"Total",
																"Video",
																"",
																"");

				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}\r\n",
																"Title",
																"Codec",
																"Length",
																"Movie Size",
																"Disc Size",
																"Bitrate",
																"Bitrate",
																"Main Audio Track",
																"Secondary Audio Track");

				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}\r\n",
																"-----",
																"------",
																"-------",
																"--------------",
																"--------------",
																"-------",
																"-------",
																"------------------",
																"---------------------");

				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-64}{1,-8}{2,-8}{3,-16}{4,-16}{5,-8}{6,-8}{7,-42}{8}\r\n",
																title,
																videoCodec,
																totalLengthShort,
																totalSize,
																discSize,
																totalBitrate,
																videoBitrate,
																audio1,
																audio2);

				report += "[/code]\r\n";
				report += "\r\n";
				report += "[code]\r\n";

				if (_bdinfoSettings.GroupByTime)
				{
					report += $"\r\n{separator}Start group {playlistTotalLength.TotalMilliseconds}{separator}\r\n";
				}

				report += "\r\n";
				report += "DISC INFO:\r\n";
				report += "\r\n";

				if (!string.IsNullOrEmpty(BDROM.DiscTitle))
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-16}{1}\r\n", "Disc Title:", BDROM.DiscTitle);

				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-16}{1}\r\n", "Disc Label:", BDROM.VolumeLabel);

				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-16}{1:N0} bytes\r\n", "Disc Size:", BDROM.Size);

				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-16}{1}\r\n", "Protection:", protection);

				if (extraFeatures.Count > 0)
				{
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-16}{1}\r\n", "Extras:", string.Join(", ", extraFeatures.ToArray()));
				}
				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-16}{1}\r\n", "BDInfo:", ProductVersion);

				report += "\r\n";
				report += "PLAYLIST REPORT:\r\n";
				report += "\r\n";
				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-24}{1}\r\n", "Name:", title);

				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-24}{1} (h:m:s.ms)\r\n", "Length:", totalLength);

				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-24}{1:N0} bytes\r\n", "Size:", totalSize);

				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-24}{1} Mbps\r\n", "Total Bitrate:", totalBitrate);
				if (playlist.AngleCount > 0)
				{
					for (int angleIndex = 0; angleIndex < playlist.AngleCount; angleIndex++)
					{
						report += "\r\n";
						report += string.Format(CultureInfo.InvariantCulture,
																		"{0,-24}{1} (h:m:s.ms) / {2} (h:m:s.ms)\r\n",
																		string.Format(CultureInfo.InvariantCulture, "Angle {0} Length:", angleIndex + 1),
																		angleLengths[angleIndex], angleTotalLengths[angleIndex]);

						report += string.Format(CultureInfo.InvariantCulture,
																		"{0,-24}{1:N0} bytes / {2:N0} bytes\r\n",
																		string.Format(CultureInfo.InvariantCulture, "Angle {0} Size:", angleIndex + 1),
																		angleSizes[angleIndex], angleTotalSizes[angleIndex]);

						report += string.Format(CultureInfo.InvariantCulture,
																		"{0,-24}{1} Mbps / {2} Mbps\r\n",
																		string.Format(CultureInfo.InvariantCulture, "Angle {0} Total Bitrate:", angleIndex + 1),
																		angleBitrates[angleIndex], angleTotalBitrates[angleIndex], angleIndex);
					}
					report += "\r\n";
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-24}{1} (h:m:s.ms)\r\n", "All Angles Length:", totalAngleLength);

					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-24}{1} bytes\r\n", "All Angles Size:", totalAngleSize);

					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-24}{1} Mbps\r\n", "All Angles Bitrate:", totalAngleBitrate);
				}

				if (!string.IsNullOrEmpty(BDROM.DiscTitle))
					summary += string.Format(CultureInfo.InvariantCulture,
																	"Disc Title: {0}\r\n", BDROM.DiscTitle);

				summary += string.Format(CultureInfo.InvariantCulture,
																 "Disc Label: {0}\r\n", BDROM.VolumeLabel);

				summary += string.Format(CultureInfo.InvariantCulture,
																 "Disc Size: {0:N0} bytes\r\n", BDROM.Size);

				summary += string.Format(CultureInfo.InvariantCulture,
																 "Protection: {0}\r\n", protection);

				summary += string.Format(CultureInfo.InvariantCulture,
																 "Playlist: {0}\r\n", title);

				summary += string.Format(CultureInfo.InvariantCulture,
																 "Size: {0:N0} bytes\r\n", totalSize);

				summary += string.Format(CultureInfo.InvariantCulture,
																 "Length: {0}\r\n", totalLength);

				summary += string.Format(CultureInfo.InvariantCulture,
																 "Total Bitrate: {0} Mbps\r\n", totalBitrate);

				if (playlist.HasHiddenTracks)
				{
					report += "\r\n(*) Indicates included stream hidden by this playlist.\r\n";
				}

				if (playlist.VideoStreams.Count > 0)
				{
					report += "\r\n";
					report += "VIDEO:\r\n";
					report += "\r\n";
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-24}{1,-20}{2,-16}\r\n",
																	"Codec",
																	"Bitrate",
																	"Description");
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-24}{1,-20}{2,-16}\r\n",
																	"-----",
																	"-------",
																	"-----------");

					foreach (TSStream stream in playlist.SortedStreams)
					{
						if (!stream.IsVideoStream) continue;

						string streamName = stream.CodecName;
						if (stream.AngleIndex > 0)
						{
							streamName += string.Format(CultureInfo.InvariantCulture,
																					" ({0})", stream.AngleIndex);
						}

						string streamBitrate = string.Format(CultureInfo.InvariantCulture,
																								"{0:D}",
																								(int)Math.Round((double)stream.BitRate / 1000));
						if (stream.AngleIndex > 0)
						{
							streamBitrate += string.Format(CultureInfo.InvariantCulture,
																							" ({0:D})",
																							(int)Math.Round((double)stream.ActiveBitRate / 1000));
						}
						streamBitrate += " kbps";

						report += string.Format(CultureInfo.InvariantCulture,
																		"{0,-24}{1,-20}{2,-16}\r\n",
																		(stream.IsHidden ? "* " : "") + streamName,
																		streamBitrate,
																		stream.Description);

						summary += string.Format(CultureInfo.InvariantCulture,
																		(stream.IsHidden ? "* " : "") + "Video: {0} / {1} / {2}\r\n",
																		streamName,
																		streamBitrate,
																		stream.Description);
					}
				}

				if (playlist.AudioStreams.Count > 0)
				{
					report += "\r\n";
					report += "AUDIO:\r\n";
					report += "\r\n";
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
																	"Codec",
																	"Language",
																	"Bitrate",
																	"Description");
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
																	"-----",
																	"--------",
																	"-------",
																	"-----------");

					foreach (TSStream stream in playlist.SortedStreams)
					{
						if (!stream.IsAudioStream) continue;

						string streamBitrate = string.Format(CultureInfo.InvariantCulture,
																								"{0:D} kbps",
																								(int)Math.Round((double)stream.BitRate / 1000));

						report += string.Format(CultureInfo.InvariantCulture,
																		"{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
																		(stream.IsHidden ? "* " : "") + stream.CodecName,
																		stream.LanguageName,
																		streamBitrate,
																		stream.Description);

						summary += string.Format(
								(stream.IsHidden ? "* " : "") + "Audio: {0} / {1} / {2}\r\n",
								stream.LanguageName,
								stream.CodecName,
								stream.Description);
					}
				}

				if (playlist.GraphicsStreams.Count > 0)
				{
					report += "\r\n";
					report += "SUBTITLES:\r\n";
					report += "\r\n";
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
																	"Codec",
																	"Language",
																	"Bitrate",
																	"Description");
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
																	"-----",
																	"--------",
																	"-------",
																	"-----------");

					foreach (TSStream stream in playlist.SortedStreams)
					{
						if (!stream.IsGraphicsStream) continue;

						string streamBitrate = string.Format(CultureInfo.InvariantCulture,
																								 "{0:F3} kbps",
																								 (double)stream.BitRate / 1000);

						report += string.Format(CultureInfo.InvariantCulture,
																		"{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
																		(stream.IsHidden ? "* " : "") + stream.CodecName,
																		stream.LanguageName,
																		streamBitrate,
																		stream.Description);

						summary += string.Format(CultureInfo.InvariantCulture,
																		 (stream.IsHidden ? "* " : "") + "Subtitle: {0} / {1}\r\n",
																		 stream.LanguageName,
																		 streamBitrate,
																		 stream.Description);
					}
				}

				if (playlist.TextStreams.Count > 0)
				{
					report += "\r\n";
					report += "TEXT:\r\n";
					report += "\r\n";
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
																	"Codec",
																	"Language",
																	"Bitrate",
																	"Description");
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
																	"-----",
																	"--------",
																	"-------",
																	"-----------");

					foreach (TSStream stream in playlist.SortedStreams)
					{
						if (!stream.IsTextStream) continue;

						string streamBitrate = string.Format(CultureInfo.InvariantCulture,
																								 "{0:F3} kbps",
																								 (double)stream.BitRate / 1000);

						report += string.Format(CultureInfo.InvariantCulture,
																		"{0,-32}{1,-16}{2,-16}{3,-16}\r\n",
																		(stream.IsHidden ? "* " : "") + stream.CodecName,
																		stream.LanguageName,
																		streamBitrate,
																		stream.Description);
					}
				}

				report += "\r\n";
				report += "FILES:\r\n";
				report += "\r\n";
				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}\r\n",
																"Name",
																"Time In",
																"Length",
																"Size",
																"Total Bitrate");
				report += string.Format(CultureInfo.InvariantCulture,
																"{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}\r\n",
																"----",
																"-------",
																"------",
																"----",
																"-------------");

				foreach (TSStreamClip clip in playlist.StreamClips)
				{
					string clipName = clip.DisplayName;

					if (clip.AngleIndex > 0)
					{
						clipName += string.Format(CultureInfo.InvariantCulture,
																			" ({0})", clip.AngleIndex);
					}

					string clipSize = string.Format(CultureInfo.InvariantCulture,
																					"{0:N0}", clip.PacketSize);

					TimeSpan clipInSpan =
							new TimeSpan((long)(clip.RelativeTimeIn * 10000000));
					TimeSpan clipOutSpan =
							new TimeSpan((long)(clip.RelativeTimeOut * 10000000));
					TimeSpan clipLengthSpan =
							new TimeSpan((long)(clip.Length * 10000000));

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

					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}\r\n",
																	clipName,
																	clipTimeIn,
																	clipLength,
																	clipSize,
																	clipBitrate);
				}

				if (_bdinfoSettings.GroupByTime)
				{
					report += "\r\n";
					report += separator + "End group" + separator;
					report += "\r\n\r\n";
				}

				report += "\r\n";
				report += "CHAPTERS:\r\n";
				report += "\r\n";
				report += string.Format(CultureInfo.InvariantCulture,
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
																"Max Frame Time");
				report += string.Format(CultureInfo.InvariantCulture,
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
																"--------------");

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

						report += string.Format(CultureInfo.InvariantCulture,
																		"{0,-16}{1,-16}{2,-16}{3,-16}{4,-16}{5,-16}{6,-16}{7,-16}{8,-16}{9,-16}{10,-16}{11,-16}{12,-16}\r\n",
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
																		string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}.{3:D3}", chapterMaxFrameSpan.Hours, chapterMaxFrameSpan.Minutes, chapterMaxFrameSpan.Seconds, chapterMaxFrameSpan.Milliseconds));

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
					report += "\r\n";
					report += "STREAM DIAGNOSTICS:\r\n";
					report += "\r\n";
					report += string.Format(CultureInfo.InvariantCulture,
																	"{0,-16}{1,-16}{2,-16}{3,-16}{4,-24}{5,-24}{6,-24}{7,-16}{8,-16}\r\n",
																	"File",
																	"PID",
																	"Type",
																	"Codec",
																	"Language",
																	"Seconds",
																	"Bitrate",
																	"Bytes",
																	"Packets");
					report += string.Format(CultureInfo.InvariantCulture,
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
																	"-------");

					Dictionary<string, TSStreamClip> reportedClips = new Dictionary<string, TSStreamClip>();
					foreach (TSStreamClip clip in playlist.StreamClips)
					{
						if (clip.StreamFile == null) continue;
						if (reportedClips.ContainsKey(clip.Name)) continue;
						reportedClips[clip.Name] = clip;

						string clipName = clip.DisplayName;
						if (clip.AngleIndex > 0)
						{
							clipName += string.Format(CultureInfo.InvariantCulture, " ({0})", clip.AngleIndex);
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

							report += string.Format(CultureInfo.InvariantCulture,
																			"{0,-16}{1,-16}{2,-16}{3,-16}{4,-24}{5,-24}{6,-24}{7,-16}{8,-16}\r\n",
																			clipName,
																			string.Format(CultureInfo.InvariantCulture, "{0} (0x{1:X})", clipStream.PID, clipStream.PID),
																			string.Format(CultureInfo.InvariantCulture, "0x{0:X2}", (byte)clipStream.StreamType),
																			clipStream.CodecShortName,
																			language,
																			clipSeconds,
																			clipBitRate,
																			clipStream.PayloadBytes.ToString("N0", CultureInfo.InvariantCulture),
																			clipStream.PacketCount.ToString("N0", CultureInfo.InvariantCulture));
						}
					}
				}

				report += "\r\n";
				report += "[/code]\r\n";
				report += "<---- END FORUMS PASTE ---->\r\n";
				report += "\r\n";

				if (_bdinfoSettings.GenerateTextSummary)
				{
					report += "QUICK SUMMARY:\r\n\r\n";
					report += summary;
					report += "\r\n";
				}

				File.AppendAllText(tmp, report + Environment.NewLine);
				GC.Collect();
			}

			if (_bdinfoSettings.AutosaveReport)
			{
				Console.WriteLine($"Saving bdinfo to {_bdinfoSettings.ReportFileName}");

				if (!tmp.Equals(_bdinfoSettings.ReportFileName))
				{
					if (!_bdinfoSettings.PrintReportToConsole)
					{
						File.Move(tmp, _bdinfoSettings.ReportFileName);
					}
					else
					{
						File.Copy(tmp, _bdinfoSettings.ReportFileName);
					}
				}
			}

			if (_bdinfoSettings.PrintReportToConsole)
			{
				var len = new FileInfo(tmp).Length;
				var mb = len / (1024 * 1024);

				if (len >= 10 * mb)
				{
					Console.WriteLine("Text to big to print to console ! Please use autosave set on true to save file on disk");
				}
			}

			if (!tmp.Equals(_bdinfoSettings.ReportFileName) && File.Exists(tmp))
			{
				File.Delete(tmp);
			}
		}
	}
}