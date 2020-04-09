using System;
using System.Collections.Generic;

namespace AnotherBDInfo
{
  internal class ScanBDROMState
  {
    public long TotalBytes = 0;
    public long FinishedBytes = 0;
    public DateTime TimeStarted = DateTime.Now;
    public TSStreamFile StreamFile = null;
    public Dictionary<string, List<TSPlaylistFile>> PlaylistMap = new Dictionary<string, List<TSPlaylistFile>>();
    public Exception Exception = null;
  }
}