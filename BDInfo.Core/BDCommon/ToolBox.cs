using System;
using System.Text;

namespace BDCommon
{
    public class ToolBox
    {
        public static string FormatFileSize(double fSize, bool formatHR = false)
        {
            if (fSize <= 0) return "0";
            var units = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

            var digitGroups = 0;
            if (formatHR)
                digitGroups = (int)(Math.Log10(fSize) / Math.Log10(1024));

            return FormattableString.Invariant($"{fSize / Math.Pow(1024, digitGroups):N2} {units[digitGroups]}");
        }

        public static string ReadString(byte[] data, int count, ref int pos)
        {
            string val = Encoding.ASCII.GetString(data, pos, count);
            pos += count;
            return val;
        }
    }
}
