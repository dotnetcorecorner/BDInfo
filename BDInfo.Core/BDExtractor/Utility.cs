using System.IO;

namespace BDExtractor
{
  internal static class Utility
  {
    public static string PathCombine(string path1, string path2)
    {
      string path = Path.Combine(path1, path2);

      if (!OperatingSystem.IsWindows())
      {
        path = path.Replace('\\', '/');
      }

      return path;
    }
  }
}
