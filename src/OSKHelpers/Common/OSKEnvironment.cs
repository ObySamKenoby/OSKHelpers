using OSKHelpers.Docker;
using System;
using System.Runtime.InteropServices;

namespace OSKHelpers.Common
{
    public class OSKEnvironment
    {
        /// <summary>
        /// True se l'ambiente di esecuzione è windows.
        /// </summary>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// True se l'ambiente di esecuzione è MacOS.
        /// </summary>
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// True se l'ambiente di esecuzione è Linux.
        /// </summary>
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// True se l'ambiente di esecuzione è interattivo (console).
        /// </summary>
        public static bool IsInteractive => Environment.UserInteractive &&
            (IsWindows || !(Console.IsInputRedirected && Console.IsOutputRedirected && Console.IsErrorRedirected));

        /// <inheritdoc cref="DockerUtils.IsDockerized"/>
        public static bool IsDockerized => DockerUtils.IsDockerized;
    }
}
