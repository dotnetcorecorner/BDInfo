using System;
using System.Globalization;

namespace BDExtractor
{
	internal static class SizeConverter
	{
    public static string SizeToText(double size)
    {
      var units = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
      int power = size > 0 ? (int)Math.Floor(Math.Log(size, 1024)) : 0;

      return $"{Math.Round(size / Math.Pow(1024, power), 2).ToString(CultureInfo.InvariantCulture)} {units[power]}";
    }
  }
}