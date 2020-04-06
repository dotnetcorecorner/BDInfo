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

namespace BDInfo.Core
{
  public class BDInfoSettings
  {
    public static bool GenerateStreamDiagnostics => true;

    public static bool ExtendedStreamDiagnostics => false;

    public static bool EnableSSIF => true;

    public static bool DisplayChapterCount => false;

    public static bool AutosaveReport => true;

    public static bool GenerateFrameDataFile => false;

    public static bool FilterLoopingPlaylists => true;

    public static bool FilterShortPlaylists => true;

    public static int FilterShortPlaylistsValue => 20;

    public static bool UseImagePrefix => false;

    public static string UseImagePrefixValue => "video-";

    public static bool KeepStreamOrder => true;

    public static bool GenerateTextSummary => true;

    public static string LastPath => string.Empty;
  }
}