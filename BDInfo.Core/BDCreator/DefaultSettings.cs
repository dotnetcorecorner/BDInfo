using BDCommon;

namespace BDCreator
{
  public sealed class DefaultSettings : BDInfoSettings
  {
    public override bool GenerateStreamDiagnostics => true;

    public override bool ExtendedStreamDiagnostics => true;

    public override bool EnableSSIF => true;

    public override bool DisplayChapterCount => true;

    public override bool AutosaveReport => true;

    public override bool GenerateFrameDataFile => true;

    public override bool FilterLoopingPlaylists => true;

    public override bool FilterShortPlaylists => true;

    public override int FilterShortPlaylistsValue => 10;

    public override bool UseImagePrefix => true;

    public override string UseImagePrefixValue => "img-";

    public override bool KeepStreamOrder => true;

    public override bool GenerateTextSummary => true;

    public override string ReportPath => "";

    public override string ReportFileName => "";

    public override bool IncludeVersionAndNotes => true;

    public override bool PrintOnlyForBigPlaylist => true;

    public override bool PrintReportToConsole => true;

    public override bool GroupByTime => false;
  }
}
