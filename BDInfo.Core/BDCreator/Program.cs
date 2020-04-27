using BDCommon;
using CommandLine;
using DiscUtils.Iso9660;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BDCreator
{
  class Program
  {
    static readonly char separator = Environment.OSVersion.Platform == PlatformID.Unix ? '/' : '\\';

    static void Main(string[] args)
    {
      Console.Clear();

      Parser.Default.ParseArguments<CmdOptions>(args)
        .WithParsed(opts => Exec(opts))
        .WithNotParsed((errs) => HandleParseError(errs));
    }

    static void Exec(CmdOptions opts)
    {
      if (!Directory.Exists(opts.Path))
      {
        Console.WriteLine($"{opts.Path} does not exist");
        return;
      }

      try
      {
        CDBuilder builder = new CDBuilder();
        BDROM rom = new BDROM(opts.Path, new DefaultSettings());
        rom.Scan();

        builder.UseJoliet = true;

        AddDir(opts.Path, builder, opts.Path);

        builder.Build(opts.Output);
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(ex.Message);
      }
    }

    static void HandleParseError(IEnumerable<Error> errs) { }

    static void AddDir(string dirPath, CDBuilder builder, string originalPath)
    {
      var files = Directory.GetFiles(dirPath);
      var dirs = Directory.GetDirectories(dirPath);
      var pathDiff = string.Join(separator, dirPath.Split(separator).Except(originalPath.Split(separator)));

      if (!string.IsNullOrWhiteSpace(pathDiff))
      {
        Console.WriteLine($"Adding directory {pathDiff}");
        builder.AddDirectory(pathDiff);
      }

      foreach (var file in files)
      {
        string location = string.IsNullOrWhiteSpace(pathDiff) ?
           Path.GetFileName(file) :
           $"{pathDiff}{separator}{Path.GetFileName(file)}";

        Console.WriteLine($"Adding file: {location}");
        builder.AddFile(location, file);
      }

      foreach (var dir in dirs)
      {
        AddDir(dir, builder, originalPath);
      }
    }
  }
}
