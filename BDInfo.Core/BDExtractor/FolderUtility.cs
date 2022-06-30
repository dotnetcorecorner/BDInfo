using System.IO;
using System.Runtime.InteropServices;

namespace BDExtractor
{
	internal static class FolderUtility
	{
		public static string Combine(string path1, params string[] paths)
		{
			string path = path1;

			foreach (var path2 in paths)
			{
				path = Path.Combine(path, path2);
			}

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				path = path.Replace('\\', '/');
			}

			return path;
		}
	}
}
