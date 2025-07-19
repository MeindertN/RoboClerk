using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk.Tests
{
    public static class TestingHelpers
    {
        public static string ConvertFileName(string input)
        {
            StringBuilder sb = new StringBuilder(input);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (sb[1] == ':')
                {
                    sb[1] = sb[0];
                    sb[0] = '/';
                }
                sb.Replace('\\', '/');
                return sb.ToString();
            }
            else
            {
                return input;
            }
        }

        public static class LogCapture
        {
            public static List<string> CapturedWarnings { get; private set; } = new List<string>();
            public static List<string> CapturedErrors { get; private set; } = new List<string>();
            public static List<string> CapturedInfo { get; private set; } = new List<string>();

            public static void Clear()
            {
                CapturedWarnings.Clear();
                CapturedErrors.Clear();
                CapturedInfo.Clear();
            }

            public static void CaptureWarning(string message)
            {
                CapturedWarnings.Add(message);
            }

            public static void CaptureError(string message)
            {
                CapturedErrors.Add(message);
            }

            public static void CaptureInfo(string message)
            {
                CapturedInfo.Add(message);
            }

            public static bool ContainsWarning(string partialMessage)
            {
                return CapturedWarnings.Any(w => w.Contains(partialMessage));
            }

            public static bool ContainsError(string partialMessage)
            {
                return CapturedErrors.Any(e => e.Contains(partialMessage));
            }

            public static bool ContainsInfo(string partialMessage)
            {
                return CapturedInfo.Any(i => i.Contains(partialMessage));
            }
        }
    }
}
