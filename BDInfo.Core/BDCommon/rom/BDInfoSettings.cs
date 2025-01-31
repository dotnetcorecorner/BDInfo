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

namespace BDCommon
{
    public abstract class BDInfoSettings
    {
        public abstract bool GenerateStreamDiagnostics { get; }

        public abstract bool ExtendedStreamDiagnostics { get; }

        public abstract bool EnableSSIF { get; }

        public abstract bool FilterLoopingPlaylists { get; }

        public abstract bool FilterShortPlaylists { get; }

        public abstract int FilterShortPlaylistsValue { get; }

        public abstract bool KeepStreamOrder { get; }

        public abstract bool GenerateTextSummary { get; }

        public abstract string ReportFileName { get; }

        public abstract bool IncludeVersionAndNotes { get; }

        public abstract bool GroupByTime { get; }
    }
}