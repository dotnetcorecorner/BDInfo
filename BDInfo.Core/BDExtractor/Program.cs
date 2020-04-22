﻿using CommandLine;
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

      using (FileStream isoStream = File.Open(opts.Path, FileMode.Open))
      {
        UdfReader cd = new UdfReader(isoStream);
        var dirs = cd.Root.GetDirectories();
        var files = cd.Root.GetFiles();

        foreach (var dir in dirs)
        {
          CopyDirs(dir, opts.Output);
        }

        foreach (var file in files)
        {
          var path = Utility.PathCombine(opts.Output, file.Name);
          file.CopyTo(path, true);
        }
      }
    }

    static void HandleParseError(IEnumerable<Error> errs) { }

    static void CopyDirs(DiscDirectoryInfo ddi, string outpath)
    {
      string path = Utility.PathCombine(outpath, ddi.FullName);
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
          var filePath = Utility.PathCombine(outpath, file.FullName);

          Console.WriteLine($"Creating file {filePath} ( {SizeConverter.SizeToText(file.Length)} )");

          if (File.Exists(filePath))
          {
            File.Delete(filePath);
          }

          using (FileStream fs = File.Create(filePath))
          {
            using (var fileStream = file.Open(FileMode.Open))
            {
              fileStream.CopyTo(fs);
            }
          }

          Console.WriteLine($"Creating file {filePath} ( {SizeConverter.SizeToText(file.Length)} )");
        }
      }

      var dirs = ddi.GetDirectories();
      if (dirs.Length > 0)
      {
        foreach (DiscDirectoryInfo dir in dirs)
        {
          CopyDirs(dir, outpath);
        }
      }
    }
  }
}
