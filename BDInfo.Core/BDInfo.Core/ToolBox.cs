using System;
using System.Globalization;
using System.Text;

namespace BDInfo.Core
{
  public class ToolBox
  {
    public static string FormatFileSize(double fSize)
    {
      if (fSize <= 0) return "0";
      var units = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

      var digitGroups = (int)(Math.Log10(fSize) / Math.Log10(1024));
      return string.Format(CultureInfo.InvariantCulture, "{0:N2} {1}", fSize / Math.Pow(1024, digitGroups), units[digitGroups]);
    }

    public static string ReadString(
        byte[] data,
        int count,
        ref int pos)
    {
      string val =
          Encoding.ASCII.GetString(data, pos, count);

      pos += count;

      return val;
    }
  }
}
