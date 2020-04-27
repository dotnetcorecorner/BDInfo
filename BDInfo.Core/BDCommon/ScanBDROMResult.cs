using System;
using System.Collections.Generic;

namespace BDCommon
{
  public class ScanBDROMResult
  {
    public Exception ScanException = new Exception("Scan has not been run.");
    public Dictionary<string, Exception> FileExceptions = new Dictionary<string, Exception>();
  }
}