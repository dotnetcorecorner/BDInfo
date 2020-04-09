using System;
using System.Collections.Generic;

namespace AnotherBDInfo
{
  public class ScanBDROMResult
  {
    public Exception ScanException = new Exception("Scan has not been run.");
    public Dictionary<string, Exception> FileExceptions = new Dictionary<string, Exception>();
  }
}