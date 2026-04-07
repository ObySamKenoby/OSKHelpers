using OSKHelpers.Docker;
using System;
using System.Runtime.InteropServices;

namespace OSKHelpers.Common
{
    public class OSKEnvironment
    {
        /// <summary>
        /// True if the runtime environment is Windows.
        /// </summary>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// True if the runtime environment is macOS.
        /// </summary>
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// True if the runtime environment is Linux.
        /// </summary>
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// True if the runtime environment is interactive (console).
        /// </summary>
        public static bool IsInteractive => Environment.UserInteractive &&
            (IsWindows || !(Console.IsInputRedirected && Console.IsOutputRedirected && Console.IsErrorRedirected));

        /// <inheritdoc cref="DockerUtils.IsDockerized"/>
        public static bool IsDockerized => DockerUtils.IsDockerized;
    }
}
