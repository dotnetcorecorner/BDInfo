﻿using BDCommon;
using System;

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

    public override bool AutosaveReport => _opts?.AutosaveReport ?? true;

    public override bool GenerateFrameDataFile => _opts?.GenerateFrameDataFile ?? false;

    public override bool FilterLoopingPlaylists => _opts?.FilterLoopingPlaylists ?? true;

    public override bool FilterShortPlaylists => _opts?.FilterShortPlaylists ?? true;

    public override int FilterShortPlaylistsValue => _opts?.FilterShortPlaylistsValue ?? 20;

    public override bool UseImagePrefix => _opts?.UseImagePrefix ?? false;

    public override string UseImagePrefixValue => _opts?.UseImagePrefixValue ?? "video-";

    public override bool KeepStreamOrder => _opts?.KeepStreamOrder ?? true;

    public override bool GenerateTextSummary => _opts?.GenerateTextSummary ?? true;

    public override string ReportPath => _opts?.ReportPath ?? AppDomain.CurrentDomain.BaseDirectory;

    public override string ReportFileName => _opts?.ReportFileName ?? "report_{0}.txt";

    public override bool IncludeVersionAndNotes => _opts?.IncludeVersionAndNotes ?? false;

    public override bool PrintOnlyForBigPlaylist => _opts?.PrintOnlyForBigPlaylist ?? true;

    public override bool PrintReportToConsole => _opts?.PrintReportToConsole ?? false;
  }
}