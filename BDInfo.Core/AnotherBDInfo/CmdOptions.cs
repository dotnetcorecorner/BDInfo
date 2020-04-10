﻿using CommandLine;

namespace AnotherBDInfo
{
  public sealed class CmdOptions
  {
    [Option('p', "path", Required = true, HelpText = "The path to iso or bluray folder")]
    public string Path { get; set; }

    [Option('g', "generatestreamdiagnostics", Required = false, Default = true, HelpText = "Generate the stream diagnostics")]
    public bool GenerateStreamDiagnostics { get; set; }

    [Option('e', "extendedstreamdiagnostics", Required = false, Default = false, HelpText = "Generete the extended stream diagnostics")]
    public bool ExtendedStreamDiagnostics { get; set; }

    [Option('b', "enablessif", Required = false, Default = true, HelpText = "Enable SSIF support")]
    public bool EnableSSIF { get; set; }

    [Option('c', "displaychaptercount", Required = false, Default = false, HelpText = "Enable chapter count")]
    public bool DisplayChapterCount { get; set; }

    [Option('a', "autosavereport", Required = false, Default = true, HelpText = "Auto save report")]
    public bool AutosaveReport { get; set; }

    [Option('f', "generateframedatafile", Required = false, Default = false, HelpText = "Generate frame data file")]
    public bool GenerateFrameDataFile { get; set; }

    [Option('l', "filterloopingplaylists", Required = false, Default = true, HelpText = "Filter loopig playlist")]
    public bool FilterLoopingPlaylists { get; set; }

    [Option('y', "filtershortplaylist", Required = false, Default = true, HelpText = "Filter short playlist")]
    public bool FilterShortPlaylists { get; set; }

    [Option('v', "filtershortplaylistvalue", Required = false, Default = 20, HelpText = "Filter number of short playlist")]
    public int FilterShortPlaylistsValue { get; set; }

    [Option('i', "useimageprefix", Required = false, Default = false, HelpText = "Use image prefix")]
    public bool UseImagePrefix { get; set; }

    [Option('x', "useimageprefixvalue", Required = false, Default = "video-", HelpText = "Image prefix")]
    public string UseImagePrefixValue { get; set; }

    [Option('k', "keepstreamorder", Required = false, Default = true, HelpText = "Keep stream order")]
    public bool KeepStreamOrder { get; set; }

    [Option('m', "generatetextsummary", Required = false, Default = true, HelpText = "Generate summary")]
    public bool GenerateTextSummary { get; set; }

    [Option('r', "reportpath", Required = false, Default = null, HelpText = "The folder where report will be saved. If none provided then will be in same location with application")]
    public string ReportPath { get; set; }

    [Option('o', "reportfilename", Required = false, Default = null, HelpText = "The report filename with extension. If no extension provided then will append .txt at end of filename")]
    public string ReportFileName { get; set; }
  }
}