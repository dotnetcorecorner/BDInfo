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

namespace BDCommon
{
  public abstract class BDInfoSettings
  {
    public virtual bool GenerateStreamDiagnostics => true;

    public virtual bool ExtendedStreamDiagnostics => false;

    public virtual bool EnableSSIF => true;

    public virtual bool DisplayChapterCount => false;

    public virtual bool AutosaveReport => true;

    public virtual bool GenerateFrameDataFile => false;

    public virtual bool FilterLoopingPlaylists => true;

    public virtual bool FilterShortPlaylists => true;

    public virtual int FilterShortPlaylistsValue => 20;

    public virtual bool UseImagePrefix => false;

    public virtual string UseImagePrefixValue => "video-";

    public virtual bool KeepStreamOrder => true;

    public virtual bool GenerateTextSummary => true;

    public virtual string ReportPath => AppDomain.CurrentDomain.BaseDirectory;

    public virtual string ReportFileName => "report_{0}.txt";

    public virtual bool IncludeVersionAndNotes => false;

    public virtual bool PrintOnlyForBigPlaylist => true;

    public virtual bool PrintReportToConsole => false;
  }
}