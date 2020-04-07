using CommandLine;

namespace BDInfo.Core
{
  public sealed class CmdOptions
  {
    [Option('p', "path", Required = true, HelpText = "The path to iso or bluray folder")]
    public string Path { get; set; }

    [Option('g', "generatestreamdiagnostics", Required = false, Default = true)]
    public bool GenerateStreamDiagnostics { get; set; }

    [Option('e', "extendedstreamdiagnostics", Required = false, Default = false)]
    public bool ExtendedStreamDiagnostics { get; set; }

    [Option('b', "enablessif", Required = false, Default = true)]
    public bool EnableSSIF { get; set; }

    [Option('c', "displaychaptercount", Required = false, Default = false)]
    public bool DisplayChapterCount { get; set; }

    [Option('a', "autosavereport", Required = false, Default = true)]
    public bool AutosaveReport { get; set; }

    [Option('f', "generateframedatafile", Required = false, Default = false)]
    public bool GenerateFrameDataFile { get; set; }

    [Option('l', "filterloopingplaylists", Required = false, Default = true)]
    public bool FilterLoopingPlaylists { get; set; }

    [Option('y', "filtershortplaylist", Required = false, Default = true)]
    public bool FilterShortPlaylists { get; set; }

    [Option('v', "filtershortplaylistvalue", Required = false, Default = 20)]
    public int FilterShortPlaylistsValue { get; set; }

    [Option('i', "useimageprefix", Required = false, Default = false)]
    public bool UseImagePrefix { get; set; }

    [Option('x', "useimageprefixvalue", Required = false, Default = "video-")]
    public string UseImagePrefixValue { get; set; }

    [Option('k', "keepstreamorder", Required = false, Default = true)]
    public bool KeepStreamOrder { get; set; }

    [Option('m', "generatetextsummary", Required = false, Default = true)]
    public bool GenerateTextSummary { get; set; }

    [Option('t', "lastpath", Required = false, Default = "")]
    public string LastPath { get; set; }

    [Option('r', "reportpath", Required = false, Default = null)]
    public string ReportPath { get; set; }
  }
}
