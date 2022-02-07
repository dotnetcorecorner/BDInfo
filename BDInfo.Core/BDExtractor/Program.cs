using CommandLine;
using CommonStuff.Core;
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

		static void Main(string[] args)
		{

			Parser.Default.ParseArguments<CmdOptions>(args)
				.WithParsed(opts => Exec(opts))
				.WithNotParsed((errs) => HandleParseError(errs));
		}

		static void Exec(CmdOptions opts)
		{
			_log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"log_{Path.GetFileName(opts.Path)}.log");

			if (!Directory.Exists(opts.Output))
			{
				Directory.CreateDirectory(opts.Output);
			}

			try
			{
				using (FileStream isoStream = File.Open(opts.Path, FileMode.Open))
				{
					UdfReader cd = new UdfReader(isoStream);
					var dirs = cd.Root.GetDirectories();
					var files = cd.Root.GetFiles();

					foreach (var dir in dirs)
					{
						CopyDir(dir, opts.Output);
					}

					foreach (var file in files)
					{
						File.AppendAllText(_log, $"Exec::: {file.FullName + Environment.NewLine}");
						File.AppendAllText(_log, $"Exec::: {file.Name + Environment.NewLine}");

						var path = FolderUtility.Combine(opts.Output, file.Name);
						File.AppendAllText(_log, $"Exec::: {path + Environment.NewLine}");

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
			File.AppendAllText(_log, $"CopyDir::: {ddi.FullName + Environment.NewLine}");
			File.AppendAllText(_log, $"CopyDir::: {ddi.Name + Environment.NewLine}");

			string path = FolderUtility.Combine(outpath, ddi.FullName);
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
					File.AppendAllText(_log, $"CopyDir::: {file.FullName + Environment.NewLine}");
					File.AppendAllText(_log, $"CopyDir::: {file.Name + Environment.NewLine}");

					var filePath = FolderUtility.Combine(outpath, file.FullName);

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

		static void CopyFile(DiscFileInfo file, string filePath)
		{
			var fc = new FileCopier(file.FullName, filePath, 4096 * 1024);
			Console.WriteLine($"Creating file {filePath} ( {SizeConverter.SizeToText(file.Length)} ) { string.Join(' ', 10) }");

			fc.OnProgressChanged += (percentage) =>
			{
				Console.Write($"\rPercent: {percentage} %{new string(' ', 10)}");
			};

			fc.OnComplete += () =>
			{
				Console.WriteLine();
			};

			fc.Copy();
		}
	}
}
