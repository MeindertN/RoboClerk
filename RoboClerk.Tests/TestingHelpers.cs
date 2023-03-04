using System.Runtime.InteropServices;
using System.Text;

namespace RoboClerk.Tests
{
    internal static class TestingHelpers
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
    }
}
