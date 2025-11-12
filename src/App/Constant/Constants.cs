namespace Launcher.App.Constant
{
    internal static class Constants
    {
#if DEBUG
        /// <summary>
        /// 是否处于 DEBUG 模式
        /// </summary>
        public const bool DBG = true;
#else
        /// <summary>
        /// 是否处于 DEBUG 模式
        /// </summary>
        public const bool DBG = false;
#endif

        /// <summary>
        /// GitHub Repo Constants.Owner
        /// </summary>
        public const string Owner = "TheMagicFlute";

        /// <summary>
        /// GitHub Repo Name
        /// </summary>
        public const string Repo = "Launcher_V2";

        /// <summary>
        /// Current Program Version (like 251001)
        /// </summary>
        public static string Version = GetCurrentVersion();

        /// <summary>
        /// Returns the current version of the program, which is the same as compile time
        /// </summary>
        /// <returns>Current version of the program</returns>
        private static string GetCurrentVersion()
        {
#if DEBUG
            return DateTime.Now.ToString("yyMMdd");
#else
            return ThisAssembly.Git.CommitDate.Substring(0, 10).Replace("-", "").Substring(2, 6);
#endif
        }
    }
}
