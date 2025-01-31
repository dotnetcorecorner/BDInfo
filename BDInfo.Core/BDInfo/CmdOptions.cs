using CommandLine;
using System;

namespace BDInfo
{
    [Serializable]
    public sealed class CmdOptions
    {
        [Option('p', "path", Required = true, HelpText = "The path to iso or bluray folder")]
        public string Path { get; set; }

        [Option('g', "generatestreamdiagnostics", Required = false, Default = null, HelpText = "Generate the stream diagnostics")]
        public bool? GenerateStreamDiagnostics { get; set; }

        [Option('e', "extendedstreamdiagnostics", Required = false, Default = null, HelpText = "Generete the extended stream diagnostics")]
        public bool? ExtendedStreamDiagnostics { get; set; }

        [Option('b', "enablessif", Required = false, Default = null, HelpText = "Enable SSIF support")]
        public bool? EnableSSIF { get; set; }

        [Option('l', "filterloopingplaylists", Required = false, Default = null, HelpText = "Filter loopig playlist")]
        public bool? FilterLoopingPlaylists { get; set; }

        [Option('y', "filtershortplaylist", Required = false, Default = null, HelpText = "Filter short playlist")]
        public bool? FilterShortPlaylists { get; set; }

        [Option('v', "filtershortplaylistvalue", Required = false, Default = 20, HelpText = "Filter number of short playlist")]
        public int FilterShortPlaylistsValue { get; set; }

        [Option('k', "keepstreamorder", Required = false, Default = null, HelpText = "Keep stream order")]
        public bool? KeepStreamOrder { get; set; }

        [Option('m', "generatetextsummary", Required = false, Default = null, HelpText = "Generate summary")]
        public bool? GenerateTextSummary { get; set; }

        [Option('o', "reportfilename", Required = false, Default = null, HelpText = "The report filename with extension. If no extension provided then will append .txt at end of filename")]
        public string ReportFileName { get; set; }

        [Option('q', "includeversionandnotes", Required = false, Default = null, HelpText = "Include version and notes inside report")]
        public bool? IncludeVersionAndNotes { get; set; }

        [Option('j', "groupbytime", Required = false, Default = null, HelpText = "Group by time")]
        public bool? GroupByTime { get; set; }
    }
}
