using CommandLine;
using DiscUtils;
using DiscUtils.Udf;
using System;
using System.Collections.Generic;
using System.IO;

namespace BDExtractor
{
	class Program
	{
		private static string _log;
		private static string _space = new string(' ', 20);

		static void Main(string[] args)
		{
			Parser.Default.ParseArguments<CmdOptions>(args)
				.WithParsed(opts => Exec(opts))
				.WithNotParsed((errs) => HandleParseError(errs));
		}

		static void Exec(CmdOptions opts)
		{
			_log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"log_{Path.GetFileName(opts.Path)}.log");
			var output = opts.Output;
			if (string.IsNullOrWhiteSpace(output))
			{
				output = Path.GetDirectoryName(opts.Path);
			}

			if (!Directory.Exists(output))
			{
				Directory.CreateDirectory(output);
			}

			try
			{
				DiscUtils.Complete.SetupHelper.SetupComplete();
				DiscUtils.Containers.SetupHelper.SetupContainers();
				DiscUtils.FileSystems.SetupHelper.SetupFileSystems();
				DiscUtils.Transports.SetupHelper.SetupTransports();

				using (FileStream isoStream = File.Open(opts.Path, FileMode.Open))
				{
					UdfReader cd = new UdfReader(isoStream);
					File.AppendAllText(_log, $"Exec cd root full::: {cd.Root.FullName + Environment.NewLine}");
					File.AppendAllText(_log, $"Exec cd root name::: {cd.Root.Name + Environment.NewLine}");

					var dirs = cd.Root.GetDirectories();
					var files = cd.Root.GetFiles();

					foreach (var dir in dirs)
					{
						CopyDir(dir, output);
					}

					foreach (var file in files)
					{
						File.AppendAllText(_log, $"Exec file fullname::: {file.FullName + Environment.NewLine}");
						File.AppendAllText(_log, $"Exec file name::: {file.Name + Environment.NewLine}");

						var path = FolderUtility.Combine(output, file.FullName);
						File.AppendAllText(_log, $"Exec file combined path::: {path + Environment.NewLine}");

						CopyFile(file, path);
					}
				}
			}
			catch (Exception ex)
			{
				var cl = Console.ForegroundColor;

				Console.ForegroundColor = ConsoleColor.Red;
				Console.Error.WriteLine(ex.Message);

				Console.ForegroundColor = cl;

				Environment.Exit(-1);
			}
		}

		static void HandleParseError(IEnumerable<Error> errs) { }

		static void CopyDir(DiscDirectoryInfo ddi, string outpath)
		{
			File.AppendAllText(_log, $"CopyDir dir fullname::: {ddi.FullName + Environment.NewLine}");
			File.AppendAllText(_log, $"CopyDir dir name::: {ddi.Name + Environment.NewLine}");

			string path = FolderUtility.Combine(outpath, ddi.FullName);
			File.AppendAllText(_log, $"CopyDir combined dir path::: {path + Environment.NewLine}");
			if (!Directory.Exists(path))
			{
				Console.WriteLine($"Creating directory {path}");
				Directory.CreateDirectory(path);
			}

			var files = ddi.GetFiles();
			if (files.Length > 0)
			{
				foreach (DiscFileInfo file in files)
				{
					File.AppendAllText(_log, $"CopyDir fullname::: {file.FullName + Environment.NewLine}");
					File.AppendAllText(_log, $"CopyDir name::: {file.Name + Environment.NewLine}");

					var filePath = FolderUtility.Combine(outpath, file.FullName);
					File.AppendAllText(_log, $"CopyDir combined file path::: {filePath + Environment.NewLine}");

					if (File.Exists(filePath))
					{
						File.Delete(filePath);
					}

					CopyFile(file, filePath);
				}
			}

			var dirs = ddi.GetDirectories();
			if (dirs.Length > 0)
			{
				foreach (DiscDirectoryInfo dir in dirs)
				{
					CopyDir(dir, outpath);
				}
			}
		}

		static void CopyFile(DiscFileInfo file, string destFilePath)
		{
			var fc = new FileCopier(file, destFilePath, 4096 * 1024); // 4MB
			Console.WriteLine($"Creating file {destFilePath} ( {SizeConverter.SizeToText(file.Length)} ) { _space }");

			fc.OnProgressChanged += (percentage) =>
			{
				Console.Write($"\rPercent: {percentage:0.00} %{_space}");
			};

			fc.OnComplete += () =>
			{
				Console.WriteLine();
			};

			fc.Copy();

			//using (FileStream fs = File.Create(filePath))
			//{
			//	using (var fileStream = file.Open(FileMode.Open))
			//	{
			//		Console.WriteLine($"Creating file {filePath} ( {SizeConverter.SizeToText(file.Length)} ) { new string(' ', 10) }");
			//		fileStream.CopyTo(fs);
			//	}
			//}
		}
	}
}
