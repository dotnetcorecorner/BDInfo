using BDCommon;
using System;
using System.IO;

namespace BDInfo
{
    public sealed class BDSettings : BDInfoSettings
    {
        private readonly CmdOptions _opts;

        public BDSettings(CmdOptions opts)
        {
            _opts = opts;
        }

        public override bool GenerateStreamDiagnostics => _opts?.GenerateStreamDiagnostics ?? true;

        public override bool ExtendedStreamDiagnostics => _opts?.ExtendedStreamDiagnostics ?? false;

        public override bool EnableSSIF => _opts?.EnableSSIF ?? true;

        public override bool DisplayChapterCount => _opts?.DisplayChapterCount ?? false;

        public override bool GenerateFrameDataFile => _opts?.GenerateFrameDataFile ?? false;

        public override bool FilterLoopingPlaylists => _opts?.FilterLoopingPlaylists ?? false;

        public override bool FilterShortPlaylists => _opts?.FilterShortPlaylists ?? true;

        public override int FilterShortPlaylistsValue => _opts?.FilterShortPlaylistsValue ?? 20;

        public override bool UseImagePrefix => _opts?.UseImagePrefix ?? false;

        public override string UseImagePrefixValue => _opts?.UseImagePrefixValue ?? "video-";

        public override bool KeepStreamOrder => _opts?.KeepStreamOrder ?? false;

        public override bool GenerateTextSummary => _opts?.GenerateTextSummary ?? true;

        public override string ReportFileName => string.IsNullOrWhiteSpace(_opts?.ReportFileName) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BDInfo_{0}.bdinfo") : _opts.ReportFileName;

        public override bool IncludeVersionAndNotes => _opts?.IncludeVersionAndNotes ?? false;

        public override bool GroupByTime => _opts?.GroupByTime ?? false;

        public bool IsExecutedAsScript => _opts?.IsScriptExecuted ?? false;
    }
}
