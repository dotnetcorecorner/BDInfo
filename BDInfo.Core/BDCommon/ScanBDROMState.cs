using System;
using System.Collections.Generic;

namespace BDCommon
{
  public class ScanBDROMState
  {
    public long TotalBytes { get { return _tb; } set { _tb = value; OnReportChange?.Invoke(this); } }
    public long FinishedBytes { get { return _fb; } set { _fb = value; OnReportChange?.Invoke(this); } }
    public DateTime TimeStarted = DateTime.Now;
    public TSStreamFile StreamFile = null;
    public Dictionary<string, List<TSPlaylistFile>> PlaylistMap = new Dictionary<string, List<TSPlaylistFile>>();
    public Exception Exception = null;

    private long _tb;
    private long _fb;

    public event Action<ScanBDROMState> OnReportChange;
  }
}