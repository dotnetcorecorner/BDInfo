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
    static void Main(string[] args)
    {
      Console.Clear();

      Parser.Default.ParseArguments<CmdOptions>(args)
        .WithParsed(opts => Exec(opts))
        .WithNotParsed((errs) => HandleParseError(errs));
    }

    static void Exec(CmdOptions opts)
    {
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
            var path = FolderUtility.Combine(opts.Output, file.Name);
            CopyFile(file, path);
          }
        }
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(ex.Message);
      }
    }

    static void HandleParseError(IEnumerable<Error> errs) { }

    static void CopyDir(DiscDirectoryInfo ddi, string outpath)
    {
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
      Console.WriteLine($"Creating file {filePath} ( {SizeConverter.SizeToText(file.Length)} )");

      fc.OnProgressChanged += (percentage) =>
      {
        Console.Write($"\rPercent: {percentage} %  ");
      };

      fc.OnComplete += () =>
      {
        Console.WriteLine();
      };

      using (FileStream fs = File.Create(filePath))
      {
        using (var fileStream = file.Open(FileMode.Open))
        {
          fileStream.CopyTo(fs);
        }
      }
    }
  }
}
