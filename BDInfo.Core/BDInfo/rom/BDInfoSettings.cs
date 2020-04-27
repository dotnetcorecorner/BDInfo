//============================================================================
// BDInfo - Blu-ray Video and Audio Analysis Tool
// Copyright © 2010 Cinema Squid
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//=============================================================================

using System;

namespace BDInfo
{
  public static class BDInfoSettings
  {
    private static CmdOptions _opts;

    internal static void Load(CmdOptions opts)
    {
      _opts = opts;
    }

    public static bool GenerateStreamDiagnostics => _opts?.GenerateStreamDiagnostics ?? true;

    public static bool ExtendedStreamDiagnostics => _opts?.ExtendedStreamDiagnostics ?? false;

    public static bool EnableSSIF => _opts?.EnableSSIF ?? true;

    public static bool DisplayChapterCount => _opts?.DisplayChapterCount ?? false;

    public static bool AutosaveReport => _opts?.AutosaveReport ?? true;

    public static bool GenerateFrameDataFile => _opts?.GenerateFrameDataFile ?? false;

    public static bool FilterLoopingPlaylists => _opts?.FilterLoopingPlaylists ?? true;

    public static bool FilterShortPlaylists => _opts?.FilterShortPlaylists ?? true;

    public static int FilterShortPlaylistsValue => _opts?.FilterShortPlaylistsValue ?? 20;

    public static bool UseImagePrefix => _opts?.UseImagePrefix ?? false;

    public static string UseImagePrefixValue => _opts?.UseImagePrefixValue ?? "video-";

    public static bool KeepStreamOrder => _opts?.KeepStreamOrder ?? true;

    public static bool GenerateTextSummary => _opts?.GenerateTextSummary ?? true;

    public static string ReportPath => _opts?.ReportPath ?? AppDomain.CurrentDomain.BaseDirectory;

    public static string ReportFileName => _opts?.ReportFileName ?? "report_{0}.txt";

    public static bool IncludeVersionAndNotes => _opts?.IncludeVersionAndNotes ?? false;

    public static bool PrintOnlyForBigPlaylist => _opts?.PrintOnlyForBigPlaylist ?? true;

    public static bool PrintReportToConsole => _opts?.PrintReportToConsole ?? false;
  }
}