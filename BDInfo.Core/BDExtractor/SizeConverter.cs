using System;
using System.Globalization;

namespace BDExtractor
{
	internal static class SizeConverter
	{
		public static string SizeToText(double size)
		{
			var units = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
			const ushort divide = 1000;

			int power = size > 0 ? (int)Math.Floor(Math.Log(size, divide)) : 0;

			return $"{Math.Round(size / Math.Pow(divide, power), 2).ToString(CultureInfo.InvariantCulture)} {units[power]}";
		}
	}
}