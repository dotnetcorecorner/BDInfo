using BDCommon;
using CommandLine;
using DiscUtils.Iso9660;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
        builder.UseJoliet = true;

        BDROM rom = new BDROM(opts.Path, new DefaultSettings());
        rom.Scan();

        AddDir(opts.Path, builder, opts.Path);

        Console.WriteLine("");
        Console.WriteLine($"Creating {opts.Output} file");

        string title = string.IsNullOrWhiteSpace(rom.DiscTitle) ? rom.VolumeLabel : rom.DiscTitle;
        var volumeIdentifier = Regex.Replace(title, @"\W+", "_").TrimEnd('_');
        volumeIdentifier = volumeIdentifier.Substring(0, Math.Min(volumeIdentifier.Length, 32));
        volumeIdentifier = Regex.Split(volumeIdentifier, "(2160|1080)(p|i)", RegexOptions.IgnoreCase)[0].TrimEnd('_');

        builder.VolumeIdentifier = volumeIdentifier.ToUpperInvariant();
        builder.Build(opts.Output);

        if (opts.Test)
        {
          bool err = false;

          try
          {
            rom = new BDROM(opts.Output, new DefaultSettings());
            rom.Scan();
          }
          catch (Exception ex)
          {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();

            err = true;
          }
          finally
          {
            if (rom.IsImage && rom.CdReader != null)
            {
              rom.CloseDiscImage();
            }

            if (opts.Delete && err)
            {
              Console.WriteLine($"Deleting {opts.Output}");
              File.Delete(opts.Output);
            }
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
