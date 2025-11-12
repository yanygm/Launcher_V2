using System.Runtime.InteropServices;

namespace Launcher.Library.Utilities
{
    public static class NativeMethods
    {
        public static bool IsAmd64
        {
            get
            {
                return Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE").ToLower() == "amd64";
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        public static extern int AllocConsole();

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        public static extern int FreeConsole();
    }
}