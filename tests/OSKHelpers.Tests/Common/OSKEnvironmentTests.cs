using System;
using System.Runtime.InteropServices;
using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="OSKEnvironment"/> class.
    /// </summary>
    [TestClass]
    public class OSKEnvironmentTests
    {
        /// <summary>
        /// Verifies that at least one of the platform properties is true.
        /// </summary>
        [TestMethod]
        public void PlatformAtLeastOneIsTrue()
        {
            var result = OSKEnvironment.IsWindows || OSKEnvironment.IsMacOS || OSKEnvironment.IsLinux;

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Verifies consistency with RuntimeInformation for Windows.
        /// </summary>
        [TestMethod]
        public void IsWindowsMatchesRuntimeInformation()
        {
            var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            Assert.AreEqual(expected, OSKEnvironment.IsWindows);
        }

        /// <summary>
        /// Verifies consistency with RuntimeInformation for Linux.
        /// </summary>
        [TestMethod]
        public void IsLinuxMatchesRuntimeInformation()
        {
            var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            Assert.AreEqual(expected, OSKEnvironment.IsLinux);
        }

        /// <summary>
        /// Verifies consistency with RuntimeInformation for macOS.
        /// </summary>
        [TestMethod]
        public void IsMacOSMatchesRuntimeInformation()
        {
            var expected = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            Assert.AreEqual(expected, OSKEnvironment.IsMacOS);
        }

        /// <summary>
        /// Verifies that IsDockerized is consistent with DockerUtils.
        /// </summary>
        [TestMethod]
        public void IsDockerizedMatchesDockerUtils()
        {
            Assert.AreEqual(Docker.DockerUtils.IsDockerized, OSKEnvironment.IsDockerized);
        }
    }
}
